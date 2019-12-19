using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Wren
{
    /// <summary>
    /// A single virtual machine for executing Wren code.
    /// 
    /// Wren has no global state, so all state stored by a running interpreter lives here.
    /// </summary>
    public class VirtualMachine : IDisposable
    {
        private WrenVmSafeHandle _handle;
        private Configuration _config;
        private WrenException _lastError = null;

        internal WrenVmSafeHandle Handle => _handle;
        private const int NULL_FOREIGN_OBJECT_INDEX = -1;
        private HashSet<Internal.WrenForeignMethodFn> _foreignMethods = new HashSet<Internal.WrenForeignMethodFn>();
        private readonly Internal.WrenForeignMethodFn _nullMethodHandler = (_) => {};
        private List<object> _foreignObjects = new List<object>();
        private Dictionary<(string, string), ForeignClass> _foreignClasses =
            new Dictionary<(string, string), ForeignClass>();
        private List<Internal.WrenForeignClassMethods> _foreignClassAllocatorsAndFinalizers =
            new List<Internal.WrenForeignClassMethods>();
        private Dictionary<(string, string), Dictionary<(bool, string), ForeignMethodFn>> _foreignClassMethods =
            new Dictionary<(string, string), Dictionary<(bool, string), ForeignMethodFn>>();

        /// Creates a new Wren virtual machine using the given <paramref name="configuration"/>. If
        /// <paramref name="configuration"/> is <c>null</c>, uses a default configuration.
        public VirtualMachine(Configuration configuration = null)
        {
            _config = configuration ?? new Configuration();
            var nativeConfig = Internal.Configuration.DefaultConfiguration();
            // TODO: Convert friendly config to native config
            WireCallbacks(ref nativeConfig);
            _handle = WrenInterop.wrenNewVM(ref nativeConfig);
        }

        /// The event Wren uses to display text when <c>System.print()</c> or the other related
        /// functions are called.
        ///
        /// If unhandled, printed text is discarded.
        public event WriteEventHandler Write;
        /// The event Wren uses to report errors.
        ///
        /// When an error occurs, this will be called with the module name, line number, and an
        /// error message. If unhandled, Wren doesn't report any errors.
        public event ErrorEventHandler Error;

        /// Immediately run the garbage collector to free unused memory.
        public void CollectGarbage()
        {
            WrenInterop.wrenCollectGarbage(_handle);
        }

        /// Runs <paramref name="source"/>, a string of Wren source code in a new fiber in this VM
        /// in the context of resolved <paramref name="module"/>.
        public InterpretResult Interpret(string module, string source)
        {
            var result = WrenInterop.wrenInterpret(_handle, module, source);

            if (_config.RaiseExceptionOnError && _lastError != null)
            {
                var error = _lastError;
                _lastError = null;
                throw error;
            }

            return result;
        }

        /// Creates a handle that can be used to invoke a method with <paramref name="signature"/>
        /// on using a receiver and arguments that are set up on the stack.
        ///
        /// This handle can be used repeatedly to directly invoke that method from C# code using
        /// <see cref="Call"/>.
        ///
        /// When you are done with this handle, it must be released using
        /// <see cref="Handle.Dispose"/>.
        public Handle MakeCallHandle(string signature)
        {
            return new Handle(this, WrenInterop.wrenMakeCallHandle(_handle, signature));
        }

        /// Calls <paramref name="method"/>, using the receiver and arguments previously set up on
        /// the stack.
        ///
        /// <paramref name="method"/> must have been created by a call to
        /// <see cref="MakeCallHandle"/>. The arguments to the method must be already on the
        /// stack. The receiver should be in slot 0 with the remaining arguments following it, in
        /// order. It is an error if the number of arguments provided does not match the method's
        /// signature.
        ///
        /// After this returns, you can access the return value from slot 0 on the stack.
        public InterpretResult Call(Handle method)
        {
            var result = WrenInterop.wrenCall(_handle, method.RawHandle);

            if (_config.RaiseExceptionOnError && _lastError != null)
            {
                var error = _lastError;
                _lastError = null;
                throw error;
            }

            return result;
        }

        /// <summary>
        /// Returns the number of slots available to the current foreign method.
        /// </summary>
        public int GetSlotCount()
        {
            return WrenInterop.wrenGetSlotCount(_handle);
        }

        /// <summary>
        /// Ensures that the foreign method stack has at least <paramref name="numSlots"/>
        /// available for use, growing the stack if needed.
        ///
        /// Does not shrink the stack if it has more than enough slots.
        ///
        /// It is an error to call this from a finalizer.
        /// </summary>
        public void EnsureSlots(int numSlots)
        {
            WrenInterop.wrenEnsureSlots(_handle, numSlots);
        }

        /// <summary>
        /// Gets the type of the object in <paramref name="slot"/>.
        /// </summary>
        public ValueType GetSlotType(int slot)
        {
            return WrenInterop.wrenGetSlotType(_handle, slot);
        }

        /// <summary>
        /// Reads a boolean value from <paramref name="slot"/>.
        /// 
        /// It is an error to call this if the slot does not contain a boolean value.
        /// </summary>
        public bool GetSlotBool(int slot)
        {
            return WrenInterop.wrenGetSlotBool(_handle, slot);
        }

        /// <summary>
        /// Reads a byte array from <paramref name="slot"/>.
        /// 
        /// It is an error to call this if the slot does not contain a string.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns>
        /// Returns a managed array of bytes copied from the underlying unmanaged VM byte array.
        /// </returns>
        public byte[] GetSlotBytes(int slot)
        {
            int length = 0;
            var slotBytesPtr = WrenInterop.wrenGetSlotBytes(_handle, slot, ref length);
            var bytes = new byte[length];
            Marshal.Copy(slotBytesPtr, bytes, 0, length);
            return bytes;
        }

        /// <summary>
        /// Reads a number from <paramref name="slot"/>.
        /// 
        /// It is an error to call this if the slot does not contain a number.
        /// </summary>
        public double GetSlotDouble(int slot)
        {
            return WrenInterop.wrenGetSlotDouble(_handle, slot);
        }

        /// <summary>
        /// Reads a foreign object from <paramref name="slot"/> and returns the foreign data stored
        /// with it.
        /// 
        /// It is an error to call this if the slot does not contain an instance of a foreign
        /// class.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns>Foreign data stored with the foreign object.</returns>
        /// <remarks>Returned data may be null.</remarks>
        public object GetSlotForeign(int slot)
        {
            var foreignPtr = WrenInterop.wrenGetSlotForeign(_handle, slot);
            return MarshalForeign(foreignPtr);
        }

        /// <summary>
        /// Reads a string from <paramref name="slot"/>.
        ///
        /// It is an error to call this if the slot does not contain a string.
        /// </summary>
        /// <param name="slot"></param>
        public string GetSlotString(int slot)
        {
            var stringPointer = WrenInterop.wrenGetSlotString(_handle, slot);
            return Marshal.PtrToStringAnsi(stringPointer);
        }

        /// <summary>
        /// Creates a handle for the value stored in <paramref name="slot"/>.
        ///
        /// This will prevent the object that is referred to from being garbage collected until
        /// the handle is released by calling <see cref="Handle.Dispose"/>.
        /// </summary>
        /// <param name="slot"></param>
        public Handle GetSlotHandle(int slot)
        {
            return new Handle(this, WrenInterop.wrenGetSlotHandle(_handle, slot));
        }

        /// <summary>
        /// Stores the boolean <paramref name="value"/> in <paramref name="slot"/>.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="value"></param>
        public void SetSlotBool(int slot, bool value)
        {
            WrenInterop.wrenSetSlotBool(_handle, slot, value);
        }

        /// <summary>
        /// Stores an array of <paramref name="bytes"/> in <paramref name="slot"/>.
        /// 
        /// The bytes are copied to a new string within Wren's heap.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="bytes"></param>
        public void SetSlotBytes(int slot, ref byte[] bytes)
        {
            var pinnedBytes = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var bytesPtr = pinnedBytes.AddrOfPinnedObject();
            WrenInterop.wrenSetSlotBytes(_handle, slot, bytesPtr, (uint) bytes.Length);
            pinnedBytes.Free();
        }

        /// <summary>
        /// Stores the numeric <paramref name="value"/> in <paramref name="slot"/>.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="value"></param>
        public void SetSlotDouble(int slot, double value)
        {
            WrenInterop.wrenSetSlotDouble(_handle, slot, value);
        }

        /// <summary>
        /// Overwrite the raw object data of a foreign class instance stored in <paramref name="slot"/>.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="foreignObject"></param>
        public void SetSlotForeign(int slot, object foreignObject)
        {
            var foreignPtr = WrenInterop.wrenGetSlotForeign(_handle, slot);
            SetForeignObject(foreignPtr, foreignObject);
        }

        /// <summary>
        /// Creates a new instance of the foreign class stored in [classSlot] with raw object
        /// storage and places the resulting object in <paramref name="slot"/>.
        /// </summary>
        /// <param name="slot">Destination slot where the new foreign object should be placed. When
        /// you’re calling this in a foreign class’s allocate callback, this should be 0.</param>
        /// <param name="classSlot">Where the foreign class being constructed can be found. When
        /// the VM calls an allocate callback for a foreign class, the class itself is already in slot 0.</param>
        /// <param name="foreignObject">Initial value of the foreign data.</param>
        /// <remarks>The raw storage size is fixed because WrenSharp manages foreign data as pointers to CLR objects.</remarks>
        public void SetSlotNewForeign(int slot, int classSlot, object foreignObject = null)
        {
            var foreignPtr = WrenInterop.wrenSetSlotNewForeign(_handle, slot, classSlot,
                (uint) Marshal.SizeOf<int>());
            SetForeignObject(foreignPtr, foreignObject);
        }

        /// <summary>
        /// Stores a new empty list in <paramref name="slot"/>.
        /// </summary>
        /// <param name="slot"></param>
        public void SetSlotNewList(int slot)
        {
            WrenInterop.wrenSetSlotNewList(_handle, slot);
        }

        /// <summary>
        /// Stores null in <paramref name="slot"/>.
        /// </summary>
        /// <param name="slot"></param>
        public void SetSlotNull(int slot)
        {
            WrenInterop.wrenSetSlotNull(_handle, slot);
        }

        /// <summary>
        /// Stores the string <paramref name="text"/> in <paramref name="slot"/>.
        /// 
        /// The <paramref name="text"/> is copied to a new string within Wren's heap. If the string
        /// may contain any null bytes in the middle, then you should use
        /// <see cref="SetSlotBytes"/> instead.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="text"></param>
        public void SetSlotString(int slot, string text)
        {
            WrenInterop.wrenSetSlotString(_handle, slot, text);
        }

        /// <summary>
        /// Stores the value captured in [handle] in <paramref name="slot"/>.
        /// 
        /// This does not release the handle for the value.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="handle"></param>
        public void SetSlotHandle(int slot, Handle handle)
        {
            WrenInterop.wrenSetSlotHandle(_handle, slot, handle.RawHandle);
        }

        /// <summary>
        /// Stores dynamic <paramref name="value"/> in <paramref name="slot"/>.
        /// </summary>
        /// <remarks>
        /// It is an error to call this if <paramref name="value"/> is not <c>null</c> or of type
        /// <see cref="bool"/>, <see cref="int"/>, <see cref="double"/>, <see cref="string"/>,
        /// or <see cref="Handle"/>.
        /// </remarks>
        /// <param name="slot"></param>
        /// <param name="value"></param>
        public void SetSlot(int slot, object value)
        {
            if (value == null)
            {
                SetSlotNull(slot);
            }
            else if (value is bool boolValue)
            {
                SetSlotBool(slot, boolValue);
            }
            else if (value is int intValue)
            {
                SetSlotDouble(slot, intValue);
            }
            else if (value is double doubleValue)
            {
                SetSlotDouble(slot, doubleValue);
            }
            else if (value is string stringValue)
            {
                SetSlotString(slot, stringValue);
            }
            else if (value is Handle handle)
            {
                SetSlotHandle(slot, handle);
            }
            else
            {
                throw new ArgumentException("Bad value type for slot", nameof(value));
            }
        }

        /// <summary>
        /// Returns the number of elements in the list stored in <paramref name="slot"/>.
        /// </summary>
        /// <param name="slot"></param>
        public int GetListCount(int slot)
        {
            return WrenInterop.wrenGetListCount(_handle, slot);
        }

        /// <summary>
        /// Reads element <paramref name="index"/> from the list in <paramref name="listSlot"/> and
        /// stores it in <paramref name="elementSlot"/>.
        /// </summary>
        /// <param name="listSlot"></param>
        /// <param name="index"></param>
        /// <param name="elementSlot"></param>
        public void GetListElement(int listSlot, int index, int elementSlot)
        {
            WrenInterop.wrenGetListElement(_handle, listSlot, index, elementSlot);
        }

        /// <summary>
        /// Takes the value stored at <paramref name="elementSlot"/> and inserts it into the list
        /// stored at <paramref name="listSlot"/> at <paramref name="index"/>.
        /// 
        /// As in Wren, negative indexes can be used to insert from the end. To append an element,
        /// use <c>-1</c> for the index.
        /// </summary>
        /// <param name="listSlot"></param>
        /// <param name="index"></param>
        /// <param name="elementSlot"></param>
        public void InsertInList(int listSlot, int index, int elementSlot)
        {
            WrenInterop.wrenInsertInList(_handle, listSlot, index, elementSlot);
        }

        /// <summary>
        /// Looks up the top level variable with <paramref name="name"/> in resolved
        /// <paramref name="module"/> and stores it in <paramref name="slot"/>.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="name"></param>
        /// <param name="slot"></param>
        public void GetVariable(string module, string name, int slot)
        {
            WrenInterop.wrenGetVariable(_handle, module, name, slot);
        }

        /// <summary>
        /// Sets the current fiber to be aborted, and uses the value in <paramref name="slot"/> as
        /// the runtime error object.
        /// </summary>
        /// <param name="slot"></param>
        public void AbortFiber(int slot)
        {
            WrenInterop.wrenAbortFiber(_handle, slot);
        }

        /// <summary>
        /// Bind a C# class to this VM in resolved <paramref name="module"/> dynamically.
        /// 
        /// Given type must be derived from <see cref="ForeignObject"/>.
        /// </summary>
        /// <param name="module"></param>
        /// <typeparam name="T">Type with which to dynamically bind.</typeparam>
        public void BindForeign<T>(string module) where T : ForeignObject
        {
            var foreignClass = new ForeignClass
            {
                Allocate = (_) => ForeignObject.Allocate<T>(this),
                Finalize = (objectToFinalize) =>
                {
                    if (objectToFinalize is ForeignObject foreignObject)
                    {
                        foreignObject.Dispose();
                    }
                }
            };
            string className = typeof(T).Name;
            _foreignClasses.Add((module, className), foreignClass);
            _foreignClassMethods.Add((module, className), ForeignObject.Methods<T>());
        }

        internal object MarshalForeign(IntPtr foreignPtr)
        {
            var objectIndex = Marshal.ReadInt32(foreignPtr);

            // Index into the foreign objects list
            var isOutOfBounds = objectIndex < 0 || objectIndex < _foreignObjects.Count - 1;
            if (isOutOfBounds)
            {
                return null;
            }
            object obj = _foreignObjects[objectIndex];
            return obj;
        }

        private void WireCallbacks(ref Internal.Configuration config)
        {
            config.bindForeignClassFn = OnBindForeignClass;
            config.bindForeignMethodFn = OnBindForeignMethod;

            config.writeFn = (_, text) => OnWrite(text);
            config.errorFn = (_, type, module, line, message) => OnError(type, module, line, message);
            // TODO: Use events for the rest of config's callbacks (resolveModuleFn, loadModuleFn)
        }

        private void SetForeignObject(IntPtr foreignPtr, object foreignObject)
        {
            var foreignObjectIndex = NULL_FOREIGN_OBJECT_INDEX;
            if (foreignObject != null)
            {
                _foreignObjects.Add(foreignObject);
                foreignObjectIndex = _foreignObjects.LastIndexOf(foreignObject);
            }
            Marshal.WriteInt32(foreignPtr, foreignObjectIndex);
        }

        private Internal.WrenForeignClassMethods OnBindForeignClass(IntPtr vm, string module, string className)
        {
            Nullable<ForeignClass> foreignClass = null;
            if (_config.BindForeignClass != null)
            {
                foreignClass = _config.BindForeignClass(this, module, className);
            }
            // Try to bind to a C# class
            if (!foreignClass.HasValue && _foreignClasses.ContainsKey((module, className)))
            {
                foreignClass = _foreignClasses[(module, className)];
            }

            // Unknown class default
            var allocatorAndFinalizer = new Internal.WrenForeignClassMethods
            {
                allocate = (_) =>
                {
                    var foreignPtr = WrenInterop.wrenSetSlotNewForeign(_handle, 0, 0,
                        (uint) Marshal.SizeOf<int>());
                    Marshal.WriteInt32(foreignPtr, NULL_FOREIGN_OBJECT_INDEX);
                },
                finalize = null
            };

            if (foreignClass.HasValue)
            {
                var foreign = foreignClass.Value;
                allocatorAndFinalizer = new Internal.WrenForeignClassMethods
                {
                    allocate = (_) => SetSlotNewForeign(0, 0, foreign.Allocate(this)),
                    finalize = (foreignPtr) =>
                    {
                        foreign.Finalize(MarshalForeign(foreignPtr));
                    }
                };
            }

            // Keep a handle on allocators and finalizers for VM's lifetime
            _foreignClassAllocatorsAndFinalizers.Add(allocatorAndFinalizer);

            return allocatorAndFinalizer;
        }

        private IntPtr OnBindForeignMethod(IntPtr vm, string module, string className, bool isStatic, string signature)
        {
            ForeignMethodFn foreignMethod = null;
            if (_config.BindForeignMethod != null) foreignMethod =
                _config.BindForeignMethod(this, module, className, isStatic, signature);
            // Try to bind to a C# class method
            if (foreignMethod == null && _foreignClassMethods.ContainsKey((module, className)))
            {
                var methods = _foreignClassMethods[(module, className)];
                if (methods.ContainsKey((isStatic, signature))) foreignMethod =
                    methods[(isStatic, signature)];
            }

            // Create delegate given to Wren VM
            var handler = _nullMethodHandler;
            if (foreignMethod != null)
            {
                handler = (_) =>
                {
                    foreignMethod(this);
                };
            }

            // Ensure handles to foreign methods are kept reachable
            if (handler != _nullMethodHandler && !_foreignMethods.Contains(handler))
            {
                _foreignMethods.Add(handler);
            }

            return Marshal.GetFunctionPointerForDelegate(handler);
        }

        private void OnWrite(string text)
        {
            if (Write != null)
            {
                Write(this, new WriteEventArgs(text));
            }
        }

        private void OnError(ErrorType type, string module, int line, string message)
        {
            if (Error != null)
            {
                var args = new ErrorEventArgs(type, module, line, message);
                Error(this, args);
            }

            if (_config.RaiseExceptionOnError)
            {
                if (type == ErrorType.WREN_ERROR_STACK_TRACE)
                {
                    _lastError.AddStackFrame(new StackFrame(type, module, line, message));
                }
                else
                {
                    _lastError = new WrenException(type, module, line, message);
                }
            }
        }

        #region IDisposable Support
        // This code added to correctly implement the disposable pattern.
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Disposes of all resources in use by this VM.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _handle.Dispose();

                if (disposing)
                {
                    _foreignObjects.Clear();
                    _foreignMethods.Clear();
                    _foreignClasses.Clear();
                    _foreignClassMethods.Clear();
                    _foreignClassAllocatorsAndFinalizers.Clear();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Disposes of all resources in use by this VM.
        /// </summary>
        ~VirtualMachine()
        {
          // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
          Dispose(false);
        }

        /// <summary>
        /// Disposes of all resources in use by this VM.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Wren
{
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

        public VirtualMachine(Configuration config = null)
        {
            _config = config ?? new Configuration();
            var nativeConfig = Internal.Configuration.DefaultConfiguration();
            // TODO: Convert friendly config to native config
            WireCallbacks(ref nativeConfig);
            _handle = WrenInterop.wrenNewVM(ref nativeConfig);
        }

        /// Display text when `System.print()` or the other related functions are called.
        ///
        /// If unhandled, printed text is discarded.
        public event WriteEventHandler Write;
        public event ErrorEventHandler Error;

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

        public Handle MakeCallHandle(string signature)
        {
            return new Handle(this, WrenInterop.wrenMakeCallHandle(_handle, signature));
        }

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

        public int GetSlotCount()
        {
            return WrenInterop.wrenGetSlotCount(_handle);
        }

        public void EnsureSlots(int numSlots)
        {
            WrenInterop.wrenEnsureSlots(_handle, numSlots);
        }

        public ValueType GetSlotType(int slot)
        {
            return WrenInterop.wrenGetSlotType(_handle, slot);
        }

        public bool GetSlotBool(int slot)
        {
            return WrenInterop.wrenGetSlotBool(_handle, slot);
        }

        public byte[] GetSlotBytes(int slot)
        {
            int length = 0;
            var slotBytesPtr = WrenInterop.wrenGetSlotBytes(_handle, slot, ref length);
            var bytes = new byte[length];
            Marshal.Copy(slotBytesPtr, bytes, 0, length);
            return bytes;
        }

        public double GetSlotDouble(int slot)
        {
            return WrenInterop.wrenGetSlotDouble(_handle, slot);
        }

        /// <summary>
        /// Reads a foreign object from [slot] and returns the foreign data stored with it.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns>Foreign data stored with the foreign object.</returns>
        /// <remarks>Returned data may be null.
        /// 
        /// It is an error to call this if the slot does not contain an instance of a foreign
        /// class.</remarks>
        public object GetSlotForeign(int slot)
        {
            var foreignPtr = WrenInterop.wrenGetSlotForeign(_handle, slot);
            return MarshalForeign(foreignPtr);
        }

        public string GetSlotString(int slot)
        {
            var stringPointer = WrenInterop.wrenGetSlotString(_handle, slot);
            return Marshal.PtrToStringAnsi(stringPointer);
        }

        public Handle GetSlotHandle(int slot)
        {
            return new Handle(this, WrenInterop.wrenGetSlotHandle(_handle, slot));
        }

        public void SetSlotBool(int slot, bool value)
        {
            WrenInterop.wrenSetSlotBool(_handle, slot, value);
        }

        public void SetSlotBytes(int slot, ref byte[] bytes)
        {
            var pinnedBytes = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var bytesPtr = pinnedBytes.AddrOfPinnedObject();
            WrenInterop.wrenSetSlotBytes(_handle, slot, bytesPtr, (uint) bytes.Length);
            pinnedBytes.Free();
        }

        public void SetSlotDouble(int slot, double value)
        {
            WrenInterop.wrenSetSlotDouble(_handle, slot, value);
        }

        /// <summary>
        /// Overwrite the raw object data of a foreign class instance stored in [slot].
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
        /// storage and places the resulting object in [slot].
        /// </summary>
        /// <param name="slot">Destination slot where the new foreign object should be placed. When
        /// you’re calling this in a foreign class’s allocate callback, this should be 0.</param>
        /// <param name="classSlot">Where the foreign class being constructed can be found. When
        /// the VM calls an allocate callback for a foreign class, the class itself is already in slot 0.</param>
        /// <param name="foreignObject">Initial value of the foreign data.</param>
        /// <remarks>The raw storage size is fixed because WrenSharp manages foreign data as pointers to CLR objects.</returns>
        public void SetSlotNewForeign(int slot, int classSlot, object foreignObject = null)
        {
            var foreignPtr = WrenInterop.wrenSetSlotNewForeign(_handle, slot, classSlot,
                (uint) Marshal.SizeOf<int>());
            SetForeignObject(foreignPtr, foreignObject);
        }

        public void SetSlotNewList(int slot)
        {
            WrenInterop.wrenSetSlotNewList(_handle, slot);
        }

        public void SetSlotNull(int slot)
        {
            WrenInterop.wrenSetSlotNull(_handle, slot);
        }

        public void SetSlotString(int slot, string text)
        {
            WrenInterop.wrenSetSlotString(_handle, slot, text);
        }

        public void SetSlotHandle(int slot, Handle handle)
        {
            WrenInterop.wrenSetSlotHandle(_handle, slot, handle.RawHandle);
        }

        public int GetListCount(int slot)
        {
            return WrenInterop.wrenGetListCount(_handle, slot);
        }

        public void GetListElement(int listSlot, int index, int elementSlot)
        {
            WrenInterop.wrenGetListElement(_handle, listSlot, index, elementSlot);
        }

        public void InsertInList(int listSlot, int index, int elementSlot)
        {
            WrenInterop.wrenInsertInList(_handle, listSlot, index, elementSlot);
        }

        public void GetVariable(string module, string name, int slot)
        {
            WrenInterop.wrenGetVariable(_handle, module, name, slot);
        }

        public void AbortFiber(int slot)
        {
            WrenInterop.wrenAbortFiber(_handle, slot);
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

            if (_config.BindForeignMethod != null)
            {
                config.bindForeignMethodFn = OnBindForeignMethod;
            }

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
            if (foreignClass.HasValue)
            {
                var foreign = foreignClass.Value;
                return new Internal.WrenForeignClassMethods
                {
                    allocate = (_) => SetSlotNewForeign(0, 0, foreign.Allocate(this)),
                    finalize = (foreignPtr) =>
                    {
                        foreign.Finalize(MarshalForeign(foreignPtr));
                    }
                };
            }

            // Unknown class
            return new Internal.WrenForeignClassMethods
            {
                allocate = (_) =>
                {
                    var foreignPtr = WrenInterop.wrenSetSlotNewForeign(_handle, 0, 0,
                        (uint) Marshal.SizeOf<int>());
                    Marshal.WriteInt32(foreignPtr, NULL_FOREIGN_OBJECT_INDEX);
                },
                finalize = null
            };
        }

        private IntPtr OnBindForeignMethod(IntPtr vm, string module, string className, bool isStatic, string signature)
        {
            var foreignMethod = _config.BindForeignMethod(this, module, className, isStatic, signature);
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
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _foreignObjects.Clear();
                }

                _handle.Dispose();

                if (disposing)
                {
                    _foreignMethods.Clear();
                }

                disposedValue = true;
            }
        }

        ~VirtualMachine()
        {
          // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
          Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

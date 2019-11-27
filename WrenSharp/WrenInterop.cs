using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace Wren
{
    // TODO: marshalling delegates as callback methods? https://docs.microsoft.com/en-us/dotnet/framework/interop/marshaling-a-delegate-as-a-callback-method

    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    internal sealed class WrenVmSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public WrenVmSafeHandle() : base(ownsHandle: true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            // Adapted from https://www.meziantou.net/stop-using-intptr-for-dealing-with-system-handles.htm
            WrenInterop.wrenFreeVM(handle);
            return true;
        }
    }

    [SuppressUnmanagedCodeSecurity()]
    internal class WrenInterop
    {
        // Initializes [configuration] with all of its default values.
        //
        // Call this before setting the particular fields you care about.
        [DllImport("libwren")]
        internal static extern void wrenInitConfiguration(WrenConfiguration* configuration);

        // Creates a new Wren virtual machine using the given [configuration]. Wren
        // will copy the configuration data, so the argument passed to this can be
        // freed after calling this. If [configuration] is `NULL`, uses a default
        // configuration.
        [DllImport("libwren")]
        internal static extern WrenVmSafeHandle wrenNewVM(WrenConfiguration* configuration);

        // Disposes of all resources is use by [vm], which was previously created by a
        // call to [wrenNewVM].
        [DllImport("libwren")]
        internal static extern void wrenFreeVM(IntPtr vm);

        // Immediately run the garbage collector to free unused memory.
        [DllImport("libwren")]
        internal static extern void wrenCollectGarbage(WrenVmSafeHandle vm);

        // Runs [source], a string of Wren source code in a new fiber in [vm] in the
        // context of resolved [module].
        [DllImport("libwren")]
        internal static extern InterpretResult wrenInterpret(WrenVmSafeHandle vm, [MarshalAs(UnmanagedType.LPStr)] string module,
                                        [MarshalAs(UnmanagedType.LPStr)] string source);

        // Creates a handle that can be used to invoke a method with [signature] on
        // using a receiver and arguments that are set up on the stack.
        //
        // This handle can be used repeatedly to directly invoke that method from C
        // code using [wrenCall].
        //
        // When you are done with this handle, it must be released using
        // [wrenReleaseHandle].
        [DllImport("libwren")]
        internal static extern WrenHandle* wrenMakeCallHandle(WrenVmSafeHandle vm, [MarshalAs(UnmanagedType.LPStr)] string signature);

        // Calls [method], using the receiver and arguments previously set up on the
        // stack.
        //
        // [method] must have been created by a call to [wrenMakeCallHandle]. The
        // arguments to the method must be already on the stack. The receiver should be
        // in slot 0 with the remaining arguments following it, in order. It is an
        // error if the number of arguments provided does not match the method's
        // signature.
        //
        // After this returns, you can access the return value from slot 0 on the stack.
        [DllImport("libwren")]
        internal static extern InterpretResult wrenCall(WrenVmSafeHandle vm, WrenHandle* method);

        // Releases the reference stored in [handle]. After calling this, [handle] can
        // no longer be used.
        [DllImport("libwren")]
        internal static extern void wrenReleaseHandle(WrenVmSafeHandle vm, WrenHandle* handle);

        // The following functions are intended to be called from foreign methods or
        // finalizers. The interface Wren provides to a foreign method is like a
        // register machine: you are given a numbered array of slots that values can be
        // read from and written to. Values always live in a slot (unless explicitly
        // captured using wrenGetSlotHandle(), which ensures the garbage collector can
        // find them.
        //
        // When your foreign function is called, you are given one slot for the receiver
        // and each argument to the method. The receiver is in slot 0 and the arguments
        // are in increasingly numbered slots after that. You are free to read and
        // write to those slots as you want. If you want more slots to use as scratch
        // space, you can call wrenEnsureSlots() to add more.
        //
        // When your function returns, every slot except slot zero is discarded and the
        // value in slot zero is used as the return value of the method. If you don't
        // store a return value in that slot yourself, it will retain its previous
        // value, the receiver.
        //
        // While Wren is dynamically typed, C is not. This means the C interface has to
        // support the various types of primitive values a Wren variable can hold: bool,
        // double, string, etc. If we supported this for every operation in the C API,
        // there would be a combinatorial explosion of functions, like "get a
        // double-valued element from a list", "insert a string key and double value
        // into a map", etc.
        //
        // To avoid that, the only way to convert to and from a raw C value is by going
        // into and out of a slot. All other functions work with values already in a
        // slot. So, to add an element to a list, you put the list in one slot, and the
        // element in another. Then there is a single API function wrenInsertInList()
        // that takes the element out of that slot and puts it into the list.
        //
        // The goal of this API is to be easy to use while not compromising performance.
        // The latter means it does not do type or bounds checking at runtime except
        // using assertions which are generally removed from release builds. C is an
        // unsafe language, so it's up to you to be careful to use it correctly. In
        // return, you get a very fast FFI.

        // Returns the number of slots available to the current foreign method.
        [DllImport("libwren")]
        internal static extern int wrenGetSlotCount(WrenVmSafeHandle vm);

        // Ensures that the foreign method stack has at least [numSlots] available for
        // use, growing the stack if needed.
        //
        // Does not shrink the stack if it has more than enough slots.
        //
        // It is an error to call this from a finalizer.
        [DllImport("libwren")]
        internal static extern void wrenEnsureSlots(WrenVmSafeHandle vm, int numSlots);

        // Gets the type of the object in [slot].
        [DllImport("libwren")]
        internal static extern Type wrenGetSlotType(WrenVmSafeHandle vm, int slot);

        // Reads a boolean value from [slot].
        //
        // It is an error to call this if the slot does not contain a boolean value.
        [DllImport("libwren")]
        internal static extern bool wrenGetSlotBool(WrenVmSafeHandle vm, int slot);

        // Reads a byte array from [slot].
        //
        // The memory for the returned string is owned by Wren. You can inspect it
        // while in your foreign method, but cannot keep a pointer to it after the
        // function returns, since the garbage collector may reclaim it.
        //
        // Returns a pointer to the first byte of the array and fill [length] with the
        // number of bytes in the array.
        //
        // It is an error to call this if the slot does not contain a string.
        [DllImport("libwren", CharSet = CharSet.Ansi)]
        [return : MarshalAs(UnmanagedType.LPStr)]
        internal static extern string wrenGetSlotBytes(WrenVmSafeHandle vm, int slot, int* length);

        // Reads a number from [slot].
        //
        // It is an error to call this if the slot does not contain a number.
        [DllImport("libwren")]
        internal static extern double wrenGetSlotDouble(WrenVmSafeHandle vm, int slot);

        // Reads a foreign object from [slot] and returns a pointer to the foreign data
        // stored with it.
        //
        // It is an error to call this if the slot does not contain an instance of a
        // foreign class.
        [DllImport("libwren")]
        internal static extern void* wrenGetSlotForeign(WrenVmSafeHandle vm, int slot);

        // Reads a string from [slot].
        //
        // The memory for the returned string is owned by Wren. You can inspect it
        // while in your foreign method, but cannot keep a pointer to it after the
        // function returns, since the garbage collector may reclaim it.
        //
        // It is an error to call this if the slot does not contain a string.
        [DllImport("libwren", CharSet = CharSet.Ansi)]
        [return : MarshalAs(UnmanagedType.LPStr)]
        internal static extern string wrenGetSlotString(WrenVmSafeHandle vm, int slot);

        // Creates a handle for the value stored in [slot].
        //
        // This will prevent the object that is referred to from being garbage collected
        // until the handle is released by calling [wrenReleaseHandle()].
        [DllImport("libwren")]
        internal static extern WrenHandle* wrenGetSlotHandle(WrenVmSafeHandle vm, int slot);

        // Stores the boolean [value] in [slot].
        [DllImport("libwren")]
        internal static extern void wrenSetSlotBool(WrenVmSafeHandle vm, int slot, bool value);

        // Stores the array [length] of [bytes] in [slot].
        //
        // The bytes are copied to a new string within Wren's heap, so you can free
        // memory used by them after this is called.
        [DllImport("libwren")]
        internal static extern void wrenSetSlotBytes(WrenVmSafeHandle vm, int slot, [MarshalAs(UnmanagedType.LPStr)] string bytes, uint length);

        // Stores the numeric [value] in [slot].
        [DllImport("libwren")]
        internal static extern void wrenSetSlotDouble(WrenVmSafeHandle vm, int slot, double value);

        // Creates a new instance of the foreign class stored in [classSlot] with [size]
        // bytes of raw storage and places the resulting object in [slot].
        //
        // This does not invoke the foreign class's constructor on the new instance. If
        // you need that to happen, call the constructor from Wren, which will then
        // call the allocator foreign method. In there, call this to create the object
        // and then the constructor will be invoked when the allocator returns.
        //
        // Returns a pointer to the foreign object's data.
        [DllImport("libwren")]
        internal static extern void* wrenSetSlotNewForeign(WrenVmSafeHandle vm, int slot, int classSlot, uint size);

        // Stores a new empty list in [slot].
        [DllImport("libwren")]
        internal static extern void wrenSetSlotNewList(WrenVmSafeHandle vm, int slot);

        // Stores null in [slot].
        [DllImport("libwren")]
        internal static extern void wrenSetSlotNull(WrenVmSafeHandle vm, int slot);

        // Stores the string [text] in [slot].
        //
        // The [text] is copied to a new string within Wren's heap, so you can free
        // memory used by it after this is called. The length is calculated using
        // [strlen()]. If the string may contain any null bytes in the middle, then you
        // should use [wrenSetSlotBytes()] instead.
        [DllImport("libwren")]
        internal static extern void wrenSetSlotString(WrenVmSafeHandle vm, int slot, [MarshalAs(UnmanagedType.LPStr)] string text);

        // Stores the value captured in [handle] in [slot].
        //
        // This does not release the handle for the value.
        [DllImport("libwren")]
        internal static extern void wrenSetSlotHandle(WrenVmSafeHandle vm, int slot, WrenHandle* handle);

        // Returns the number of elements in the list stored in [slot].
        [DllImport("libwren")]
        internal static extern int wrenGetListCount(WrenVmSafeHandle vm, int slot);

        // Reads element [index] from the list in [listSlot] and stores it in
        // [elementSlot].
        [DllImport("libwren")]
        internal static extern void wrenGetListElement(WrenVmSafeHandle vm, int listSlot, int index, int elementSlot);

        // Takes the value stored at [elementSlot] and inserts it into the list stored
        // at [listSlot] at [index].
        //
        // As in Wren, negative indexes can be used to insert from the end. To append
        // an element, use `-1` for the index.
        [DllImport("libwren")]
        internal static extern void wrenInsertInList(WrenVmSafeHandle vm, int listSlot, int index, int elementSlot);

        // Looks up the top level variable with [name] in resolved [module] and stores
        // it in [slot].
        [DllImport("libwren")]
        internal static extern void wrenGetVariable(WrenVmSafeHandle vm, [MarshalAs(UnmanagedType.LPStr)] string module, [MarshalAs(UnmanagedType.LPStr)] string name,
                            int slot);

        // Sets the current fiber to be aborted, and uses the value in [slot] as the
        // runtime error object.
        [DllImport("libwren")]
        internal static extern void wrenAbortFiber(WrenVmSafeHandle vm, int slot);

        // Returns the user data associated with the WrenVM.
        [DllImport("libwren")]
        internal static extern void* wrenGetUserData(WrenVmSafeHandle vm);

        // Sets user data associated with the WrenVM.
        [DllImport("libwren")]
        internal static extern void wrenSetUserData(WrenVmSafeHandle vm, void* userData);
    }
}

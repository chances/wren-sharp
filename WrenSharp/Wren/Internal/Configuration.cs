using System;
using System.Runtime.InteropServices;

namespace Wren.Internal
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void WrenForeignMethodFn(IntPtr vm);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void WrenFinalizerFn(IntPtr data);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr WrenBindForeignMethodFn(IntPtr vm,
        [MarshalAs(UnmanagedType.LPStr)] string module,
        [MarshalAs(UnmanagedType.LPStr)] string className,
        bool isStatic,
        [MarshalAs(UnmanagedType.LPStr)] string signature);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate WrenForeignClassMethods WrenBindForeignClassFn(IntPtr vm,
        [MarshalAs(UnmanagedType.LPStr)] string module,
        [MarshalAs(UnmanagedType.LPStr)] string className);
    internal delegate void WrenWriteFn(IntPtr vm, [MarshalAs(UnmanagedType.LPStr)] string text);
    internal delegate void WrenErrorFn(IntPtr vm, ErrorType type,
        [MarshalAs(UnmanagedType.LPStr)] string module,
        int line,
        [MarshalAs(UnmanagedType.LPStr)] string message);

    [StructLayout(LayoutKind.Sequential)]
    internal struct WrenForeignClassMethods
    {
        public WrenForeignMethodFn allocate;
        public WrenFinalizerFn finalize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Configuration
    {
        /// The callback Wren will use to allocate, reallocate, and deallocate memory.
        ///
        /// If `NULL`, defaults to a built-in function that uses `realloc` and `free`.
        public IntPtr reallocateFn; //   WrenReallocateFn

        /// The callback Wren uses to resolve a module name.
        ///
        /// Some host applications may wish to support "relative" imports, where the
        /// meaning of an import string depends on the module that contains it. To
        /// support that without baking any policy into Wren itself, the VM gives the
        /// host a chance to resolve an import string.
        ///
        /// Before an import is loaded, it calls this, passing in the name of the
        /// module that contains the import and the import string. The host app can
        /// look at both of those and produce a new "canonical" string that uniquely
        /// identifies the module. This string is then used as the name of the module
        /// going forward. It is what is passed to [loadModuleFn], how duplicate
        /// imports of the same module are detected, and how the module is reported in
        /// stack traces.
        ///
        /// If you leave this function NULL, then the original import string is
        /// treated as the resolved string.
        ///
        /// If an import cannot be resolved by the embedder, it should return NULL and
        /// Wren will report that as a runtime error.
        ///
        /// Wren will take ownership of the string you return and free it for you, so
        /// it should be allocated using the same allocation function you provide
        /// above.
        public IntPtr resolveModuleFn; //   WrenResolveModuleFn

        /// The callback Wren uses to load a module.
        ///
        /// Since Wren does not talk directly to the file system, it relies on the
        /// embedder to physically locate and read the source code for a module. The
        /// first time an import appears, Wren will call this and pass in the name of
        /// the module being imported. The VM should return the soure code for that
        /// module. Memory for the source should be allocated using [reallocateFn] and
        /// Wren will take ownership over it.
        ///
        /// This will only be called once for any given module name. Wren caches the
        /// result internally so subsequent imports of the same module will use the
        /// previous source and not call this.
        ///
        /// If a module with the given name could not be found by the embedder, it
        /// should return NULL and Wren will report that as a runtime error.
        public IntPtr loadModuleFn; //   WrenLoadModuleFn

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public WrenBindForeignMethodFn bindForeignMethodFn;

        public WrenBindForeignClassFn bindForeignClassFn;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public WrenWriteFn writeFn;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public WrenErrorFn errorFn;

        /// The number of bytes Wren will allocate before triggering the first garbage
        /// collection.
        ///
        /// If zero, defaults to 10MB.
        public ulong initialHeapSize;

        /// After a collection occurs, the threshold for the next collection is
        /// determined based on the number of bytes remaining in use. This allows Wren
        /// to shrink its memory usage automatically after reclaiming a large amount
        /// of memory.
        ///
        /// This can be used to ensure that the heap does not get too small, which can
        /// in turn lead to a large number of collections afterwards as the heap grows
        /// back to a usable size.
        ///
        /// If zero, defaults to 1MB.
        public ulong minHeapSize;

        /// Wren will resize the heap automatically as the number of bytes
        /// remaining in use after a collection changes. This number determines the
        /// amount of additional memory Wren will use after a collection, as a
        /// percentage of the current heap size.
        ///
        /// For example, say that this is 50. After a garbage collection, when there
        /// are 400 bytes of memory still in use, the next collection will be triggered
        /// after a total of 600 bytes are allocated (including the 400 already in
        /// use.)
        ///
        /// Setting this to a smaller number wastes less memory, but triggers more
        /// frequent garbage collections.
        ///
        /// If zero, defaults to 50.
        public int heapGrowthPercent;

        /// User-defined data associated with the VM.
        public IntPtr userData; //   void*

        public static Configuration DefaultConfiguration()
        {
            Configuration config = new Configuration();
            WrenInterop.wrenInitConfiguration(ref config);
            return config;
        }
    }
}

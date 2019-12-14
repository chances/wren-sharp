using System;

namespace Wren
{
    public delegate void ForeignMethodFn(VirtualMachine vm);
    public delegate ForeignMethodFn BindForeignMethodFn(VirtualMachine vm,
        string module, string className, bool isStatic, string signature);
    public delegate Nullable<ForeignClass> BindForeignClassFn(VirtualMachine vm,
        string module, string className);

    public struct ForeignClass
    {
        public Func<VirtualMachine, object> Allocate;
        public Action<object> Finalize;
    }

    public class Configuration
    {
        /// <summary>
        /// Callback function invoked when the Wren VM encounters a foreign method declaration.
        /// </summary>
        public BindForeignMethodFn BindForeignMethod { get; set; } = null;

        /// <summary>
        /// Callback function invoked when the Wren VM encounters a foreign class declaration.
        /// </summary>
        public BindForeignClassFn BindForeignClass { get; set; } = null;

        /// <summary>
        /// Whether the WrenErrorFn handler shall also raise a WrenException
        /// </summary>
        public bool RaiseExceptionOnError { get; set; } = false;
    }
}

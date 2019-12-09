using System;

namespace Wren
{
    public delegate void ForeignMethodFn(VirtualMachine vm);
    public delegate ForeignMethodFn BindForeignMethodFn(VirtualMachine vm,
        string module, string className, bool isStatic, string signature);
    public delegate ForeignObject BindForeignClassFn(VirtualMachine vm,
        string module, string className);

    public abstract class ForeignObject : IDisposable
    {
        public abstract object Allocate(VirtualMachine vm);

        public void Dispose() {}
    }

    public class Configuration
    {
        /// <summary>
        /// Callback function invoked when the Wren VM encounters a foreign method declaration.
        /// </summary>
        public BindForeignMethodFn BindForeignMethod { get; set; } = null;

        public BindForeignClassFn BindForeignClass { get; set; } = null;

        /// <summary>
        /// Whether the WrenWriteFn handler shall also write to System.Console
        /// </summary>
        public bool WriteToConsole { get; set; } = false;

        /// <summary>
        /// Whether the WrenErrorFn handler shall also raise a WrenException
        /// </summary>
        public bool RaiseExceptionOnError { get; set; } = false;
    }
}

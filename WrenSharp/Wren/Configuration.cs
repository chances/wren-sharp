using System;

namespace Wren
{
    /// <summary>
    /// A function callable from Wren code, but implemented in C#.
    /// </summary>
    /// <param name="vm"></param>
    public delegate void ForeignMethodFn(VirtualMachine vm);
    /// <summary>
    /// Returns a delegate to a foreign method on <paramref name="className"/> in
    /// <paramref name="module"/> with <paramref name="signature"/>.
    /// </summary>
    /// <param name="vm"></param>
    /// <param name="module"></param>
    /// <param name="className"></param>
    /// <param name="isStatic"></param>
    /// <param name="signature"></param>
    /// <returns></returns>
    public delegate ForeignMethodFn BindForeignMethodFn(VirtualMachine vm,
        string module, string className, bool isStatic, string signature);
    /// <summary>
    /// When a foreign class is declared, this will be called with the class's module and name when
    /// the class body is executed. It should return the foreign functions used to allocate and
    /// (optionally) finalize (i.e. Dispose) the foreign object when an instance is created.
    /// </summary>
    /// <param name="vm"></param>
    /// <param name="module"></param>
    /// <param name="className"></param>
    /// <returns></returns>
    public delegate Nullable<ForeignClass> BindForeignClassFn(VirtualMachine vm,
        string module, string className);

    /// <summary>
    /// Foreign functions used to allocate and (optionally) finalize (i.e. Dispose) a foreign
    /// object.
    /// </summary>
    public struct ForeignClass
    {
        /// <summary>
        /// The callback invoked when the foreign object is created.
        /// 
        /// This must be provided. The body of this  must call
        /// <see cref="VirtualMachine.SetSlotNewForeign"/> exactly once.
        /// </summary>
        public Func<VirtualMachine, object> Allocate;
        /// <summary>
        /// A finalizer function for freeing resources owned by an instance of a foreign class.
        /// Unlike most foreign methods, finalizers do not have access to the VM and should not
        /// interact with it since it's in the middle of a garbage collection.
        /// 
        /// This may be set to <c>null</c>> if the foreign class does not need to finalize.
        /// </summary>
        public Action<object> Finalize;
    }

    /// <summary>
    /// Options to configure the behavior of a <see cref="VirtualMachine"/>.
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Callback function invoked when the Wren VM encounters a foreign method declaration.
        /// 
        /// When a foreign method is declared in a class, this will be called with the foreign
        /// method's module, class, and signature when the class body is executed. It should
        /// return the foreign function that will be bound to that method.
        /// 
        /// If the foreign function could not be found, this should return <c>null</c>> and
        /// Wren will report it as runtime error.
        /// </summary>
        public BindForeignMethodFn BindForeignMethod { get; set; } = null;

        /// <summary>
        /// Callback function invoked when the Wren VM encounters a foreign class declaration.
        /// 
        /// When a foreign class is declared, this will be called with the class's module and name
        /// when the class body is executed. It should return the foreign functions used to
        /// allocate and (optionally) finalize (i.e. Dispose) the foreign object when an instance
        /// is created.
        /// </summary>
        public BindForeignClassFn BindForeignClass { get; set; } = null;

        /// <summary>
        /// Whether calls to <see cref="VirtualMachine.Interpret"/> and
        /// <see cref="VirtualMachine.Call"/> shall raise a <see cref="WrenException"/> on Wren
        /// errors.
        /// </summary>
        public bool RaiseExceptionOnError { get; set; } = false;
    }
}

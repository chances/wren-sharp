namespace Wren
{
    /// <summary>
    /// Result of the interpretation of Wren code either via <see cref="VirtualMachine.Interpret"/>
    /// or <see cref="VirtualMachine.Call"/>.
    /// </summary>
    public enum InterpretResult
    {
        /// Success
        WREN_RESULT_SUCCESS = 0,
        /// Error during Wren source compilation
        WREN_RESULT_COMPILE_ERROR = 1,
        /// Error during runtime interpretation of Wren code
        WREN_RESULT_RUNTIME_ERROR = 2
    }
}

namespace Wren
{
    /// <summary>
    /// Type of error raised by the Wren VM.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// A syntax or resolution error detected at compile time.
        /// </summary>
        WREN_ERROR_COMPILE = 0,

        /// <summary>
        /// The error message for a runtime error.
        /// </summary>
        WREN_ERROR_RUNTIME = 1,

        /// <summary>
        /// One entry of a runtime error's stack trace.
        /// </summary>
        WREN_ERROR_STACK_TRACE = 2
    }
}

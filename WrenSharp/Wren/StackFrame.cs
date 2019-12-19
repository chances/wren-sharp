namespace Wren
{
    /// <summary>
    /// A single, traceable frame of a Wren runtime error's stack trace.
    /// </summary>
    public class StackFrame
    {
        /// <param name="type"></param>
        /// <param name="module"></param>
        /// <param name="line"></param>
        /// <param name="message"></param>
        public StackFrame(ErrorType type, string module, int line, string message)
        {
            Type = type;
            Module = module;
            Line = line;
            Message = message;
        }

        /// <summary>
        /// Type of error raised by the Wren VM.
        /// </summary>
        public ErrorType Type { get; private set; }
        /// <summary>
        /// Name of the Wren module associated with this stack frame.
        /// </summary>
        public string Module { get; private set; }
        /// <summary>
        /// Line number inside the module associated with this stack frame.
        /// </summary>
        public int Line { get; private set; }
        /// <summary>
        /// Error message associated with this stack frame.
        /// </summary>
        public string Message { get; private set; }
    }
}

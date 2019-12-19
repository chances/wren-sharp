using System;
using System.Collections.Generic;

namespace Wren
{
    /// <summary>
    /// An error reported by the Wren VM.
    /// </summary>
    public class WrenException : Exception
    {
        /// <param name="type"></param>
        /// <param name="module"></param>
        /// <param name="line"></param>
        /// <param name="message"></param>
        public WrenException(ErrorType type, string module, int line, string message)
            : base(message)
        {
            Type = type;
            Module = module;
            Line = line;

            WrenStackTrace.Add(new StackFrame(type, module, line, message));
        }

        /// <summary>
        /// A runtime error's stack trace.
        /// </summary>
        public List<StackFrame> WrenStackTrace { get; } = new List<StackFrame>();
        /// <summary>
        /// Type of error raised by the Wren VM.
        /// </summary>
        public ErrorType Type { get; private set; }
        /// <summary>
        /// Name of the Wren module where the error occurred.
        /// </summary>
        public string Module { get; private set; }
        /// <summary>
        /// Line number inside the module where the error occurred.
        /// </summary>
        public int Line { get; private set; }

        internal void AddStackFrame(StackFrame stackFrame)
        {
            WrenStackTrace.Add(stackFrame);
        }
    }
}

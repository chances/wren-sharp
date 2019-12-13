using System;
using System.Collections.Generic;

namespace Wren
{
    public class WrenException : Exception
    {
        public WrenException(ErrorType type, string module, int line, string message)
            : base(message)
        {
            Type = type;
            Module = module;
            Line = line;

            WrenStackTrace.Add(new StackFrame(type, module, line, message));
        }

        public List<StackFrame> WrenStackTrace { get; } = new List<StackFrame>();
        public ErrorType Type { get; private set; }
        public string Module { get; private set; }
        public int Line { get; private set; }

        internal void AddStackFrame(StackFrame stackFrame)
        {
            WrenStackTrace.Add(stackFrame);
        }
    }
}

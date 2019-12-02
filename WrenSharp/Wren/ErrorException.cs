using System;

namespace Wren
{
    public class WrenException : Exception
    {
        public WrenException()
        {
        }

        public WrenException(string message)
            : base(message)
        {
        }

        public WrenException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public WrenException(ErrorType type, string module, int line, string message)
            : base(message)
        {
            Type = type;
            Module = module;
            Line = line;
        }

        public ErrorType Type { get; private set; }
        public string Module { get; private set; }
        public int Line { get; private set; }
    }
}

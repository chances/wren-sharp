namespace Wren
{
    public class StackFrame
    {
        public StackFrame(ErrorType type, string module, int line, string message)
        {
            Type = type;
            Module = module;
            Line = line;
            Message = message;
        }

        public ErrorType Type { get; private set; }
        public string Module { get; private set; }
        public int Line { get; private set; }
        public string Message { get; private set; }
    }
}

namespace Wren
{
    public class Configuration
    {
        /// Whether the WrenWriteFn handler shall also write to System.Console
        public bool WriteToConsole { get; set; } = false;

        /// Whether the WrenErrorFn handler shall also raise a WrenException
        public bool RaiseExceptionOnError { get; set; } = false;
    }
}

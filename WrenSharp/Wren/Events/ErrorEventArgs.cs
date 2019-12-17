using System;

namespace Wren
{
    /// <summary>
    /// An event handler for errors raised by the Wren VM as a result of a call to
    /// <see cref="VirtualMachine.Interpret"/> or <see cref="VirtualMachine.Call"/>.
    /// </summary>
    /// <param name="sender">VM where the error was raised</param>
    /// <param name="args">Reported error</param>
    public delegate void ErrorEventHandler(VirtualMachine sender, ErrorEventArgs args); 

    /// <summary>
    /// An error reported by the Wren VM as a result of a call to
    /// <see cref="VirtualMachine.Interpret"/> or <see cref="VirtualMachine.Call"/>.
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        /// <param name="type"></param>
        /// <param name="module"></param>
        /// <param name="line"></param>
        /// <param name="message"></param>
        public ErrorEventArgs(ErrorType type, string module, int line, string message)
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
        /// Name of the Wren module where the error occurred.
        /// </summary>
        public string Module { get; private set; }
        /// <summary>
        /// Line number inside the module where the error occurred.
        /// </summary>
        public int Line { get; private set; }
        /// <summary>
        /// Error message of the error raised by the Wren VM.
        /// </summary>
        public string Message { get; private set; }
    }
}

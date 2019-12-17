using System;

namespace Wren
{
    /// <summary>
    /// An event handler Wren uses to display text when <c>System.print()</c> or the other related
    /// Wren functions are called.
    /// </summary>
    /// <param name="sender">VM where the error was raised</param>
    /// <param name="args">Message written</param>
    public delegate void WriteEventHandler(VirtualMachine sender, WriteEventArgs args); 

    /// <summary>
    /// A message written when <c>System.print()</c> or the other related Wren functions are
    /// called.
    /// </summary>
    public class WriteEventArgs : EventArgs
    {
        /// <param name="text"></param>
        public WriteEventArgs(string text)
        {
            Text = text;
        }

        /// <summary>
        /// Text written by <c>System.print()</c> or the other related Wren functions
        /// </summary>
        public string Text { get; private set; }
    }
}

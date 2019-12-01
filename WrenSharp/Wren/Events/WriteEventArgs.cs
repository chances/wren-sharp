using System;

namespace Wren
{
    public delegate void WriteEventHandler(VirtualMachine sender, WriteEventArgs args); 

    public class WriteEventArgs : EventArgs
    {
        public WriteEventArgs(string text)
        {
            Text = text;
        }

        public string Text { get; private set; }
        public bool Handled { get; private set; }

        public void SetHandled()
        {
            Handled = true;
        }
    }
}

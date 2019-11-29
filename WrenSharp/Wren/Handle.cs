using System;
using System.Runtime.InteropServices;

namespace Wren
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Handle
    {
        ulong value;

        IntPtr prev;
        IntPtr next;
    }
}

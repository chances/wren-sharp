using System;
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace Wren
{
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    internal sealed class WrenHandleSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal VirtualMachine VirtualMachine { private get; set; }

        public WrenHandleSafeHandle() : base(ownsHandle: true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            if (VirtualMachine == null)
            {
                throw new InvalidOperationException("Wren handles must be associated with a Wren VM!");
            }
            WrenInterop.wrenReleaseHandle(VirtualMachine.Handle, handle);
            return true;
        }
    }

    public sealed class Handle : IDisposable
    {
        private WrenHandleSafeHandle _rawHandle;

        internal WrenHandleSafeHandle RawHandle => _rawHandle;

        /// <summary>
        /// Whether this Wren handle pointer is set and is not released.
        /// 
        /// Does not guarentee this is a valid reference into the Wren virtual machine!
        /// </summary>
        public bool HasReference => !_rawHandle.IsInvalid && !_rawHandle.IsClosed;

        /// <summary>
        /// Whether this Wren handle has been released.
        /// </summary>
        public bool IsReleased => _rawHandle.IsClosed;

        internal Handle(VirtualMachine vm, WrenHandleSafeHandle handle)
        {
            handle.VirtualMachine = vm;
            _rawHandle = handle;
        }

        public void Dispose()
        {
            _rawHandle.Dispose();
        }
    }
}

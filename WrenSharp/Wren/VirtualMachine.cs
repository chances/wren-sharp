using System;
using System.Runtime.InteropServices;

namespace Wren
{
    public class VirtualMachine : IDisposable
    {
        private WrenVmSafeHandle _handle;
        private Configuration _config;

        public VirtualMachine(Configuration config = null)
        {
            _config = config ?? new Configuration();
            var nativeConfig = Internal.Configuration.DefaultConfiguration();
            // TODO: Convert friendly config to native config
            WireEvents(ref nativeConfig);
            _handle = WrenInterop.wrenNewVM(ref nativeConfig);
        }

        /// Display text when `System.print()` or the other related functions are called.
        ///
        /// If unhandled, printed text is discarded.
        public event WriteEventHandler Write;
        public event ErrorEventHandler Error;

        public InterpretResult Interpret(string module, string source)
        {
            return WrenInterop.wrenInterpret(_handle, module, source);
        }

        public int GetSlotCount()
        {
            return WrenInterop.wrenGetSlotCount(_handle);
        }

        public void EnsureSlots(int numSlots)
        {
            WrenInterop.wrenEnsureSlots(_handle, numSlots);
        }

        public ValueType GetSlotType(int slot)
        {
            return WrenInterop.wrenGetSlotType(_handle, slot);
        }

        public bool GetSlotBool(int slot)
        {
            return WrenInterop.wrenGetSlotBool(_handle, slot);
        }

        public string GetSlotBytes(int slot, out int length)
        {
            // TODO: Figure out how to use byte[] here...
            throw new NotImplementedException();
            // int outLength = 0;
            // var bytes = WrenInterop.wrenGetSlotBytes(_handle, slot, ref outLength);
            // length = outLength;
            // return bytes;
        }

        public double GetSlotDouble(int slot)
        {
            return WrenInterop.wrenGetSlotDouble(_handle, slot);
        }

        public IntPtr GetSlotForeign(int slot)
        {
            throw new NotImplementedException();
            // TODO: WrenInterop.wrenGetSlotForeign(_handle, int slot);
        }

        public string GetSlotString(int slot)
        {
            return WrenInterop.wrenGetSlotString(_handle, slot);
        }

        public Handle GetSlotHandle(int slot)
        {
            throw new NotImplementedException();
            // TODO: return WrenInterop.wrenGetSlotHandle(_handle, int slot);
        }

        public void SetSlotBool(int slot, bool value)
        {
            WrenInterop.wrenSetSlotBool(_handle, slot, value);
        }

        public void SetSlotBytes(int slot, string bytes, uint length)
        {
            WrenInterop.wrenSetSlotBytes(_handle, slot, bytes, length);
        }

        public void SetSlotDouble(int slot, double value)
        {
            WrenInterop.wrenSetSlotDouble(_handle, slot, value);
        }

        public void SetSlotNewForeign(int slot, int classSlot, uint size)
        {
            throw new NotImplementedException();
            // TODO: WrenInterop.wrenSetSlotNewForeign(_handle, int slot, int classSlot, uint size);
        }

        public void SetSlotNewList(int slot)
        {
            throw new NotImplementedException();
            // TODO: WrenInterop.wrenSetSlotNewList(_handle, int slot);
        }

        public void SetSlotNull(int slot)
        {
            WrenInterop.wrenSetSlotNull(_handle, slot);
        }

        public void SetSlotString(int slot, string text)
        {
            WrenInterop.wrenSetSlotString(_handle, slot, text);
        }

        public void SetSlotHandle(int slot, Handle handle)
        {
            throw new NotImplementedException();
            // TODO: WrenInterop.wrenSetSlotHandle(_handle, int slot, WrenHandle* handle);
        }

        public int GetListCount(int slot)
        {
            return WrenInterop.wrenGetListCount(_handle, slot);
        }

        public void GetListElement(int listSlot, int index, int elementSlot)
        {
            throw new NotImplementedException();
            // TODO: WrenInterop.wrenGetListElement(_handle, int listSlot, int index, int elementSlot);
        }

        public void InsertInList(int listSlot, int index, int elementSlot)
        {
            throw new NotImplementedException();
            // TODO: WrenInterop.wrenInsertInList(_handle, int listSlot, int index, int elementSlot);
        }

        public void AbortFiber(int slot)
        {
            WrenInterop.wrenAbortFiber(_handle, slot);
        }

        private void WireEvents(ref Internal.Configuration config)
        {
            config.writeFn = (_, text) => OnWrite(text);
            config.errorFn = (_, type, module, line, message) => OnError(type, module, line, message);
            // TODO: Use events for the rest of config's callbacks (resolveModuleFn, loadModuleFn, bindForeignMethodFn, bindForeignClassFn, errorFn)
        }

        private void OnWrite(string text)
        {
            var wasHandled = false;

            if (Write != null)
            {
                var args = new WriteEventArgs(text);
                Write(this, args);
                wasHandled = args.Handled;
            }

            if (!wasHandled && _config.WriteToConsole)
            {
                System.Console.Write(text);
            }
        }

        private void OnError(ErrorType type, string module, int line, string message)
        {
            if (Error != null)
            {
                var args = new ErrorEventArgs(type, module, line, message);
                Error(this, args);
            }

            if (_config.RaiseExceptionOnError)
            {
                throw new WrenException(type, module, line, message);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                _handle.Dispose();

                disposedValue = true;
            }
        }

        ~VirtualMachine()
        {
          // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
          Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

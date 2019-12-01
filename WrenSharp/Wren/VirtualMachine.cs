using System;
using System.Runtime.InteropServices;

namespace Wren
{
    public class VirtualMachine : IDisposable
    {
        private WrenVmSafeHandle _handle;
        private Configuration _config;

        public VirtualMachine(Configuration config)
        {
            _config = config;
            var nativeConfig = Internal.Configuration.DefaultConfiguration();
            // TODO: Convert friendly config to native config
            WireEvents(ref nativeConfig);
            _handle = WrenInterop.wrenNewVM(ref nativeConfig);
        }

        /// Display text when `System.print()` or the other related functions are called.
        ///
        /// If unhandled, printed text is discarded.
        public event WriteEventHandler Write;

        public InterpretResult Interpret(string module, string source)
        {
            return WrenInterop.wrenInterpret(_handle, module, source);
        }

        private void WireEvents(ref Internal.Configuration config)
        {
            config.writeFn = (_, text) => OnWrite(text);
            // TODO: Use events for the rest of config's callbacks (resolveModuleFn, loadModuleFn, bindForeignMethodFn, bindForeignClassFn, errorFn)
        }

        private void OnWrite(string text)
        {
            var wasHandled = false;

            if (Write != null)
            {
                WriteEventArgs args = new WriteEventArgs(text);
                Write(this, args);
                wasHandled = args.Handled;
            }

            if (!wasHandled && _config.WriteToConsole)
            {
                System.Console.Write(text);
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

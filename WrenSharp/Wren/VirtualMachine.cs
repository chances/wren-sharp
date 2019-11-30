namespace Wren
{
    public class VirtualMachine
    {
        private WrenVmSafeHandle _handle;

        public VirtualMachine(Configuration config)
        {
            _handle = WrenInterop.wrenNewVM(ref config);
        }

        public InterpretResult Interpret(string module, string source)
        {
            return WrenInterop.wrenInterpret(_handle, module, source);
        }
    }
}

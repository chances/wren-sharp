using System.Text;
using Wren;
using Xunit;

namespace WrenSharp.Tests
{
    // http://wren.io/embedding/slots-and-handles.html#handles
    public class WrenHandles
    {
        [Fact]
        public void GetVariableHandle()
        {
            var vm = new VirtualMachine();
            var result = vm.Interpret("vars", "var foo = \"bar\"");
            Assert.True(result == InterpretResult.WREN_RESULT_SUCCESS);

            // Sequester some slots
            var expectedNumSlots = 1;
            vm.EnsureSlots(expectedNumSlots);
            var numSlots = vm.GetSlotCount();
            Assert.True(numSlots == expectedNumSlots);

            vm.GetVariable("vars", "foo", 0);
            Assert.True(vm.GetSlotType(0) == ValueType.WREN_TYPE_STRING);

            var handle = vm.GetSlotHandle(0);
            Assert.True(handle.HasReference, "Handle has reference");

            // Invalidate the currently sequestered slots
            result = vm.Interpret("vars", "var fizz = \"buzz\"");
            Assert.True(result == InterpretResult.WREN_RESULT_SUCCESS);

            // Sequester some slots
            vm.EnsureSlots(expectedNumSlots);
            numSlots = vm.GetSlotCount();
            Assert.True(numSlots >= expectedNumSlots);

            var expectedFirstSlot = "bar";

            vm.SetSlotHandle(0, handle);
            Assert.True(vm.GetSlotType(0) == ValueType.WREN_TYPE_STRING);
            Assert.True(expectedFirstSlot == vm.GetSlotString(0));

            handle.Dispose();
            Assert.True(handle.IsReleased, "Handle to string variable was released");

            vm.Dispose();
        }
    }
}

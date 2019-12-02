using Wren;
using Xunit;

namespace WrenSharp.Tests
{
    // http://wren.io/embedding/slots-and-handles.html
    public class WrenSlotsAndHandles
    {
        [Fact]
        public void EnsureSlots()
        {
            var expectedNumSlots = 3;

            var vm = new VirtualMachine();
            vm.EnsureSlots(expectedNumSlots);
            var numSlots = vm.GetSlotCount();
            Assert.True(numSlots == expectedNumSlots);
            vm.Dispose();
        }

        [Fact]
        public void SetPrimitiveValuesToSlots()
        {
            var expectedNumSlots = 2;
            var expectedFirstSlot = false;
            var expectedThirdSlot = 14.5d;

            var vm = new VirtualMachine();

            // Sequester some slots
            vm.EnsureSlots(expectedNumSlots);
            var numSlots = vm.GetSlotCount();
            Assert.True(numSlots == expectedNumSlots);

            // Set and then assert slots' values
            vm.SetSlotBool(0, expectedFirstSlot);
            vm.SetSlotNull(1);
            vm.SetSlotDouble(2, expectedThirdSlot);

            Assert.True(expectedFirstSlot == vm.GetSlotBool(0));
            Assert.True(vm.GetSlotType(1) == ValueType.WREN_TYPE_NULL);
            Assert.True(expectedThirdSlot == vm.GetSlotDouble(2));

            vm.Dispose();
        }
    }
}

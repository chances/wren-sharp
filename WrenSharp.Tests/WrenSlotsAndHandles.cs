using System.Text;
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
            var expectedNumSlots = 3;
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

        [Fact]
        public void SetStringsToSlots()
        {
            var expectedNumSlots = 2;
            var expectedFirstSlot = "foo";
            var expectedSecondSlot = "The quick brown fox";

            var vm = new VirtualMachine();

            // Sequester some slots
            vm.EnsureSlots(expectedNumSlots);
            var numSlots = vm.GetSlotCount();
            Assert.True(numSlots == expectedNumSlots);

            // Set and then assert slots' values
            vm.SetSlotString(0, expectedFirstSlot);
            vm.SetSlotString(1, expectedSecondSlot);

            Assert.Matches(expectedFirstSlot, vm.GetSlotString(0));
            Assert.Matches(expectedSecondSlot, vm.GetSlotString(1));

            vm.Dispose();
        }

        [Fact]
        public void SetByteArrayToSlot()
        {
            var expectedNumSlots = 1;
            var expectedString = "The quick brown fox";
            var expectedSlot = Encoding.UTF8.GetBytes(expectedString);
            var expectedBytesLength = expectedSlot.Length;

            var vm = new VirtualMachine();

            // Sequester some slots
            vm.EnsureSlots(expectedNumSlots);
            var numSlots = vm.GetSlotCount();
            Assert.True(numSlots == expectedNumSlots);

            // Set and then assert slots' values
            vm.SetSlotBytes(0, ref expectedSlot);
            Assert.True(vm.GetSlotType(0) == ValueType.WREN_TYPE_STRING);

            var bytes = vm.GetSlotBytes(0);
            Assert.True(bytes.Length == expectedBytesLength);
            var bytesAsString = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            Assert.Matches(expectedString, bytesAsString);

            vm.Dispose();
        }

        [Fact]
        public void LookupVariable()
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
            Assert.Matches("bar", vm.GetSlotString(0));

            vm.Dispose();
        }

        [Fact]
        public void InsertListItems()
        {
            var expectedNumSlots = 2;
            var expectedFirstItem = 2.5d;
            var expectedSecondItem = 14.5d;

            var vm = new VirtualMachine();
            var result = vm.Interpret("lists", "var foo = []");
            Assert.True(result == InterpretResult.WREN_RESULT_SUCCESS);

            // Sequester some slots
            vm.EnsureSlots(expectedNumSlots);
            var numSlots = vm.GetSlotCount();
            Assert.True(numSlots == expectedNumSlots);

            vm.GetVariable("lists", "foo", 0);
            Assert.True(vm.GetSlotType(0) == ValueType.WREN_TYPE_LIST);
            vm.SetSlotDouble(1, expectedFirstItem);
            vm.InsertInList(0, -1, 1);
            vm.SetSlotDouble(1, expectedSecondItem);
            vm.InsertInList(0, -1, 1);
            Assert.True(vm.GetListCount(0) == expectedNumSlots);

            vm.Dispose();
        }

        [Fact]
        public void GetListItems()
        {
            var expectedNumSlots = 2;
            var expectedFirstItem = 2.5d;
            var expectedSecondItem = "foobar";

            var vm = new VirtualMachine();
            var result = vm.Interpret("lists", "var foo = [2.5, \"foobar\"]");
            Assert.True(result == InterpretResult.WREN_RESULT_SUCCESS);

            // Sequester some slots
            vm.EnsureSlots(expectedNumSlots);
            var numSlots = vm.GetSlotCount();
            Assert.True(numSlots == expectedNumSlots);

            vm.GetVariable("lists", "foo", 0);
            Assert.True(vm.GetSlotType(0) == ValueType.WREN_TYPE_LIST);

            vm.GetListElement(0, 0, 1);
            Assert.True(vm.GetSlotType(1) == ValueType.WREN_TYPE_NUM);
            Assert.True(vm.GetSlotDouble(1) == expectedFirstItem);

            vm.GetListElement(0, 1, 1);
            Assert.True(vm.GetSlotType(1) == ValueType.WREN_TYPE_STRING);
            Assert.True(vm.GetSlotString(1) == expectedSecondItem);

            vm.Dispose();
        }
    }
}

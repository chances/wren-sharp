using System;
using Xunit;
using Wren;
using System.Text;

namespace WrenSharp.Tests
{
    // https://lukewickstead.wordpress.com/2013/01/16/xunit-cheat-sheet/

    public class WrenVirtualMachine
    {
        [Fact]
        public void InitWrenVM()
        {
            var vm = new VirtualMachine(new Configuration());
            var result = vm.Interpret("my_module", "System.print(\"I am running in a VM!\")");
            Assert.True(result == InterpretResult.WREN_RESULT_SUCCESS);
            vm.Dispose();
        }

        [Fact]
        public void WrenWriteFn()
        {
            var expectedText = "I am running in a VM!";

            var vm = new VirtualMachine(new Configuration());
            var text = new StringBuilder();
            vm.Write += (_vm, writtenText) => text.Append(writtenText.Text);
            var result = vm.Interpret("my_module", $"System.print(\"{expectedText}\")");
            Assert.True(result == InterpretResult.WREN_RESULT_SUCCESS);
            Assert.Matches(expectedText + "\n", text.ToString());
            vm.Dispose();
        }
    }
}

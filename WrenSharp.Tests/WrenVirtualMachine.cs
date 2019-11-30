using System;
using Xunit;
using Wren;

namespace WrenSharp.Tests
{
    // https://lukewickstead.wordpress.com/2013/01/16/xunit-cheat-sheet/

    public class WrenVirtualMachine
    {
        [Fact]
        public void InitWrenVM()
        {
            var vm = new VirtualMachine(Configuration.DefaultConfiguration());
            var result = vm.Interpret("my_module", "System.print(\"I am running in a VM!\")");
            Assert.True(result == InterpretResult.WREN_RESULT_SUCCESS);
        }
    }
}

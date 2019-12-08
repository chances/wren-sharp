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
            var vm = new VirtualMachine();
            var result = vm.Interpret("my_module", "System.print(\"I am running in a VM!\")");
            Assert.True(result == InterpretResult.WREN_RESULT_SUCCESS);
            vm.Dispose();
        }

        [Fact]
        public void WrenWriteFn()
        {
            var expectedText = "I am running in a VM!";

            var vm = new VirtualMachine();
            var text = new StringBuilder();
            vm.Write += (_vm, writtenText) => text.Append(writtenText.Text);
            var result = vm.Interpret("my_module", $"System.print(\"{expectedText}\")");
            Assert.True(result == InterpretResult.WREN_RESULT_SUCCESS);
            Assert.StartsWith(expectedText, text.ToString());

            vm.Dispose();
        }

        [Fact]
        public void WrenErrorFn()
        {
            var expectedError = "my_module(1): Error at 'foo': Variable is used but not defined.";

            var vm = new VirtualMachine();
            var text = new StringBuilder();
            vm.Error += (_vm, error) => text.Append($"{error.Module}({error.Line}): {error.Message}");
            var result = vm.Interpret("my_module", "foo");
            Assert.True(result == InterpretResult.WREN_RESULT_COMPILE_ERROR, "VM compile-time error");
            Assert.StartsWith(expectedError, text.ToString());

            vm.Dispose();
        }

        [Fact]
        public void WrenThrowsOnError()
        {
            var expectedErrorModule = "my_module";
            var expectedErrorLine = 1;
            var expectedError = "Error at 'foo': Variable is used but not defined.";

            var vm = new VirtualMachine(new Configuration
            {
                RaiseExceptionOnError = true
            });

            var ex = Assert.Throws<WrenException>(() => { vm.Interpret(expectedErrorModule, "foo"); });
            Assert.True(ex.Type == ErrorType.WREN_ERROR_COMPILE, "VM compile-time error");
            Assert.True(ex.Module == expectedErrorModule, $"Error in module '{expectedErrorModule}'");
            Assert.True(ex.Line == expectedErrorLine, "Error on line one");
            Assert.StartsWith(expectedError, ex.Message);

            vm.Dispose();
        }
    }
}

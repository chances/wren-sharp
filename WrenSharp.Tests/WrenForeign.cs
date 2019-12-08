using Wren;
using Xunit;

namespace WrenSharp.Tests
{
    // http://wren.io/embedding/calling-c-from-wren.html
    public class WrenForeign
    {
        private BindForeignMethodFn foreignMethodBinder = (_, module, className, isStatic, signature) =>
        {
            if (module == "math" && className == "BindMath" && isStatic && signature == "add(_,_)")
            {
                ForeignMethodFn bindMathAdd = (vm) =>
                {
                    var a = vm.GetSlotDouble(1);
                    var b = vm.GetSlotDouble(2);
                    vm.SetSlotDouble(0, a + b);
                };
                return bindMathAdd;
            }

            return null; // Return a no-op for unmatched foreign methods
        };

        [Fact]
        public void WrenBindForeignMethodNoOp()
        {
            var vm = new VirtualMachine(new Configuration
            {
                RaiseExceptionOnError = true,
                BindForeignMethod = foreignMethodBinder
            });

            vm.Interpret("math", @"class BindMath {
  foreign static sum(a, b) 
}

var foreignSum = 0.0

class DoMath {
  static sum() {
    return BindMath.sum(" + 1.5 + "," + 3 + @")
  }
}");

            vm.Interpret("math", "foreignSum = DoMath.sum()");

            vm.EnsureSlots(1);
            vm.GetVariable("math", "foreignSum", 0);
            Assert.True(vm.GetSlotType(0) == ValueType.WREN_TYPE_UNKNOWN, "Sum method return value is unknown");
        }

        [Fact]
        public void WrenBindForeignMethod()
        {
            var a = 4.5d;
            var b = 8d;
            var expectedSum = a + b;

            var vm = new VirtualMachine(new Configuration
            {
                RaiseExceptionOnError = true,
                BindForeignMethod = foreignMethodBinder
            });

            vm.Interpret("math", @"class BindMath {
  foreign static add(a, b) 
}

var foreignSum = 0.0

class DoMath {
  static sum() {
    return BindMath.add(" + a + "," + b + @")
  }
}");

            vm.Interpret("math", "foreignSum = DoMath.sum()");

            vm.EnsureSlots(1);
            vm.GetVariable("math", "foreignSum", 0);
            var slotType = vm.GetSlotType(0);
            Assert.True(slotType == ValueType.WREN_TYPE_NUM, "Sum method return value is a number");
            Assert.True(vm.GetSlotDouble(0) == expectedSum, "Sum method return value matches expected sum");
        }
    }
}

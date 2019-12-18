using Wren;
using Xunit;

namespace WrenSharp.Tests
{
    public class ForeignObjectEdgeCases
    {
        [Fact]
        public void ForeignObjectMethodAcceptsGenericObjectArgument()
        {
            var classModule = "boolBot";

            var vm = new Wren.VirtualMachine(new Configuration
            {
                RaiseExceptionOnError = true
            });
            vm.BindForeign<BoolBot>(classModule);
            vm.Interpret(classModule, @"foreign class BoolBot { 
  construct new() {}

  foreign and(a,b) 
  foreign not(a) 
}");

            vm.Interpret(classModule, "var bot = BoolBot.new()");

            vm.EnsureSlots(3);
            vm.GetVariable(classModule, "bot", 0);
            var andHandle = vm.MakeCallHandle("and(_,_)");
            vm.SetSlotBool(1, false);
            vm.SetSlotBool(2, true);
            vm.Call(andHandle);

            Assert.True(vm.GetSlotType(0) == ValueType.WREN_TYPE_BOOL, "BoolBot.and result is a bool value");
            Assert.True(vm.GetSlotBool(0) == false, "result is false");
            andHandle.Dispose();

            vm.EnsureSlots(2);
            vm.GetVariable(classModule, "bot", 0);
            var notHandle = vm.MakeCallHandle("not(_)");
            vm.SetSlotBool(1, false);
            vm.Call(notHandle);

            Assert.True(vm.GetSlotType(0) == ValueType.WREN_TYPE_BOOL, "BoolBot.not result is a bool value");
            Assert.True(vm.GetSlotBool(0) == true, "result is true");
            notHandle.Dispose();

            vm.Dispose();
        }
    }

    internal class BoolBot : Wren.ForeignObject
    {
        public BoolBot() {}

        public bool And(bool a, object b)
        {
            if (b is bool bAsBool)
            {
                return a && bAsBool;
            }

            return false;
        }

        public bool Not(object a)
        {
            if (a is bool aAsBool)
            {
                return !aAsBool;
            }

            return a == null;
        }
    }
}

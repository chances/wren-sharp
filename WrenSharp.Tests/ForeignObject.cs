using System.IO;
using System.Text;
using Wren;
using Wren.Attributes;
using Xunit;

namespace WrenSharp.Tests
{
    public class ForeignObject
    {
        [Fact]
        public void BindFileClass()
        {
            var classModule = "io";

            var vm = new Wren.VirtualMachine(new Configuration
            {
                RaiseExceptionOnError = true
            });
            vm.BindForeign<File>(classModule);
            vm.Interpret(classModule, @"foreign class File { 
  construct create(path) {}

  foreign write(text) 
  foreign close() 
}");

            vm.Interpret(classModule, "var file = File.create(\"/tmp/wren_BindFileClass.txt\")");
            vm.Interpret(classModule, "file.write(\"wren script\")");

            vm.Dispose();
        }

        [Fact]
        public void BindFileClassMethodTypeMismatch()
        {
            var classModule = "io";

            var vm = new Wren.VirtualMachine(new Configuration
            {
                RaiseExceptionOnError = true
            });
            vm.BindForeign<File>(classModule);
            vm.Interpret(classModule, @"foreign class File { 
  construct create(path) {}

  foreign write(text) 
  foreign close() 
}");

            vm.Interpret(classModule, "var file = File.create(\"/tmp/wren_BindFileClassMethodTypeMismatch.txt\")");
            var ex = Assert.Throws<WrenException>(() => vm.Interpret(classModule, "file.write(75)"));
            Assert.StartsWith(ex.Message, "Foreign method 'Write' parameter 'buffer' type mismatch given actual parameter of type System.Double (75 in slot 1), expected type String");

            vm.Dispose();
        }

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

    internal class File : Wren.ForeignObject
    {
        private System.IO.FileStream _file;

        public File(string fileName)
        {
            _file = System.IO.File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        public void Write(string buffer)
        {
            if (_file == null || !_file.CanWrite) return;

            _file.Write(Encoding.UTF8.GetBytes(buffer));
            _file.Flush();
        }

        public void Close()
        {
            Dispose();
        }

        [WrenIgnore]
        public override void Dispose()
        {
            if (_file == null) return;

            _file.Flush();
            _file.Dispose();
            _file = null;
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

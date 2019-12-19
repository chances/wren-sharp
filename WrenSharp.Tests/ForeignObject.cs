using System;
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
        public void ForeignObjectMethodAcceptsOddArguments()
        {
            var classModule = "boolBot";

            var vm = new Wren.VirtualMachine(new Configuration
            {
                RaiseExceptionOnError = true
            });
            vm.BindForeign<MathBot>(classModule);
            vm.Interpret(classModule, @"foreign class MathBot {
  construct new() {}

  foreign add(a,b)
  foreign addInts(a,b)
  foreign addStrings(a,b)
  foreign and(a,b)
  foreign not(a)
  foreign badActualParam(a)
}");

            vm.Interpret(classModule, "var bot = MathBot.new()");

            #region MathBot.add
            vm.EnsureSlots(3);
            vm.GetVariable(classModule, "bot", 0);
            var addHandle = vm.MakeCallHandle("add(_,_)");
            vm.SetSlot(1, 1);
            vm.SetSlot(2, 2.3d);
            vm.Call(addHandle);

            Assert.True(vm.GetSlotType(0) == Wren.ValueType.WREN_TYPE_NUM, "MathBot.add result is a number value");
            Assert.Equal(3.3d, vm.GetSlotDouble(0));
            #endregion

            #region MathBot.addInts
            vm.EnsureSlots(3);
            vm.GetVariable(classModule, "bot", 0);
            var addIntsHandle = vm.MakeCallHandle("addInts(_,_)");
            vm.SetSlot(1, 4);
            vm.SetSlot(2, 5.5);
            vm.Call(addIntsHandle);

            Assert.True(vm.GetSlotType(0) == Wren.ValueType.WREN_TYPE_NUM, "MathBot.addInts result is a number value");
            Assert.Equal(9, vm.GetSlotDouble(0));
            #endregion

            #region MathBot.addStrings
            vm.EnsureSlots(3);
            vm.GetVariable(classModule, "bot", 0);
            var addStringsHandle = vm.MakeCallHandle("addStrings(_,_)");

            vm.EnsureSlots(3);
            vm.GetVariable(classModule, "bot", 0);
            vm.SetSlot(1, 4.5);
            vm.SetSlot(2, "foo");
            vm.Call(addStringsHandle);
            Assert.True(vm.GetSlotType(0) == Wren.ValueType.WREN_TYPE_STRING, "MathBot.addStrings result is a string value");
            Assert.Equal("4.5foo", vm.GetSlotString(0));

            vm.EnsureSlots(3);
            vm.GetVariable(classModule, "bot", 0);
            vm.SetSlot(1, false);
            vm.SetSlot(2, 42);
            vm.Call(addStringsHandle);
            Assert.True(vm.GetSlotType(0) == Wren.ValueType.WREN_TYPE_STRING, "MathBot.addStrings result is a string value");
            Assert.Equal("False42", vm.GetSlotString(0));
            #endregion

            #region MathBot.and
            vm.EnsureSlots(3);
            vm.GetVariable(classModule, "bot", 0);
            var andHandle = vm.MakeCallHandle("and(_,_)");
            vm.SetSlot(1, false);
            vm.SetSlot(2, true);
            vm.Call(andHandle);

            Assert.True(vm.GetSlotType(0) == Wren.ValueType.WREN_TYPE_BOOL, "MathBot.and result is a bool value");
            Assert.Equal(false, vm.GetSlotBool(0));
            #endregion

            #region MathBot.not
            vm.EnsureSlots(2);
            vm.GetVariable(classModule, "bot", 0);
            var notHandle = vm.MakeCallHandle("not(_)");
            vm.SetSlot(1, false);
            vm.Call(notHandle);

            Assert.True(vm.GetSlotType(0) == Wren.ValueType.WREN_TYPE_BOOL, "MathBot.not result is a bool value");
            Assert.Equal(true, vm.GetSlotBool(0));
            #endregion

            #region MathBot.badActualParam
            vm.EnsureSlots(2);
            vm.GetVariable(classModule, "bot", 0);
            var badActualParamHandle = vm.MakeCallHandle("badActualParam(_)");
            vm.SetSlot(1, 4.5d);

            var ex = Assert.Throws<WrenException>(() => vm.Call(badActualParamHandle));
            Assert.StartsWith("Foreign method 'BadActualParam' parameter 'a' type mismatch given actual parameter of type System.Double (4.5 in slot 1", ex.Message);

            vm.EnsureSlots(2);
            vm.GetVariable(classModule, "bot", 0);
            vm.SetSlot(1, badActualParamHandle);

            ex = Assert.Throws<WrenException>(() => vm.Call(badActualParamHandle));
            Assert.StartsWith("Foreign method 'BadActualParam' parameter 'a' type mismatch given actual parameter of type Unknown (Unknown in slot 1", ex.Message);
            #endregion

            addHandle.Dispose();
            addIntsHandle.Dispose();
            addStringsHandle.Dispose();
            andHandle.Dispose();
            notHandle.Dispose();
            badActualParamHandle.Dispose();
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

    internal class MathBot : Wren.ForeignObject
    {
        public MathBot() {}

        public double Add(int a, double b)
        {
            return ((double) a) + b;
        }

        public int AddInts(int a, object b)
        {
            if (b is int bAsInt)
            {
                return a + bAsInt;
            }
            else if (b is double bAsDouble)
            {
                return (int) (a + bAsDouble);
            }

            // Else, fail the method with an error
            this.AbortFiber("Expected a number as second parameter");
            return 0;
        }

        public string AddStrings(object a, object b)
        {
            return $"{a}{b}";
        }

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

        public void BadActualParam(bool a) {}

        public void ThisIsNotBoundBecauseOfGenericParam(Nullable<double> a) {}
    }
}

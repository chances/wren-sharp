using System;
using System.IO;
using System.Text;
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
            Assert.True(vm.GetSlotType(0) == Wren.ValueType.WREN_TYPE_UNKNOWN, "Sum method return value is unknown");
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
            Assert.True(slotType == Wren.ValueType.WREN_TYPE_NUM, "Sum method return value is a number");
            Assert.True(vm.GetSlotDouble(0) == expectedSum, "Sum method return value matches expected sum");
        }

        [Fact]
        public void WrenBindForeignClass()
        {
            var vm = new VirtualMachine(new Configuration
            {
                RaiseExceptionOnError = true,
                BindForeignClass = (_, module, className) =>
                {
                    if (module == "io" && className == "File")
                    {
                        return new ForeignClass
                        {
                            Allocate = (vm) =>
                            {
                                if (vm.GetSlotType(1) != Wren.ValueType.WREN_TYPE_STRING)
                                {
                                    throw new ArgumentException();
                                }
                                var file = File.Open(vm.GetSlotString(1), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                                return file;
                            },
                            Finalize = (file) =>
                            {
                                if (file != null && file is FileStream fileStream && fileStream.CanWrite)
                                {
                                    fileStream.Dispose();
                                }
                            }
                        };
                    }

                    return null;
                },
                BindForeignMethod = (_, module, className, isStatic, signature) =>
                {
                    if (module == "io" && className == "File" && !isStatic && signature == "write(_)")
                    {
                        return (vm) =>
                        {
                            var file = vm.GetSlotForeign(0);
                            if (file == null)
                            {
                                vm.SetSlotString(0, "Cannot write to a closed file.");
                                vm.AbortFiber(0);
                            }
                            else if (file is FileStream fileStream && fileStream.CanWrite && vm.GetSlotType(1) == Wren.ValueType.WREN_TYPE_STRING)
                            {
                                fileStream.Write(Encoding.UTF8.GetBytes(vm.GetSlotString(1)));
                                fileStream.Flush();
                            }
                        };
                    }

                    if (module == "io" && className == "File" && !isStatic && signature == "close()")
                    {
                        return (vm) =>
                        {
                            var file = vm.GetSlotForeign(0);
                            if (file != null && file is FileStream fileStream && fileStream.CanWrite)
                            {
                                fileStream.Flush();
                                fileStream.Dispose();
                                vm.SetSlotForeign(0, null);
                            }
                        };
                    }

                    return null;
                }
            });

            vm.Interpret("io", @"foreign class File { 
  construct create(path) {}

  foreign write(text) 
  foreign close() 
}");
            //Then
            vm.Interpret("io", @"var file = File.create(""/tmp/wren.txt"") 
file.write(""some text"") 
file.close()");

            vm.Dispose();
        }
    }
}

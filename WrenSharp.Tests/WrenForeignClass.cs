using System;
using System.IO;
using System.Text;
using Wren;
using Xunit;

namespace WrenSharp.Tests
{
    public class WrenForeignClass
    {
        [Fact]
        public void WrenBindForeignClass()
        {
            var vm = InitVmWithFileClass("io");

            vm.Interpret("io", @"var file = File.create(""/tmp/wren.txt"")
file.write(""some text"") 
file.close()");
            // Test passes if code runs without error

            vm.Dispose();
        }

        [Fact]
        public void WrenBindForeignClassAbortFiber()
        {
            var vm = InitVmWithFileClass("io");

            var runtimeError = Assert.Throws<WrenException>(() =>
            {
                vm.Interpret("io", @"var file = File.create(""/tmp/wren.txt"") 
file.close()
file.write(""some text"")");
            });
            Assert.True(runtimeError.Type == ErrorType.WREN_ERROR_RUNTIME, "Runtime error write after close");
            Assert.StartsWith(runtimeError.Message, "Cannot write to a closed file.");
            Assert.True(runtimeError.WrenStackTrace.Count > 1, "Runtime error has stack trace");
            Assert.True(runtimeError.WrenStackTrace[1].Line == 3, "Trace error to line 3 of script");

            vm.Dispose();
        }

        private VirtualMachine InitVmWithFileClass(string classModule)
        {
            var vm = new VirtualMachine(new Configuration
            {
                RaiseExceptionOnError = true,
                BindForeignClass = (_, module, className) =>
                {
                    if (module == classModule && className == "File")
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
                    if (module == classModule && className == "File" && !isStatic && signature == "write(_)")
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

                    if (module == classModule && className == "File" && !isStatic && signature == "close()")
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

            vm.Interpret(classModule, @"foreign class File { 
  construct create(path) {}

  foreign write(text) 
  foreign close() 
}");
            return vm;
        }
    }
}

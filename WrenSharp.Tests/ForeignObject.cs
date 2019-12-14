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

            vm.Interpret(classModule, "var file = File.create(\"/tmp/wren.txt\")");
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

            vm.Interpret(classModule, "var file = File.create(\"/tmp/wren.txt\")");
            var ex = Assert.Throws<WrenException>(() => vm.Interpret(classModule, "file.write(75)"));
            Assert.StartsWith(ex.Message, "Foreign method 'Write' type mismatch given formal parameter 1, expected type String");

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
}

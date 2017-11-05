using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CILDisassembler.Tests
{
    [TestClass]
    public class DissassemblerTestsGenerator
    {
        /// <summary>
        /// This is not a real test and instead writes data used by the actual
        /// tests. WARNING: This overwrites existing source files.
        /// 
        /// There's a lot of obvious potential to misuse this. The resulting
        /// disassembly needs to be manually verified otherwise it's just a
        /// long winded way to write Assert(1 == 1).
        /// 
        /// To test against the difference in compiler output between Release
        /// and Debug builds, there's two child projects. The TestMethods.cs
        /// must be kept identical between them.
        /// </summary>
        [TestMethod]
        public void WriteTestDataToProjectFiles()
        {
            foreach (var buildname in new[] { "Release", "Debug" })
            {
                var dllname = $"CILDisassembler.Tests.{buildname}.dll";
                var path = GetSourcePath();
                var destinationFile = $"Disassembler{buildname}Tests.cs";
                var destinationPath = Path.Combine(
                    Path.GetDirectoryName(path), destinationFile);

                var sb = new StringBuilder();
                var tests = new List<string>();

                var assemblyPath = Path.Combine(Path.GetDirectoryName(
                    GetType().Assembly.Location), dllname);

                if (!File.Exists(assemblyPath))
                {
                    Assert.Fail($"Unable to locate '{assemblyPath}'");
                }

                var assembly = Assembly.LoadFile(assemblyPath);

                foreach (var methodname in TestMethodAccess.MethodNames)
                {
                    sb.Clear();

                    var method = TestMethodAccess
                        .MethodByName(assembly, methodname);
                    Disassembler.Disassemble(method,
                        DisassemblerTests.DisassemblerOptions, sb);

                    tests.Add($@"            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, ""{methodname}""),
                    @""{sb.Replace("\"", "\"\"")}"", ""{methodname} {buildname}"");

");
                }

                var filecontents = $@"using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CILDisassembler.Tests
{{
    [TestClass]
    public class Disassembler{buildname}Tests : DisassemblerTests
    {{
        [TestMethod]
        public void Ensure{buildname}DissassemblyProducesExpectedOutput()
        {{
            var assemblyPath = Path.Combine(Path.GetDirectoryName(
                GetType().Assembly.Location), ""{dllname}"");
            var assembly = Assembly.LoadFile(assemblyPath);

{string.Join("\r\n", tests)}
        }}
    }}
}}";

                Console.WriteLine($"Writing output to '{destinationPath}'");
                File.WriteAllText(destinationPath, filecontents);
            }
        }

        private static string GetSourcePath([CallerFilePath]string path = "")
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(
                    nameof(path), "Unable to get path of source file.");
            }

            return path;
        }
    }
}

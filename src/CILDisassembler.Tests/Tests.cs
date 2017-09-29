using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CILDisassembler.Tests
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void TestMethodSanityCheck()
        {
            try
            {
                AssertDisassembly(
                    LocalMethod(nameof(EmptyMethod)),
                    @"Expected failure");

                Assert.Fail("Sanity check did not pass.");
            }
            catch (AssertFailedException)
            {
            }

            AssertNormalized(@"
                    nop
                    ret
            ", "nop\nret");

            try
            {
                AssertNormalized(@"
                    nop
                    ret
                ", "nop\nxret");
            }
            catch (AssertFailedException)
            {
            }
        }

        [TestMethod]
        public void EnsureDissassemblyProducesExpectedOutput()
        {
            // Doing these tests against methods inside the code is extremely
            // sloppy because it errors or succeeds depending on if it's
            // a Release or Debug build, and further can break from compiler
            // changes.
            // TODO: Grab a random subset of byte code from the runtime and
            // use that.
            AssertDisassembly(
                LocalMethod(nameof(EmptyMethod)),
                @"nop
                    ret");

            AssertDisassembly(
                LocalMethod(nameof(AdditionMethod)),
                @"nop
ldc.i4 13092608
stloc.0
ldloc.0
ldc.i4.5
shl
stloc.1
br.s 0
ldloc.1
ret");
        }

        private static void AssertDisassembly(
            MethodInfo method, string expected)
        {
            var sb = new StringBuilder();
            Disassembler.Disassemble(method, sb);
            AssertNormalized(expected, sb.ToString());
        }

        private static readonly Regex _trimLeadingSpacesExp =
            new Regex(@"^\s+", RegexOptions.Multiline);

        private static void AssertNormalized(string expected, string actual)
        {
            Assert.AreEqual(NormalizeString(expected), NormalizeString(actual));
        }

        private static string NormalizeString(string s)
        {
            return _trimLeadingSpacesExp
                .Replace(s.Replace("\r\n", "\n").Trim(), "");
        }

        private static MethodInfo LocalMethod(string name)
        {
            return typeof(Tests).GetMethod(
                name, BindingFlags.Static | BindingFlags.NonPublic);
        }

        private static void EmptyMethod()
        {
        }

        private static int AdditionMethod()
        {
            var x = 0xC7C700;
            return x << 5;
        }
    }
}

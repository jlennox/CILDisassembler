using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CILDisassembler.Tests
{
    [TestClass]
    public class DisassemblerTests
    {
        public static DisassemblerOptions DisassemblerOptions = DisassemblerOptions.AlignOperand;

        [TestMethod]
        public void AssertNormalizedSanityCheck()
        {
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

        protected static byte[] Base64ToMethod(string base64)
        {
            return Convert.FromBase64String(base64);
        }

        protected static void AssertDisassembly(
            MethodInfo methodInfo, string expected, string testName = "")
        {
            var sb = new StringBuilder();
            Disassembler.Disassemble(methodInfo, DisassemblerOptions, sb);
            var disassembly = sb.ToString();
            AssertNormalized(expected, disassembly, testName);
        }

        private static readonly Regex _trimLeadingSpacesExp =
            new Regex(@"^\s+", RegexOptions.Multiline);

        private static void AssertNormalized(
            string expected, string actual, string testName = "")
        {
            Assert.AreEqual(
                NormalizeString(expected),
                NormalizeString(actual),
                $"Test '{testName}' failed.");
        }

        private static string NormalizeString(string s)
        {
            return _trimLeadingSpacesExp
                .Replace(s.Replace("\r\n", "\n").Trim(), "");
        }
    }
}

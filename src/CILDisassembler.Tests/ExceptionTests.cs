using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CILDisassembler.Tests
{
    [TestClass]
    public class ExceptionTests
    {
        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);

        [TestMethod]
        public void FailsOnNonCILMethods()
        {
            try
            {
                var method = typeof(ExceptionTests).GetMethod(
                    "DestroyIcon", BindingFlags.NonPublic | BindingFlags.Static);

                Disassembler.Disassemble(
                    method, DisassemblerOptions.None,
                    new StringBuilder());

                Assert.Fail("Did not throw.");
            }
            catch (ArgumentOutOfRangeException e)
                when (e.Message.StartsWith(Disassembler.CILOnlyMethodsError))
            {
            }
        }

        public static void PleaseDontOptimizeOutMyReference()
        {
            DestroyIcon(IntPtr.Zero);
        }
    }
}

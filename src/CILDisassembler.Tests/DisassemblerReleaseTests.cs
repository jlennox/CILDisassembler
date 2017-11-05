using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CILDisassembler.Tests
{
    [TestClass]
    public class DisassemblerReleaseTests : DisassemblerTests
    {
        [TestMethod]
        public void EnsureReleaseDissassemblyProducesExpectedOutput()
        {
            var assemblyPath = Path.Combine(Path.GetDirectoryName(
                GetType().Assembly.Location), "CILDisassembler.Tests.Release.dll");
            var assembly = Assembly.LoadFile(assemblyPath);

            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "Empty"),
                    @"ret
", "Empty Release");


            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "Goto"),
                    @"IL_0000:
ldc.i4.0
call         System.Threading.Thread.Sleep
call         System.DateTime.get_Now
stloc.1
ldloca.s     1
call         System.DateTime.get_Ticks
ldc.i4.1
conv.i8
beq.s        IL_001d
ldc.i4.1
call         System.Threading.Thread.Sleep
IL_001d:
ldc.i4.0
stloc.0
IL_001f:
ldc.i4.2
call         System.Threading.Thread.Sleep
ldloc.0
dup
ldc.i4.1
add
stloc.0
ldc.i4.5
blt.s        IL_001f
ldc.i4.3
call         System.Threading.Thread.Sleep
br.s         IL_0000
", "Goto Release");


            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "TwoByteInstruction"),
                    @"ldc.i4.0
stloc.0
IL_0002:
ldloc.0
dup
ldc.i4.1
add
stloc.0
ldc.i4.6
ble.s        IL_0002
ret
", "TwoByteInstruction Release");


            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "Addition"),
                    @"ldc.i4       13092608
ldc.i4.5
shl
ret
", "Addition Release");


            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "Call"),
                    @"call         System.DateTime.get_Now
stloc.0
ldstr        ""1/1/2017""
ldloca.s     1
call         System.DateTime.TryParse
brfalse.s    IL_0028
ldloc.0
ldloc.1
call         System.DateTime.op_Equality
brfalse.s    IL_0028
ldloc.1
box          System.DateTime
call         System.Console.WriteLine
IL_0028:
ret
", "Call Release");


            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "InlineType"),
                    @"call         System.DateTime.get_Now
box          System.DateTime
call         System.Console.WriteLine
ret
", "InlineType Release");


        }
    }
}
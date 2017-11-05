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
    public class DisassemblerDebugTests : DisassemblerTests
    {
        [TestMethod]
        public void EnsureDebugDissassemblyProducesExpectedOutput()
        {
            var assemblyPath = Path.Combine(Path.GetDirectoryName(
                GetType().Assembly.Location), "CILDisassembler.Tests.Debug.dll");
            var assembly = Assembly.LoadFile(assemblyPath);

            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "Empty"),
                    @"nop
ret
", "Empty Debug");


            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "Goto"),
                    @"nop
nop
IL_0001:
ldc.i4.0
call         System.Threading.Thread.Sleep
nop
call         System.DateTime.get_Now
stloc.2
ldloca.s     2
call         System.DateTime.get_Ticks
ldc.i4.1
conv.i8
ceq
stloc.1
ldloc.1
brfalse.s    IL_0021
nop
br.s         IL_0028
IL_0021:
ldc.i4.1
call         System.Threading.Thread.Sleep
nop
IL_0028:
nop
ldc.i4.0
stloc.0
IL_002b:
nop
ldc.i4.2
call         System.Threading.Thread.Sleep
nop
ldloc.0
dup
ldc.i4.1
add
stloc.0
ldc.i4.5
clt
stloc.3
ldloc.3
brfalse.s    IL_0042
nop
br.s         IL_002b
IL_0042:
ldc.i4.3
call         System.Threading.Thread.Sleep
nop
br.s         IL_0001
", "Goto Debug");


            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "TwoByteInstruction"),
                    @"nop
ldc.i4.0
stloc.0
br.s         IL_0016
nop
ldloc.0
dup
ldc.i4.1
add
stloc.0
ldc.i4.6
cgt
stloc.1
ldloc.1
brfalse.s    IL_001a
nop
br.s         IL_0019
IL_0015:
nop
IL_0016:
ldc.i4.1
stloc.2
br.s         IL_0004
IL_001a:
ret
", "TwoByteInstruction Debug");


            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "Addition"),
                    @"nop
ldc.i4       13092608
stloc.0
ldloc.0
ldc.i4.5
shl
stloc.1
br.s         IL_000d
IL_000d:
ldloc.1
ret
", "Addition Debug");


            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "Call"),
                    @"nop
call         System.DateTime.get_Now
stloc.0
ldstr        ""1/1/2017""
ldloca.s     1
call         System.DateTime.TryParse
stloc.2
ldloc.2
brfalse.s    IL_0032
nop
ldloc.0
ldloc.1
call         System.DateTime.op_Equality
stloc.3
ldloc.3
brfalse.s    IL_0031
nop
ldloc.1
box          System.DateTime
call         System.Console.WriteLine
nop
nop
IL_0031:
nop
IL_0032:
ret
", "Call Debug");


            AssertDisassembly(
                TestMethodAccess.MethodByName(
                    assembly, "InlineType"),
                    @"nop
call         System.DateTime.get_Now
box          System.DateTime
call         System.Console.WriteLine
nop
ret
", "InlineType Debug");


        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using CILDisassembler.Tests.Release;

namespace CILDisassembler.Tests
{
    static class TestMethodAccess
    {
        private const string _className = "TestMethods";
        public static string[] MethodNames = new[] {
            nameof(TestMethods.Empty),
            nameof(TestMethods.Goto),
            nameof(TestMethods.TwoByteInstruction),
            nameof(TestMethods.Addition),
            nameof(TestMethods.Call),
            nameof(TestMethods.InlineType)
        };

        public static MethodInfo MethodByName(Assembly assembly, string name)
        {
            return assembly.GetTypes()
                .FirstOrDefault(t => t.Name == _className)
                .GetMethod(name, BindingFlags.Static | BindingFlags.Public);
        }
    }
}

//MIT License

//Copyright(c) 2017 Joseph Lennox

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CILDisassembler
{
    [Flags]
    public enum DisassemblerOptions
    {
        None,
        /// <summary>
        /// Include the address as a label before each instruction.
        /// </summary>
        AddressLabel = 1 << 0,
        /// <summary>
        /// Include a comment with the bytes after each instruction.
        /// </summary>
        BytesComment = 1 << 1,
        /// <summary>
        /// Align the operand's to the same text columns.
        /// </summary>
        AlignOperand = 1 << 2,
        ResharperLike = AddressLabel | AlignOperand,
        Default = AlignOperand,
        All = int.MaxValue
    }

    public struct DisassembledOpcode
    {
        private const int _alignOperandLength = 13;

        public uint Offset;
        public OpCode OpCode;
        public int OperandSize;
        public long Operand;

        // The byte size of the opcode+operand.
        public int ByteSize => (OpCode.Value > 0xFF ? 2 : 1) + OperandSize;

        public void Nmunonic(
            Module module,
            DisassemblerOptions options,
            StringBuilder output)
        {
            var addressLabelOption = options.HasFlag(DisassemblerOptions.AddressLabel);
            var bytesCommentOption = options.HasFlag(DisassemblerOptions.BytesComment);
            var alignOperandOption = options.HasFlag(DisassemblerOptions.AlignOperand);

            if (addressLabelOption)
            {
                output.Append("ADDR_");
                output.Append(Offset.ToString("x4"));
                output.Append(":  ");
            }

            var name = OpCode.Name;
            output.Append(name);

            if (OperandSize > 0)
            {
                if (alignOperandOption && name.Length < _alignOperandLength)
                {
                    output.Append(' ', _alignOperandLength - name.Length);
                }
                else
                {
                    output.Append(' ');
                }

                switch (OpCode.OperandType)
                {
                    case OperandType.InlineMethod:
                        var methodarg = module.ResolveMethod((int)Operand);
                        output.Append(methodarg.DeclaringType.ToString());
                        output.Append('.');
                        output.Append(methodarg.Name);
                        break;
                    case OperandType.InlineField:
                        var fieldarg = module.ResolveField((int)Operand);
                        output.Append(fieldarg.DeclaringType.FullName);
                        output.Append('.');
                        output.Append(fieldarg.Name);
                        break;
                    case OperandType.InlineString:
                        var stringarg = module.ResolveString((int)Operand);
                        output.EnsureCapacity(stringarg.Length + 2);
                        output.Append('"');
                        foreach (var chr in stringarg)
                        {
                            switch (chr)
                            {
                                case '\n': output.Append("\\n"); continue;
                                case '\r': output.Append("\\r"); continue;
                                case '\t': output.Append("\\t"); continue;
                                case '\\': output.Append("\\\\"); continue;
                                case '"': output.Append("\\\""); continue;
                            }

                            if (chr < 32)
                            {
                                output.Append("\\x");
                                output.Append(((int)chr).ToString("X2"));
                                continue;
                            }

                            output.Append(chr);
                        }
                        output.Append('"');
                        break;
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        var typearg = module.ResolveType((int)Operand);
                        output.Append(typearg.FullName);
                        break;
                    case OperandType.InlineBrTarget:
                    case OperandType.ShortInlineBrTarget:
                        var abs = GetAbsoluteBranchDestination();
                        LabelFromOffset(abs, output);
                        break;
                    default:
                        output.Append(Operand);
                        break;
                }
            }

            if (bytesCommentOption)
            {
                output.Append("\t// 0x");
                var opcodeFormat = OpCode.Value > 0xFF ? "x4" : "x2";
                output.Append(OpCode.Value.ToString(opcodeFormat));

                if (OperandSize > 0)
                {
                    output.Append(" 0x");
                    var operandFormat = "x" + OperandSize * 2;
                    output.Append(Operand.ToString(operandFormat));
                }
            }
        }

        internal uint GetAbsoluteBranchDestination()
        {
            switch (OpCode.OperandType)
            {
                case OperandType.InlineBrTarget:
                    return (uint)((int)Operand + Offset + ByteSize);
                case OperandType.ShortInlineBrTarget:
                    return (uint)((sbyte)Operand + Offset + ByteSize);
            }

            throw new ArgumentOutOfRangeException(
                nameof(OperandType), OpCode.OperandType,
                "Can only be called on branch instructions.");
        }

        internal static void LabelFromOffset(uint offset, StringBuilder output)
        {
            output.Append("IL_");
            output.Append(offset.ToString("x4"));
        }
    }

    public static class Disassembler
    {
        internal const string CILOnlyMethodsError = "Only IL methods are supported.";

        public static int Disassemble(
            MethodInfo methodInfo,
            DisassemblerOptions options,
            StringBuilder output)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (!methodInfo.MethodImplementationFlags
                .HasFlag(MethodImplAttributes.IL))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(methodInfo.MethodImplementationFlags),
                    methodInfo.MethodImplementationFlags.ToString(),
                    CILOnlyMethodsError);
            }

            // This happens on extern (ie, DLLImport) methods.
            var body = methodInfo.GetMethodBody();

            if (body == null)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(methodInfo.MethodImplementationFlags),
                    methodInfo.MethodImplementationFlags.ToString(),
                    CILOnlyMethodsError);
            }

            return Disassemble(body.GetILAsByteArray(),
                methodInfo.Module, options, output);
        }

        public static int Disassemble(
            byte[] bytecode, Module module,
            DisassemblerOptions options,
            StringBuilder output)
        {
            return Disassemble(
                bytecode, 0, bytecode.Length,
                module, options, output);
        }

        public static int Disassemble(
            byte[] bytecode, int offset, int count,
            Module module, StringBuilder output)
        {
            return Disassemble(
                bytecode, offset, count, module,
                DisassemblerOptions.Default, output);
        }

        public static int Disassemble(
            byte[] bytecode, int offset, int count,
            Module module, DisassemblerOptions options,
            StringBuilder output)
        {
            // Make the pretty iffy assumption about how much each byte will
            // add to the output.
            output.EnsureCapacity(output.Length + count * 5);

            var instructionsDecoded = 0;
            var opcodes = GetOpCodes(bytecode, offset, count).ToArray();
            var requiresLabel = new HashSet<uint>();

            foreach (var opcode in opcodes)
            {
                switch (opcode.OpCode.OperandType)
                {
                    case OperandType.InlineBrTarget:
                    case OperandType.ShortInlineBrTarget:
                        var abs = opcode.GetAbsoluteBranchDestination();
                        requiresLabel.Add(abs);
                        break;
                }
            }

            foreach (var opcode in opcodes)
            {
                if (requiresLabel.Contains(opcode.Offset))
                {
                    DisassembledOpcode.LabelFromOffset(opcode.Offset, output);
                    output.Append(":\n");
                }

                opcode.Nmunonic(module, options, output);
                output.Append('\n');
                ++instructionsDecoded;
            }

            return instructionsDecoded;
        }

        public static IEnumerable<DisassembledOpcode> GetOpCodes(
            byte[] bytecode, int offset, int count)
        {
            var end = offset + count;

            ushort prefix = 0;
            uint ciloffset = 0;

            for (var i = offset; i < end; ++i)
            {
                var opcodeByte = (ushort)bytecode[i];

                // The first byte of all two byte opcodes are a "prefix."
                // Prefix7 = 0xF8, Prefix1 = 0xFE
                if (opcodeByte >= OpCodes.Prefix7.Value &&
                    opcodeByte <= OpCodes.Prefix1.Value)
                {
                    prefix = opcodeByte;
                    continue;
                }

                if (prefix != 0)
                {
                    opcodeByte |= (ushort)(prefix << 8);
                    prefix = 0;
                }

                var opcode = LookupTables.OpcodeLookup[opcodeByte];
                var operandSize = LookupTables
                    .OperandSizeLookup[(byte)opcode.OperandType];

                var dis = new DisassembledOpcode {
                    OpCode = opcode,
                    OperandSize = operandSize,
                    Offset = ciloffset
                };

                if (operandSize > 0)
                {
                    var val = 0L;

                    for (var nread = 0;
                        i < end && nread < operandSize;
                        ++nread)
                    {
                        ++i;
                        val |= (long)bytecode[i] << (nread * 8);
                    }

                    dis.Operand = val;
                }

                ciloffset += (uint)dis.ByteSize;

                yield return dis;
            }

            // Need to check spec if this is actually invalid.
            if (prefix != 0)
            {
                throw new InvalidProgramException(
                    "A prefix instruction was the final value in a method.");
            }
        }
    }

    internal static class LookupTables
    {
        public static int[] OperandSizeLookup = InitializeOperandSizeLookup();
        public static OpCode[] OpcodeLookup = InitializeOpcodeLookup();

        private static int[] InitializeOperandSizeLookup()
        {
            // https://github.com/jbevain/cecil/blob/master/Mono.Cecil.Cil/Instruction.cs#L62
            var operandSizeLookup = new int[0xFF];
            operandSizeLookup[(byte)OperandType.InlineBrTarget] = 4;
            operandSizeLookup[(byte)OperandType.InlineField] = 4;
            operandSizeLookup[(byte)OperandType.InlineI] = 4;
            operandSizeLookup[(byte)OperandType.InlineI8] = 8;
            operandSizeLookup[(byte)OperandType.InlineMethod] = 4;
            operandSizeLookup[(byte)OperandType.InlineNone] = 0;
            operandSizeLookup[(byte)OperandType.InlinePhi] = 0;
            operandSizeLookup[(byte)OperandType.InlineR] = 8;
            operandSizeLookup[(byte)OperandType.InlineSig] = 4;
            operandSizeLookup[(byte)OperandType.InlineString] = 4;
            operandSizeLookup[(byte)OperandType.InlineSwitch] = 4; // Broken?
            operandSizeLookup[(byte)OperandType.InlineTok] = 4;
            operandSizeLookup[(byte)OperandType.InlineType] = 4;
            operandSizeLookup[(byte)OperandType.InlineVar] = 2;
            operandSizeLookup[(byte)OperandType.ShortInlineBrTarget] = 1;
            operandSizeLookup[(byte)OperandType.ShortInlineI] = 1;
            operandSizeLookup[(byte)OperandType.ShortInlineR] = 4;
            operandSizeLookup[(byte)OperandType.ShortInlineVar] = 1;
            return operandSizeLookup;
        }

        private static OpCode[] InitializeOpcodeLookup()
        {
            var opcodeLookup = new OpCode[0xFFFF];
            var maxOpcode = 0;

            foreach (var field in typeof(OpCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(t => t.FieldType == typeof(OpCode)))
            {
                var opcode = (OpCode)field.GetValue(null);
                var absoluteValue = (ushort)opcode.Value;
                opcodeLookup[absoluteValue] = opcode;

                if (absoluteValue > maxOpcode)
                {
                    maxOpcode = absoluteValue;
                }
            }

            Array.Resize(ref opcodeLookup, maxOpcode);

            return opcodeLookup;
        }
    }
}
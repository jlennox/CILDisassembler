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
using System.Threading.Tasks;

namespace CILDisassembler
{
    public struct DisassembledOpcode
    {
        public OpCode OpCode;
        public int OperandSize;
        public long Operand;

        public void Nmunonic(StringBuilder output)
        {
            output.Append(OpCode.ToString());

            if (OperandSize > 0)
            {
                output.Append(' ');
                output.Append(Operand);
            }
        }
    }

    public class Disassembler
    {
        public static int Disassemble(
            MethodInfo methodInfo, StringBuilder output)
        {
            var body = methodInfo.GetMethodBody();
            return Disassemble(body.GetILAsByteArray(), output);
        }

        public static int Disassemble(
            byte[] bytecode, StringBuilder output)
        {
            return Disassemble(bytecode, 0, bytecode.Length, output);
        }

        public static int Disassemble(
            byte[] bytecode, int offset, int count, StringBuilder output)
        {
            // Make the pretty iffy assumption about how much each byte will
            // add to the output.
            output.EnsureCapacity(output.Length + count * 5);

            var instructionsDecoded = 0;

            foreach (var dis in GetOpCodes(bytecode, offset, count))
            {
                dis.Nmunonic(output);
                output.Append('\n');
                ++instructionsDecoded;
            }

            return instructionsDecoded;
        }

        public static IEnumerable<DisassembledOpcode> GetOpCodes(
            byte[] bytecode, int offset, int count)
        {
            var end = offset + count;

            for (var i = offset; i < end; ++i)
            {
                var opcode = LookupTables.OpcodeLookup[bytecode[i]];
                var operandSize = LookupTables
                    .OperandSizeLookup[(byte)opcode.OperandType];

                var dis = new DisassembledOpcode {
                    OpCode = opcode,
                    OperandSize = operandSize
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

                yield return dis;
            }
        }
    }

    internal static class LookupTables
    {
        public static int[] OperandSizeLookup = InitializeOperandSizeLookup();
        public static OpCode[] OpcodeLookup = InitializeOpcodeLookup();

        private static int[] InitializeOperandSizeLookup()
        {
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
            operandSizeLookup[(byte)OperandType.InlineSwitch] = 4;
            operandSizeLookup[(byte)OperandType.InlineTok] = 4; //??
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CILDissassembler.Tests.Debug
{
    public static class TestMethods
    {
        public static void Empty()
        {
        }

        public static void Goto()
        {
            verystart:
            Thread.Sleep(0);
            if (DateTime.Now.Ticks == 1)
            {
                // Forward jump.
                goto skip1;
            }
            Thread.Sleep(1);
            skip1:
            var x = 0;
            start:
            Thread.Sleep(2);
            if (x++ < 5)
            {
                // Backward jump.
                goto start;
            }
            Thread.Sleep(3);
            // Jump to 0.
            goto verystart;

            // Indefinitely recursive methods do not terminate with a "ret"
            // even in debug builds.
        }

        public static void TwoByteInstruction()
        {
            var x = 0;
            while (true)
            {
                // Creates the 'cgt' instruction, 0xFE 0x02
                if (x++ > 6)
                {
                    break;
                }
            }
        }

        public static int Addition()
        {
            var x = 0xC7C700;
            return x << 5;
        }

        public static void InlineType()
        {
            // The datetime struct will cause a 'box' instruction with the
            // operand type of InlineType.
            Console.WriteLine(DateTime.Now);
        }

        public static void Call()
        {
            // Property getter.
            var x = DateTime.Now;

            // Method.
            if (DateTime.TryParse("1/1/2017", out DateTime result))
            {
                // Reference x as to not let it get optimized out.
                if (x == result)
                {
                    Console.WriteLine(result);
                }
            }
        }
    }
}

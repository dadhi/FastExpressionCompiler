using System;
using FastExpressionCompiler.UnitTests;

namespace FastExpressionCompiler.UnitTests
{

    public class Program
    {
        public static void Main()
        {
            var result = ArithmeticOperationsTests.Run();

            if (result)
                Console.WriteLine("Selected tests passed: " + nameof(ArithmeticOperationsTests));
            else
                Console.WriteLine("Selected tests FAILED! ERROR! boing-boing!: " + nameof(ArithmeticOperationsTests));
        }
    }
}

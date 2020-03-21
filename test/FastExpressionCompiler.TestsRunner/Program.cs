using System;
using FastExpressionCompiler.UnitTests;

namespace FastExpressionCompiler.UnitTests
{

    public class Program
    {
        public static void Main()
        {
            TestBase[] tests =
            {
                new ArithmeticOperationsTests()
            };

            var failed = false;
            var totalTestCount = 0;
            foreach (var test in tests)
            {
                try
                {
                    totalTestCount += test.Run();
                }
                catch (Exception ex)
                {
                    failed = true;
                    Console.WriteLine($"Failed! {test.GetType().Name} with: {ex}");
                }
            }

            if (failed)
            {
                Console.WriteLine("Some tests are FAILED! ERROR!");
                Environment.Exit(1); // error
            }
            else
                Console.WriteLine($"{totalTestCount} tests are passing green!");
        }
    }
}

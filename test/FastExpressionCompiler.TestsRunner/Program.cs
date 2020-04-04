using System;
using FastExpressionCompiler.UnitTests;

namespace FastExpressionCompiler.UnitTests
{

    public class Program
    {
        public static void Main()
        {
            var failed = false;
            var totalTestCount = 0;
            void TryRun(Func<int> run) 
            {
                try
                {
                    totalTestCount += run();
                }
                catch (Exception ex)
                {
                    failed = true;
                    Console.WriteLine($"Failed! {run.Method.DeclaringType.FullName} with: {ex}");
                }
            }

            TryRun(new ArithmeticOperationsTests().Run);
            // TryRun(FastExpressionCompiler.LightExpression.UnitTests.ArithmeticOperationsTests.Run);

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

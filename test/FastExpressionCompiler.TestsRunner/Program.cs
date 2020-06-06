using System;
using System.Diagnostics;
using FastExpressionCompiler.UnitTests;

namespace FastExpressionCompiler.UnitTests
{
    public class Program
    {
        public static void Main()
        {
            var failed = false;
            var totalTestPassed = 0;
            void TryRun(Func<int> run)
            {
                var testsTypeName = run.Method.DeclaringType.FullName;
                try
                {
                    var testsPassed = run();
                    totalTestPassed += testsPassed;
                    Console.WriteLine($"{testsPassed} of {testsTypeName} are passing.");
                }
                catch (Exception ex)
                {
                    failed = true;
                    Console.WriteLine($"ERROR: Some of {testsTypeName} are failed with {ex}!");
                }
            }

            var sw = Stopwatch.StartNew();

            TryRun(new ArithmeticOperationsTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.ArithmeticOperationsTests().Run);

            if (failed)
            {
                Console.WriteLine("ERROR: Some tests are FAILED!");
                Environment.Exit(1); // error
                return;
            }

            Console.WriteLine($"{totalTestPassed} of all tests are passing in {sw.ElapsedMilliseconds} ms.");
        }
    }
}

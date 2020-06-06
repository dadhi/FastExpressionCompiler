using System;
using System.Diagnostics;

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
                    Console.WriteLine($"{testsPassed, -4} of {testsTypeName} are passing.");
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
            TryRun(new AssignTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.AssignTests().Run);
            TryRun(new BinaryExpressionTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.BinaryExpressionTests().Run);
            TryRun(new BlockTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.BlockTests().Run);
            TryRun(new ClosureConstantTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.ClosureConstantTests().Run);
            TryRun(new CoalesceTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.CoalesceTests().Run);
            TryRun(new ConditionalOperatorsTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.ConditionalOperatorsTests().Run);
            TryRun(new ConstantAndConversionTests().Run);
            TryRun(new ConvertOperatorsTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.ConvertOperatorsTests().Run);
            TryRun(new DefaultTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.DefaultTests().Run);
            TryRun(new EqualityOperatorsTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.EqualityOperatorsTests().Run);
            TryRun(new GotoTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.GotoTests().Run);
            TryRun(new HoistedLambdaExprTests().Run);
            TryRun(new LoopTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.LoopTests().Run);
            TryRun(new ManuallyComposedExprTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.ManuallyComposedExprTests().Run);
            TryRun(new NestedLambdaTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.NestedLambdaTests().Run);
            TryRun(new PreConstructedClosureTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.PreConstructedClosureTests().Run);

            if (failed)
            {
                Console.WriteLine("ERROR: Some tests are FAILED!");
                Environment.Exit(1); // error
                return;
            }

            Console.WriteLine($"{totalTestPassed, -4} of all tests are passing in {sw.ElapsedMilliseconds} ms.");
        }
    }
}

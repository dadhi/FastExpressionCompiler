using System;
using System.Diagnostics;
using FastExpressionCompiler.IssueTests;

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
                    Console.WriteLine($"{testsPassed,-4} of {testsTypeName} are passing.");
                }
                catch (Exception ex)
                {
                    failed = true;
                    Console.WriteLine($"ERROR: Some of {testsTypeName} are failed with {ex}!");
                }
            }

            var sw = Stopwatch.StartNew();

            Console.WriteLine();
            Console.WriteLine("Running UnitTests...");
            Console.WriteLine();

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
            TryRun(new TryCatchTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.TryCatchTests().Run);
            TryRun(new TypeBinaryExpressionTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.TypeBinaryExpressionTests().Run);
            TryRun(new UnaryExpressionTests().Run);
            TryRun(new FastExpressionCompiler.LightExpression.UnitTests.UnaryExpressionTests().Run);
            TryRun(new ValueTypeTests().Run);

            Console.WriteLine();
            Console.WriteLine("Running IssueTests...");
            Console.WriteLine();

            TryRun(new Issue14_String_constant_comparisons_fail().Run);
            TryRun(new FastExpressionCompiler.LightExpression.IssueTests.Issue14_String_constant_comparisons_fail().Run);
            TryRun(new Issue19_Nested_CallExpression_causes_AccessViolationException().Run);
            TryRun(new FastExpressionCompiler.LightExpression.IssueTests.Issue19_Nested_CallExpression_causes_AccessViolationException().Run);
            TryRun(new Issue44_Conversion_To_Nullable_Throws_Exception().Run);
            TryRun(new FastExpressionCompiler.LightExpression.IssueTests.Issue44_Conversion_To_Nullable_Throws_Exception().Run);
            TryRun(new Issue55_CompileFast_crash_with_ref_parameter().Run);
            TryRun(new FastExpressionCompiler.LightExpression.IssueTests.Issue55_CompileFast_crash_with_ref_parameter().Run);

            Console.WriteLine();

            if (failed)
            {
                Console.WriteLine("ERROR: Some tests are FAILED!");
                Environment.Exit(1); // error
                return;
            }

            Console.WriteLine($"{totalTestPassed,-4} of all tests are passing in {sw.ElapsedMilliseconds} ms.");
        }
    }
}

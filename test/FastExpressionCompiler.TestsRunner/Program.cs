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
            void RunTest(Func<int> run, string name = null)
            {
                var testsName = name ?? run.Method.DeclaringType.FullName;
                try
                {
                    var testsPassed = run();
                    totalTestPassed += testsPassed;
                    Console.WriteLine($"{testsPassed,-4} of {testsName} are passing.");
                }
                catch (Exception ex)
                {
                    failed = true;
                    Console.WriteLine($"ERROR: Some of {testsName} are failed with {ex}!");
                }
            }

            var sw = Stopwatch.StartNew();

            Console.WriteLine();
            Console.WriteLine("Running UnitTests...");
            Console.WriteLine();

            RunTest(new ArithmeticOperationsTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.ArithmeticOperationsTests().Run);
            RunTest(new AssignTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.AssignTests().Run);
            RunTest(new BinaryExpressionTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.BinaryExpressionTests().Run);
            RunTest(new BlockTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.BlockTests().Run);
            RunTest(new ClosureConstantTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.ClosureConstantTests().Run);
            RunTest(new CoalesceTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.CoalesceTests().Run);
            RunTest(new ConditionalOperatorsTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.ConditionalOperatorsTests().Run);
            RunTest(new ConstantAndConversionTests().Run);
            RunTest(new ConvertOperatorsTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.ConvertOperatorsTests().Run);
            RunTest(new DefaultTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.DefaultTests().Run);
            RunTest(new EqualityOperatorsTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.EqualityOperatorsTests().Run);
            RunTest(new GotoTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.GotoTests().Run);
            RunTest(new HoistedLambdaExprTests().Run);
            RunTest(new LoopTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.LoopTests().Run);
            RunTest(new ManuallyComposedExprTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.ManuallyComposedExprTests().Run);
            RunTest(new NestedLambdaTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.NestedLambdaTests().Run);
            RunTest(new PreConstructedClosureTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.PreConstructedClosureTests().Run);
            RunTest(new TryCatchTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.TryCatchTests().Run);
            RunTest(new TypeBinaryExpressionTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.TypeBinaryExpressionTests().Run);
            RunTest(new UnaryExpressionTests().Run);
            RunTest(new FastExpressionCompiler.LightExpression.UnitTests.UnaryExpressionTests().Run);
            RunTest(new ValueTypeTests().Run);

            Console.WriteLine();
            Console.WriteLine("Running IssueTests...");
            Console.WriteLine();

            RunTest(new Issue14_String_constant_comparisons_fail().Run);
            RunTest(new FastExpressionCompiler.LightExpression.IssueTests.Issue14_String_constant_comparisons_fail().Run);
            RunTest(new Issue19_Nested_CallExpression_causes_AccessViolationException().Run);
            RunTest(new FastExpressionCompiler.LightExpression.IssueTests.Issue19_Nested_CallExpression_causes_AccessViolationException().Run);
            RunTest(new Issue44_Conversion_To_Nullable_Throws_Exception().Run);
            RunTest(new FastExpressionCompiler.LightExpression.IssueTests.Issue44_Conversion_To_Nullable_Throws_Exception().Run);
            RunTest(new Issue55_CompileFast_crash_with_ref_parameter().Run);
            RunTest(new FastExpressionCompiler.LightExpression.IssueTests.Issue55_CompileFast_crash_with_ref_parameter().Run);
            RunTest(new Issue67_Equality_comparison_with_nullables_throws_at_delegate_invoke().Run);
            RunTest(new Issue71_Cannot_bind_to_the_target_method_because_its_signature().Run);
            RunTest(new FastExpressionCompiler.LightExpression.IssueTests.Issue71_Cannot_bind_to_the_target_method_because_its_signature().Run);
            RunTest(() => new Issue72_Try_CompileFast_for_MS_Extensions_ObjectMethodExecutor().Run().GetAwaiter().GetResult(),
                nameof(Issue72_Try_CompileFast_for_MS_Extensions_ObjectMethodExecutor));

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

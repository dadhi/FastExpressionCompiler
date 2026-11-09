//#define INTERPRETATION_DIAGNOSTICS

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FastExpressionCompiler.IssueTests;

namespace FastExpressionCompiler.UnitTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            RunRuntimeAccessorContracts();
            if (Array.IndexOf(args, "--runtime-accessor-contracts") >= 0)
                return;

            // TestTools.AllowPrintCS = false;
            // TestTools.AllowPrintIL = false;
            // TestTools.AllowPrintExpression = false;

            // ILGeneratorTools.DisableILGeneratorPooling = true;
            // LightExpression.ILGeneratorTools.DisableILGeneratorPooling = true;

            // new LightExpression.IssueTests.Issue414_Incorrect_il_when_passing_by_ref_value().Run();
            // new TryCatchTests().Run();
            // new Issue320_Bad_label_content_in_ILGenerator_when_creating_through_DynamicModule().Run();
            // new Issue422_InvalidProgramException_when_having_TryCatch_Default_in_Catch().Run();
            // new Issue428_Expression_Switch_without_a_default_case_incorrectly_calls_first_case_for_unmatched_values().Run();
            // new LightExpression.IssueTests.Issue365_Working_with_ref_return_values().Run();
            // new LightExpression.IssueTests.Issue321_Call_with_out_parameter_to_field_type_that_is_not_value_type_fails().Run();
            // new LightExpression.IssueTests.Issue363_ActionFunc16Generics().Run();
            // new Issue455_TypeAs_should_return_null().Run();
            // new BlockTests().Run();
            // new LightExpression.IssueTests.Issue237_Trying_to_implement_For_Foreach_loop_but_getting_an_InvalidProgramException_thrown().Run();
            // new AssignTests().Run();
            // new LightExpression.IssueTests.Issue365_Working_with_ref_return_values().Run();
            // new ArithmeticOperationsTests().Run();
            // new Issue156_InvokeAction().Run();
            // new Issue252_Bad_code_gen_for_comparison_of_nullable_type_to_null().Run();
            // new ConditionalOperatorsTests().Run();
            // new ConstantAndConversionTests().Run();
            // new Issue284_Invalid_Program_after_Coalesce().Run(); // test print cs
            // new Issue440_Errors_with_simplified_Switch_cases().Run(); // test print cs
            // new LoopTests().Run(); // test print cs
            // new Issue458_Support_TryFault().Run();
            // new Issue127_Switch_is_supported().Run();
            // new Issue316_in_parameter().Run();
            // new LightExpression.IssueTests.Issue347_InvalidProgramException_on_compiling_an_expression_that_returns_a_record_which_implements_IList().Run();
            // new LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest().Run();
            // new LightExpression.IssueTests.Issue274_Failing_Expressions_in_Linq2DB().Run();
            // new LightExpression.IssueTests.Issue55_CompileFast_crash_with_ref_parameter().Run();
            // new LightExpression.IssueTests.Issue341_Equality_comparison_between_nullable_and_null_inside_Any_produces_incorrect_compiled_expression().Run();
            // new Issue441_Fails_to_pass_Constant_as_call_parameter_by_reference().Run();
            // new Issue461_InvalidProgramException_when_null_checking_type_by_ref().Run();

            var testXStopwatch = Stopwatch.StartNew();
            var st = new TestRun(TestFlags.RethrowException);
            var lt = new LightExpression.TestRun(LightExpression.TestFlags.RethrowException);

            // the Light tests run on the thread pool in parallel with the System ones
            var ltTime = 0L;
            var ltTask = Task.Run(() =>
            {
                var ltStopwatch = Stopwatch.StartNew();
                GeneratedTests.RunLight(lt);

                ltTime = ltStopwatch.ElapsedMilliseconds;
            });

            var stStopwatch = Stopwatch.StartNew();
            GeneratedTests.RunStandard(st);

            var stTime = stStopwatch.ElapsedMilliseconds;
            ltTask.Wait();

            if (st.FailedTestCount > 0 || lt.FailedTestCount > 0)
            {
                Console.WriteLine("ERROR: Some TestX tests are FAILED!");
                Environment.Exit(1);
            }
            Console.WriteLine($"//TestX: {st.TotalTestCount + lt.TotalTestCount} tests are passing in {testXStopwatch.ElapsedMilliseconds} ms (System: {st.TotalTestCount} in {stTime} ms, Light: {lt.TotalTestCount} in {ltTime} ms, in parallel).");

            RunAllTests();
        }

        private static void RunRuntimeAccessorContracts()
        {
            var standard = new TestRun(TestFlags.RethrowException);
            var light = new LightExpression.TestRun(LightExpression.TestFlags.RethrowException);
            GeneratedTests.RunContracts(standard, light);
            if (standard.FailedTestCount != 0 || light.FailedTestCount != 0)
                throw new InvalidOperationException("Runtime accessor contracts failed; see the member/assertion diagnostics above.");
            Console.WriteLine($"Runtime accessor contracts passed: {standard.TotalTestCount} Standard, {light.TotalTestCount} LightExpression.");
        }

        public static void RunAllTests()
        {
            var failed = false;
            var totalTestsPassed = 0;
            void Run(Func<int> run, string name = null)
            {
                try
                {
                    var testsPassed = run();
                    System.Threading.Interlocked.Add(ref totalTestsPassed, testsPassed);
#if DEBUG
                    // we don't need to list the tests one-by-one on CI, and it makes avoiding it saves 30% of time
                    var testsName = name ?? run.Method.DeclaringType.FullName;
                    Console.WriteLine($"{testsPassed,-4} of {testsName}");
#endif
                }
                catch (Exception ex)
                {
                    failed = true;
                    var testsName = name ?? run.Method.DeclaringType.FullName;
                    Console.WriteLine($"""
                    --------------------------------------------
                    ERROR: Tests `{testsName}` failed with
                    {ex}
                    --------------------------------------------
                    """);
                }
            }

            Console.WriteLine("""

            //## .NET Latest (Core): Running UnitTests and IssueTests in parallel...
            """);

            var sw = Stopwatch.StartNew();

            var unitTests = Task.Run(() =>
            {
                GeneratedTests.RunUnitLegacy(Run);

                Console.WriteLine($"{Environment.NewLine}//UnitTests are passing in {sw.ElapsedMilliseconds} ms.");

            });

            var issueTests = Task.Run(() =>
            {
                GeneratedTests.RunIssueLegacy(Run);

                Console.WriteLine($"{Environment.NewLine}//IssueTests are passing in {sw.ElapsedMilliseconds} ms.");
            });

            Task.WaitAll(unitTests, issueTests);
            Console.WriteLine();
            if (failed)
            {
                Console.WriteLine("ERROR: Some tests are FAILED!");
                Environment.ExitCode = 1; // error exit code
                return;
            }

            Console.WriteLine($"//ALL {totalTestsPassed,-4} tests are passing in {sw.ElapsedMilliseconds} ms.");
        }
    }
}

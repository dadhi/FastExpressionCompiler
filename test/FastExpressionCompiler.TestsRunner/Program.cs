using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FastExpressionCompiler.IssueTests;

namespace FastExpressionCompiler.UnitTests
{
    public class Program
    {
        public static void Main()
        {
            RunAllTests();
        }

        public static void RunAllTests()
        {
            var failed = false;
            var totalTestPassed = 0;
            void Run(Func<int> run, string name = null)
            {
                var testsName = name ?? run.Method.DeclaringType.FullName;
                try
                {
                    var testsPassed = run();
                    totalTestPassed += testsPassed;
                    Console.WriteLine($"{testsPassed,-4} of {testsName}");
                }
                catch (Exception ex)
                {
                    failed = true;
                    Console.WriteLine($"ERROR: Tests `{testsName}` failed with '{ex}'");
                }
            }

            var sw = Stopwatch.StartNew();

            Console.WriteLine();
            Console.WriteLine("NETCOREAPP3.1: Running UnitTests and IssueTests in parallel...");
            Console.WriteLine();

            // todo: @perf try Parallel.ForEach
            var unitTests = Task.Run(() => 
            {
                Run(new ArithmeticOperationsTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.ArithmeticOperationsTests().Run);
                Run(new AssignTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.AssignTests().Run);
                Run(new BinaryExpressionTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.BinaryExpressionTests().Run);
                Run(new BlockTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.BlockTests().Run);
                Run(new ClosureConstantTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.ClosureConstantTests().Run);
                Run(new CoalesceTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.CoalesceTests().Run);
                Run(new ConditionalOperatorsTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.ConditionalOperatorsTests().Run);
                Run(new ConstantAndConversionTests().Run);
                Run(new ConvertOperatorsTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.ConvertOperatorsTests().Run);
                Run(new DefaultTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.DefaultTests().Run);
                Run(new EqualityOperatorsTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.EqualityOperatorsTests().Run);
                Run(new GotoTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.GotoTests().Run);
                Run(new HoistedLambdaExprTests().Run);
                Run(new LoopTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.LoopTests().Run);
                Run(new ManuallyComposedExprTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.ManuallyComposedExprTests().Run);
                Run(new NestedLambdaTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.NestedLambdaTests().Run);
                Run(new PreConstructedClosureTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.PreConstructedClosureTests().Run);
                Run(new TryCatchTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.TryCatchTests().Run);
                Run(new TypeBinaryExpressionTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.TypeBinaryExpressionTests().Run);
                Run(new UnaryExpressionTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.UnaryExpressionTests().Run);
                Run(new ValueTypeTests().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest().Run);
                Run(new FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests().Run);
            });

            var issueTests = Task.Run(() => 
            {
                Run(new Issue14_String_constant_comparisons_fail().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue14_String_constant_comparisons_fail().Run);
                Run(new Issue19_Nested_CallExpression_causes_AccessViolationException().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue19_Nested_CallExpression_causes_AccessViolationException().Run);
                Run(new Issue44_Conversion_To_Nullable_Throws_Exception().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue44_Conversion_To_Nullable_Throws_Exception().Run);
                Run(new Issue55_CompileFast_crash_with_ref_parameter().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue55_CompileFast_crash_with_ref_parameter().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue65_Add_LightExpression_Elvis_operator_support().Run);
                Run(new Issue67_Equality_comparison_with_nullables_throws_at_delegate_invoke().Run);
                Run(new Issue71_Cannot_bind_to_the_target_method_because_its_signature().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue71_Cannot_bind_to_the_target_method_because_its_signature().Run);
                Run(new Issue72_Try_CompileFast_for_MS_Extensions_ObjectMethodExecutor().Run);
                Run(new Issue76_Expression_Convert_causing_signature_or_security_transparency_is_not_compatible_exception().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue76_Expression_Convert_causing_signature_or_security_transparency_is_not_compatible_exception().Run);
                Run(new Issue78_blocks_with_constant_return().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue78_blocks_with_constant_return().Run);
                Run(new Issue83_linq2db().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue83_linq2db().Run);
                Run(new Issue88_Constant_from_static_field().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue88_Constant_from_static_field().Run);
                Run(new Issue91_Issue95_Tests().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue91_Issue95_Tests().Run);
                Run(new Issue100_LightExpression_wrong_return_type().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue100_LightExpression_wrong_return_type().Run);
                Run(new Issue101_Not_supported_Assign_Modes().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue101_Not_supported_Assign_Modes().Run);
                Run(new Issue102_Label_and_Goto_Expression().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue102_Label_and_Goto_Expression().Run);
                Run(new Issue106_Power_support().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue106_Power_support().Run);
                Run(new Issue107_Assign_also_works_for_variables().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue107_Assign_also_works_for_variables().Run);
                Run(new Issue127_Switch_is_supported().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue127_Switch_is_supported().Run);
                Run(new Issue146_bool_par().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue146_bool_par().Run);
                Run(new Issue147_int_try_parse().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue147_int_try_parse().Run);
                Run(new Issue150_New_AttemptToReadProtectedMemory().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue150_New_AttemptToReadProtectedMemory().Run);
                Run(new Issue153_MinValueMethodNotSupported().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue153_MinValueMethodNotSupported().Run);
                Run(new Issue156_InvokeAction().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue156_InvokeAction().Run);
                Run(new Issue159_NumericConversions().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue159_NumericConversions().Run);
                Run(new Issue170_Serializer_Person_Ref().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue170_Serializer_Person_Ref().Run);
                Run(new Issue174_NullReferenceInSetter().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue174_NullReferenceInSetter().Run);
                Run(new Issue177_Cannot_compile_to_the_required_delegate_type_with_non_generic_CompileFast().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue177_Cannot_compile_to_the_required_delegate_type_with_non_generic_CompileFast().Run);
                Run(new Issue179_Add_something_like_LambdaExpression_CompileToMethod().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue179_Add_something_like_LambdaExpression_CompileToMethod().Run);
                Run(new Issue181_TryEmitIncDecAssign_InvalidCastException().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue181_TryEmitIncDecAssign_InvalidCastException().Run);
                Run(new Issue183_NullableDecimal().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue183_NullableDecimal().Run);
                Run(new Issue190_Inc_Dec_Assign_Parent_Block_Var().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue190_Inc_Dec_Assign_Parent_Block_Var().Run);
                Run(new Issue196_AutoMapper_tests_are_failing_when_using_FEC.FastExpressionCompilerBug().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue196_AutoMapper_tests_are_failing_when_using_FEC.FastExpressionCompilerBug().Run);
                Run(new Issue197_Operation_could_destabilize_the_runtime().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue197_Operation_could_destabilize_the_runtime().Run);
                Run(new Issue204_Operation_could_destabilize_the_runtime__AutoMapper().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue204_Operation_could_destabilize_the_runtime__AutoMapper().Run);
                Run(new Issue209_AutoMapper_Operation_could_destabilize_the_runtime().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue209_AutoMapper_Operation_could_destabilize_the_runtime().Run);
                Run(new Issue243_Pass_Parameter_By_Ref_is_supported().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue243_Pass_Parameter_By_Ref_is_supported().Run);
                Run(new Nested_lambdas_assigned_to_vars().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Nested_lambdas_assigned_to_vars().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue231_ExpressionNew_does_not_work_with_struct().Run);
                Run(new Issue252_Bad_code_gen_for_comparison_of_nullable_type_to_null().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue252_Bad_code_gen_for_comparison_of_nullable_type_to_null().Run);

                Run(new Issue248_Calling_method_with_in_out_parameters_in_expression_lead_to_NullReferenceException_on_calling_site().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue248_Calling_method_with_in_out_parameters_in_expression_lead_to_NullReferenceException_on_calling_site().Run);
                
                Run(new Issue251_Bad_code_gen_for_byRef_parameters().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue251_Bad_code_gen_for_byRef_parameters().Run);
#if NETCOREAPP3_1
                Run(new Issue237_Trying_to_implement_For_Foreach_loop_but_getting_an_InvalidProgramException_thrown().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue237_Trying_to_implement_For_Foreach_loop_but_getting_an_InvalidProgramException_thrown().Run);
#endif
                Run(new Issue261_Loop_wih_conditions_fails().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue261_Loop_wih_conditions_fails().Run);

                Run(new Issue274_Failing_Expressions_in_Linq2DB().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue274_Failing_Expressions_in_Linq2DB().Run);

                Run(new Issue281_Index_Out_of_Range().Run);
                Run(new FastExpressionCompiler.LightExpression.IssueTests.Issue281_Index_Out_of_Range().Run);
            });

            Task.WaitAll(unitTests, issueTests);
            Console.WriteLine();
            if (failed)
            {
                Console.WriteLine("ERROR: Some tests are FAILED!");
                Environment.ExitCode = 1; // error exit code
                return;
            }

            Console.WriteLine($"{totalTestPassed,-4} of all tests are passing in {sw.ElapsedMilliseconds} ms.");
        }
    }
}

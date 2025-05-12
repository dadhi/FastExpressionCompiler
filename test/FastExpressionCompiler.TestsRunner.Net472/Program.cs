using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FastExpressionCompiler.IssueTests;

namespace FastExpressionCompiler.UnitTests
{
    public class Program
    {
        public static void Main()
        {
            // new Issue414_Incorrect_il_when_passing_by_ref_value().Run();
            // new Issue55_CompileFast_crash_with_ref_parameter().Run();

            RunTestsX();

            RunAllTests();
        }

        public static void RunTestsX()
        {
            var totalStopwatch = Stopwatch.StartNew();

            Console.WriteLine("""

            ### TestX runs on FEC tests (UnitTests and IssueTests) and FEC.LightExpression tests in ||
            """);

            var lightTestsStopwatch = Stopwatch.StartNew();
            var lightTestsThread = new Thread(RunLightExpressionTests) { IsBackground = false, Name = "Tests - FEC.LightExpression" };
            static void RunLightExpressionTests(object state)
            {
                var justLightTestsStopwatch = Stopwatch.StartNew();

                var t = (LightExpression.TestRun)state;
                t.Run(new LightExpression.IssueTests.Issue183_NullableDecimal());
                t.Run(new LightExpression.IssueTests.Issue468_Optimize_the_delegate_access_to_the_Closure_object_for_the_modern_NET());
                t.Run(new LightExpression.IssueTests.Issue472_TryInterpret_and_Reduce_primitive_arithmetic_and_logical_expressions_during_the_compilation());
                t.Run(new LightExpression.IssueTests.Issue473_InvalidProgramException_when_using_Expression_Condition_with_converted_decimal_expression());

                Console.WriteLine($"Just LightExpression tests are passing in {justLightTestsStopwatch.ElapsedMilliseconds} ms.");
            }
            var lightTests = new LightExpression.TestRun();
            lightTestsThread.Start(lightTests);


            var fecTestsStopwatch = Stopwatch.StartNew();

            var fecTests = new TestRun();
            fecTests.Run(new Issue183_NullableDecimal());
            fecTests.Run(new Issue468_Optimize_the_delegate_access_to_the_Closure_object_for_the_modern_NET());
            fecTests.Run(new Issue472_TryInterpret_and_Reduce_primitive_arithmetic_and_logical_expressions_during_the_compilation());
            fecTests.Run(new Issue473_InvalidProgramException_when_using_Expression_Condition_with_converted_decimal_expression());


            Console.WriteLine($"FEC tests are passing in {fecTestsStopwatch.ElapsedMilliseconds} ms.");


            var waitForLightStopwatch = Stopwatch.StartNew();
            lightTestsThread.Join(); // wait for the light tests to finish
            Console.WriteLine($"LightExpression tests + Thread New, Start, Join: {lightTestsStopwatch.ElapsedMilliseconds} ms.");
            Console.WriteLine($"--> waited for the LightExpression tests to finish after FEC tests complete for {waitForLightStopwatch.ElapsedMilliseconds} ms.");

            Console.WriteLine();
            if (fecTests.FailedTestCount > 0 || lightTests.FailedTestCount > 0)
            {
                // todo: @wip output the failed tests
                Console.WriteLine("ERROR: Some tests are FAILED!");
                Environment.Exit(1); // exit with error
                return;
            }

            var totalTestCount = fecTests.TotalTestCount + lightTests.TotalTestCount;
            Console.WriteLine($"TestX: {totalTestCount,-4} tests are passing in {totalStopwatch.ElapsedMilliseconds} ms.");
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
                    totalTestsPassed += testsPassed;
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

            ### .NET Framework 4.72: Running UnitTests and IssueTests in parallel...
            """);

            var sw = Stopwatch.StartNew();

            var unitTests = Task.Run(() =>
            {
                Run(new ArithmeticOperationsTests().Run);
                Run(new LightExpression.UnitTests.ArithmeticOperationsTests().Run);
                Run(new AssignTests().Run);
                Run(new LightExpression.UnitTests.AssignTests().Run);
                Run(new BinaryExpressionTests().Run);
                Run(new LightExpression.UnitTests.BinaryExpressionTests().Run);
                Run(new BlockTests().Run);
                Run(new LightExpression.UnitTests.BlockTests().Run);
                Run(new ClosureConstantTests().Run);
                Run(new LightExpression.UnitTests.ClosureConstantTests().Run);
                Run(new CoalesceTests().Run);
                Run(new LightExpression.UnitTests.CoalesceTests().Run);
                Run(new ConditionalOperatorsTests().Run);
                Run(new LightExpression.UnitTests.ConditionalOperatorsTests().Run);
                Run(new ConstantAndConversionTests().Run);
                Run(new LightExpression.UnitTests.ConstantAndConversionTests().Run);
                Run(new ConvertOperatorsTests().Run);
                Run(new LightExpression.UnitTests.ConvertOperatorsTests().Run);
                Run(new DefaultTests().Run);
                Run(new LightExpression.UnitTests.DefaultTests().Run);
                Run(new EqualityOperatorsTests().Run);
                Run(new LightExpression.UnitTests.EqualityOperatorsTests().Run);
                Run(new GotoTests().Run);
                Run(new LightExpression.UnitTests.GotoTests().Run);
                Run(new HoistedLambdaExprTests().Run);
                Run(new LightExpression.UnitTests.HoistedLambdaExprTests().Run);
                Run(new LoopTests().Run);
                Run(new LightExpression.UnitTests.LoopTests().Run);
                Run(new ListInitTests().Run);
                Run(new LightExpression.UnitTests.ListInitTests().Run);
                Run(new ManuallyComposedExprTests().Run);
                Run(new LightExpression.UnitTests.ManuallyComposedExprTests().Run);
                Run(new NestedLambdaTests().Run);
                Run(new LightExpression.UnitTests.NestedLambdaTests().Run);
                Run(new PreConstructedClosureTests().Run);
                Run(new LightExpression.UnitTests.PreConstructedClosureTests().Run);
                Run(new TryCatchTests().Run);
                Run(new LightExpression.UnitTests.TryCatchTests().Run);
                Run(new TypeBinaryExpressionTests().Run);
                Run(new LightExpression.UnitTests.TypeBinaryExpressionTests().Run);
                Run(new UnaryExpressionTests().Run);
                Run(new LightExpression.UnitTests.UnaryExpressionTests().Run);
                Run(new ValueTypeTests().Run);
                Run(new LightExpression.UnitTests.ValueTypeTests().Run);
                Run(new LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest().Run);
                Run(new LightExpression.UnitTests.LightExpressionTests().Run);
                Run(new ToCSharpStringTests().Run);
                Run(new LightExpression.UnitTests.ToCSharpStringTests().Run);

                Console.WriteLine($"{Environment.NewLine}UnitTests are passing in {sw.ElapsedMilliseconds} ms.");
            });

            var issueTests = Task.Run(() =>
            {
                Run(new Issue14_String_constant_comparisons_fail().Run);
                Run(new LightExpression.IssueTests.Issue14_String_constant_comparisons_fail().Run);
                Run(new Issue19_Nested_CallExpression_causes_AccessViolationException().Run);
                Run(new LightExpression.IssueTests.Issue19_Nested_CallExpression_causes_AccessViolationException().Run);
                Run(new Issue44_Conversion_To_Nullable_Throws_Exception().Run);
                Run(new LightExpression.IssueTests.Issue44_Conversion_To_Nullable_Throws_Exception().Run);
                Run(new Issue55_CompileFast_crash_with_ref_parameter().Run);
                Run(new LightExpression.IssueTests.Issue55_CompileFast_crash_with_ref_parameter().Run);
                Run(new Issue67_Equality_comparison_with_nullables_throws_at_delegate_invoke().Run);
                Run(new Issue71_Cannot_bind_to_the_target_method_because_its_signature().Run);
                Run(new LightExpression.IssueTests.Issue71_Cannot_bind_to_the_target_method_because_its_signature().Run);
                Run(new Issue72_Try_CompileFast_for_MS_Extensions_ObjectMethodExecutor().Run);
                Run(new Issue76_Expression_Convert_causing_signature_or_security_transparency_is_not_compatible_exception().Run);
                Run(new LightExpression.IssueTests.Issue76_Expression_Convert_causing_signature_or_security_transparency_is_not_compatible_exception().Run);
                Run(new Issue78_blocks_with_constant_return().Run);
                Run(new LightExpression.IssueTests.Issue78_blocks_with_constant_return().Run);
                Run(new Issue83_linq2db().Run);
                Run(new LightExpression.IssueTests.Issue83_linq2db().Run);
                Run(new Issue88_Constant_from_static_field().Run);
                Run(new LightExpression.IssueTests.Issue88_Constant_from_static_field().Run);
                Run(new Issue91_Issue95_Tests().Run);
                Run(new LightExpression.IssueTests.Issue91_Issue95_Tests().Run);
                Run(new Issue100_LightExpression_wrong_return_type().Run);
                Run(new LightExpression.IssueTests.Issue100_LightExpression_wrong_return_type().Run);
                Run(new Issue101_Not_supported_Assign_Modes().Run);
                Run(new LightExpression.IssueTests.Issue101_Not_supported_Assign_Modes().Run);
                Run(new Issue102_Label_and_Goto_Expression().Run);
                Run(new LightExpression.IssueTests.Issue102_Label_and_Goto_Expression().Run);
                Run(new Issue106_Power_support().Run);
                Run(new LightExpression.IssueTests.Issue106_Power_support().Run);
                Run(new Issue107_Assign_also_works_for_variables().Run);
                Run(new LightExpression.IssueTests.Issue107_Assign_also_works_for_variables().Run);
                Run(new Issue127_Switch_is_supported().Run);
                Run(new LightExpression.IssueTests.Issue127_Switch_is_supported().Run);
                Run(new Issue146_bool_par().Run);
                Run(new LightExpression.IssueTests.Issue146_bool_par().Run);
                Run(new Issue147_int_try_parse().Run);
                Run(new LightExpression.IssueTests.Issue147_int_try_parse().Run);
                Run(new Issue150_New_AttemptToReadProtectedMemory().Run);
                Run(new LightExpression.IssueTests.Issue150_New_AttemptToReadProtectedMemory().Run);
                Run(new Issue153_MinValueMethodNotSupported().Run);
                Run(new LightExpression.IssueTests.Issue153_MinValueMethodNotSupported().Run);
                Run(new Issue156_InvokeAction().Run);
                Run(new LightExpression.IssueTests.Issue156_InvokeAction().Run);
                Run(new Issue159_NumericConversions().Run);
                Run(new LightExpression.IssueTests.Issue159_NumericConversions().Run);
                Run(new Issue170_Serializer_Person_Ref().Run);
                Run(new LightExpression.IssueTests.Issue170_Serializer_Person_Ref().Run);
                Run(new Issue174_NullReferenceInSetter().Run);
                Run(new LightExpression.IssueTests.Issue174_NullReferenceInSetter().Run);
                Run(new Issue177_Cannot_compile_to_the_required_delegate_type_with_non_generic_CompileFast().Run);
                Run(new LightExpression.IssueTests.Issue177_Cannot_compile_to_the_required_delegate_type_with_non_generic_CompileFast().Run);
                Run(new Issue179_Add_something_like_LambdaExpression_CompileToMethod().Run);
                Run(new LightExpression.IssueTests.Issue179_Add_something_like_LambdaExpression_CompileToMethod().Run);
                Run(new Issue181_TryEmitIncDecAssign_InvalidCastException().Run);
                Run(new LightExpression.IssueTests.Issue181_TryEmitIncDecAssign_InvalidCastException().Run);
                // todo: @wip #453
                // Run(new Issue183_NullableDecimal().Run);
                // Run(new LightExpression.IssueTests.Issue183_NullableDecimal().Run);
                Run(new Issue190_Inc_Dec_Assign_Parent_Block_Var().Run);
                Run(new LightExpression.IssueTests.Issue190_Inc_Dec_Assign_Parent_Block_Var().Run);
                Run(new Issue196_AutoMapper_tests_are_failing_when_using_FEC().Run);
                Run(new LightExpression.IssueTests.Issue196_AutoMapper_tests_are_failing_when_using_FEC().Run);
                Run(new Issue197_Operation_could_destabilize_the_runtime().Run);
                Run(new LightExpression.IssueTests.Issue197_Operation_could_destabilize_the_runtime().Run);
                Run(new Issue204_Operation_could_destabilize_the_runtime__AutoMapper().Run);
                Run(new LightExpression.IssueTests.Issue204_Operation_could_destabilize_the_runtime__AutoMapper().Run);
                Run(new Issue209_AutoMapper_Operation_could_destabilize_the_runtime().Run);
                Run(new LightExpression.IssueTests.Issue209_AutoMapper_Operation_could_destabilize_the_runtime().Run);
                Run(new Issue243_Pass_Parameter_By_Ref_is_supported().Run);
                Run(new LightExpression.IssueTests.Issue243_Pass_Parameter_By_Ref_is_supported().Run);
                Run(new Nested_lambdas_assigned_to_vars().Run);
                Run(new LightExpression.IssueTests.Nested_lambdas_assigned_to_vars().Run);

                Run(new Issue252_Bad_code_gen_for_comparison_of_nullable_type_to_null().Run);
                Run(new LightExpression.IssueTests.Issue252_Bad_code_gen_for_comparison_of_nullable_type_to_null().Run);

                Run(new Issue248_Calling_method_with_in_out_parameters_in_expression_lead_to_NullReferenceException_on_calling_site().Run);
                Run(new LightExpression.IssueTests.Issue248_Calling_method_with_in_out_parameters_in_expression_lead_to_NullReferenceException_on_calling_site().Run);

                Run(new Issue251_Bad_code_gen_for_byRef_parameters().Run);
                Run(new LightExpression.IssueTests.Issue251_Bad_code_gen_for_byRef_parameters().Run);

                Run(new Issue261_Loop_wih_conditions_fails().Run);
                Run(new LightExpression.IssueTests.Issue261_Loop_wih_conditions_fails().Run);

                Run(new Issue274_Failing_Expressions_in_Linq2DB().Run);
                Run(new LightExpression.IssueTests.Issue274_Failing_Expressions_in_Linq2DB().Run);

                Run(new Issue281_Index_Out_of_Range().Run);
                Run(new LightExpression.IssueTests.Issue281_Index_Out_of_Range().Run);

                Run(new Issue284_Invalid_Program_after_Coalesce().Run);
                Run(new LightExpression.IssueTests.Issue284_Invalid_Program_after_Coalesce().Run);

                Run(new Issue293_Recursive_Methods().Run);
                Run(new LightExpression.IssueTests.Issue293_Recursive_Methods().Run);

                Run(new Issue300_Bad_label_content_in_ILGenerator_in_the_Mapster_benchmark_with_FEC_V3().Run);
                Run(new LightExpression.IssueTests.Issue300_Bad_label_content_in_ILGenerator_in_the_Mapster_benchmark_with_FEC_V3().Run);

                Run(new Issue302_Error_compiling_expression_with_array_access().Run);
                Run(new LightExpression.IssueTests.Issue302_Error_compiling_expression_with_array_access().Run);

                Run(new Issue305_CompileFast_generates_incorrect_code_with_arrays_and_printing().Run);
                Run(new LightExpression.IssueTests.Issue305_CompileFast_generates_incorrect_code_with_arrays_and_printing().Run);

                Run(new Issue307_Switch_with_fall_through_throws_InvalidProgramException().Run);
                Run(new LightExpression.IssueTests.Issue307_Switch_with_fall_through_throws_InvalidProgramException().Run);

                Run(new Issue308_Wrong_delegate_type_returned_with_closure().Run);
                Run(new LightExpression.IssueTests.Issue308_Wrong_delegate_type_returned_with_closure().Run);

                Run(new Issue309_InvalidProgramException_with_MakeBinary_liftToNull_true().Run);
                Run(new LightExpression.IssueTests.Issue309_InvalidProgramException_with_MakeBinary_liftToNull_true().Run);

                Run(new Issue310_InvalidProgramException_ignored_nullable().Run);
                Run(new LightExpression.IssueTests.Issue310_InvalidProgramException_ignored_nullable().Run);

                Run(new Issue314_LiftToNull_ToExpressionString().Run);
                Run(new LightExpression.IssueTests.Issue314_LiftToNull_ToExpressionString().Run);

                Run(new Issue316_in_parameter().Run);
                Run(new LightExpression.IssueTests.Issue316_in_parameter().Run);

                Run(new Issue320_Bad_label_content_in_ILGenerator_when_creating_through_DynamicModule().Run);
                Run(new LightExpression.IssueTests.Issue320_Bad_label_content_in_ILGenerator_when_creating_through_DynamicModule().Run);

                Run(new Issue321_Call_with_out_parameter_to_field_type_that_is_not_value_type_fails().Run);
                Run(new LightExpression.IssueTests.Issue321_Call_with_out_parameter_to_field_type_that_is_not_value_type_fails().Run);

                Run(new Issue322_NullableIntArgumentWithDefaultIntValue().Run);
                Run(new LightExpression.IssueTests.Issue322_NullableIntArgumentWithDefaultIntValue().Run);

                Run(new Issue333_AccessViolationException_and_other_suspicious_behavior_on_invoking_result_of_CompileFast().Run);
                Run(new LightExpression.IssueTests.Issue333_AccessViolationException_and_other_suspicious_behavior_on_invoking_result_of_CompileFast().Run);

                Run(new LightExpression.IssueTests.Issue346_Is_it_possible_to_implement_ref_local_variables().Run);

                Run(new Issue347_InvalidProgramException_on_compiling_an_expression_that_returns_a_record_which_implements_IList().Run);
                Run(new LightExpression.IssueTests.Issue347_InvalidProgramException_on_compiling_an_expression_that_returns_a_record_which_implements_IList().Run);

                Run(new Issue352_xxxAssign_does_not_work_with_MemberAccess().Run);
                Run(new LightExpression.IssueTests.Issue352_xxxAssign_does_not_work_with_MemberAccess().Run);

                Run(new Issue353_NullReferenceException_when_calling_CompileFast_results().Run);
                Run(new LightExpression.IssueTests.Issue353_NullReferenceException_when_calling_CompileFast_results().Run);

                Run(new Issue355_Error_with_converting_to_from_signed_unsigned_integers().Run);
                Run(new LightExpression.IssueTests.Issue355_Error_with_converting_to_from_signed_unsigned_integers().Run);

                Run(new LightExpression.IssueTests.Issue363_ActionFunc16Generics().Run);

                Run(new LightExpression.IssueTests.Issue365_Working_with_ref_return_values().Run);

                Run(new Issue366_FEC334_gives_incorrect_results_in_some_linq_operations().Run);

                Run(new Issue374_CompileFast_does_not_work_with_HasFlag().Run);
                Run(new LightExpression.IssueTests.Issue374_CompileFast_does_not_work_with_HasFlag().Run);

                Run(new Issue380_Comparisons_with_nullable_types().Run);
                Run(new LightExpression.IssueTests.Issue380_Comparisons_with_nullable_types().Run);

                Run(new Issue386_Value_can_not_be_null_in_Nullable_convert().Run);
                Run(new LightExpression.IssueTests.Issue386_Value_can_not_be_null_in_Nullable_convert().Run);

                Run(new Issue400_Fix_the_direct_assignment_of_Try_to_Member().Run);
                Run(new LightExpression.IssueTests.Issue400_Fix_the_direct_assignment_of_Try_to_Member().Run);

                Run(new Issue404_String_plus_parameter_causes_Exception_in_target_invocation().Run);
                Run(new LightExpression.IssueTests.Issue404_String_plus_parameter_causes_Exception_in_target_invocation().Run);

                Run(new Issue408_Dictionary_mapping_failing_when_the_InvocationExpression_inlining_is_involved().Run);
                Run(new LightExpression.IssueTests.Issue408_Dictionary_mapping_failing_when_the_InvocationExpression_inlining_is_involved().Run);

                Run(new Issue414_Incorrect_il_when_passing_by_ref_value().Run);
                Run(new LightExpression.IssueTests.Issue414_Incorrect_il_when_passing_by_ref_value().Run);

                Run(new Issue418_Wrong_output_when_comparing_NaN_value().Run);
                Run(new LightExpression.IssueTests.Issue418_Wrong_output_when_comparing_NaN_value().Run);

                Run(new Issue419_The_JIT_compiler_encountered_invalid_IL_code_or_an_internal_limitation().Run);
                Run(new LightExpression.IssueTests.Issue419_The_JIT_compiler_encountered_invalid_IL_code_or_an_internal_limitation().Run);

                Run(new Issue420_Nullable_DateTime_comparison_differs_from_Expression_Compile().Run);
                Run(new LightExpression.IssueTests.Issue420_Nullable_DateTime_comparison_differs_from_Expression_Compile().Run);

                Run(new Issue421_Date_difference_is_giving_wrong_negative_value().Run);

                Run(new Issue422_InvalidProgramException_when_having_TryCatch_Default_in_Catch().Run);
                Run(new LightExpression.IssueTests.Issue422_InvalidProgramException_when_having_TryCatch_Default_in_Catch().Run);

                Run(new Issue423_Converting_a_uint_to_a_float_gives_the_wrong_result().Run);
                Run(new LightExpression.IssueTests.Issue423_Converting_a_uint_to_a_float_gives_the_wrong_result().Run);

                Run(new Issue426_Directly_passing_a_method_result_to_another_method_by_ref_fails_with_InvalidProgramException().Run);
                Run(new LightExpression.IssueTests.Issue426_Directly_passing_a_method_result_to_another_method_by_ref_fails_with_InvalidProgramException().Run);

                Run(new Issue428_Expression_Switch_without_a_default_case_incorrectly_calls_first_case_for_unmatched_values().Run);
                Run(new LightExpression.IssueTests.Issue428_Expression_Switch_without_a_default_case_incorrectly_calls_first_case_for_unmatched_values().Run);

                Run(new Issue430_TryCatch_Bad_label_content_in_ILGenerator().Run);
                Run(new LightExpression.IssueTests.Issue430_TryCatch_Bad_label_content_in_ILGenerator().Run);

                Run(new Issue437_Shared_variables_with_nested_lambdas_returning_incorrect_values().Run);
                Run(new LightExpression.IssueTests.Issue437_Shared_variables_with_nested_lambdas_returning_incorrect_values().Run);

                Run(new Issue439_Support_unused_Field_access_in_Block().Run);
                Run(new LightExpression.IssueTests.Issue439_Support_unused_Field_access_in_Block().Run);

                Run(new Issue440_Errors_with_simplified_Switch_cases().Run);
                Run(new LightExpression.IssueTests.Issue440_Errors_with_simplified_Switch_cases().Run);

                Run(new Issue441_Fails_to_pass_Constant_as_call_parameter_by_reference().Run);
                Run(new LightExpression.IssueTests.Issue441_Fails_to_pass_Constant_as_call_parameter_by_reference().Run);

                Run(new Issue442_TryCatch_and_the_Goto_outside_problems().Run);
                Run(new LightExpression.IssueTests.Issue442_TryCatch_and_the_Goto_outside_problems().Run);

                Run(new Issue443_Nested_Calls_with_lambda_parameters().Run);
                Run(new LightExpression.IssueTests.Issue443_Nested_Calls_with_lambda_parameters().Run);

                Run(new Issue449_MemberInit_produces_InvalidProgram().Run);
                Run(new LightExpression.IssueTests.Issue449_MemberInit_produces_InvalidProgram().Run);

                Run(new Issue451_Operator_implicit_explicit_produces_InvalidProgram().Run);
                Run(new LightExpression.IssueTests.Issue451_Operator_implicit_explicit_produces_InvalidProgram().Run);

                Run(new Issue455_TypeAs_should_return_null().Run);
                Run(new LightExpression.IssueTests.Issue455_TypeAs_should_return_null().Run);

                Run(new Issue460_ArgumentException_when_converting_from_object_to_type_with_explicit_operator().Run);
                Run(new LightExpression.IssueTests.Issue460_ArgumentException_when_converting_from_object_to_type_with_explicit_operator().Run);

                Run(new Issue461_InvalidProgramException_when_null_checking_type_by_ref().Run);
                Run(new LightExpression.IssueTests.Issue461_InvalidProgramException_when_null_checking_type_by_ref().Run);

                Console.WriteLine($"{Environment.NewLine}IssueTests are passing in {sw.ElapsedMilliseconds} ms.");
            });

            Task.WaitAll(unitTests, issueTests);
            Console.WriteLine();
            if (failed)
            {
                Console.WriteLine("ERROR: Some tests are FAILED!");
                Environment.Exit(1); // error
                return;
            }

            Console.WriteLine($"ALL {totalTestsPassed,-4} tests are passing in {sw.ElapsedMilliseconds} ms.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using FastExpressionCompiler.FlatExpression;

using static FastExpressionCompiler.LightExpression.Expression;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.LightExpression.UnitTests
{
    public partial class LightExpressionTests : ITest
    {
        public int Run()
        {
            Can_roundtrip_compound_assignment_without_conversion();
            Can_roundtrip_compound_assignment_with_conversion();
            Can_compile_lambda_without_converting_to_expression();
            Can_compile_lambda_with_property();
            Can_compile_lambda_with_call_and_property();
            Nested_Func_using_outer_parameter();
            Nested_Action_using_outer_parameter_and_closed_value();
            Can_compile_complex_expr_with_Arrays_and_Casts();
            Can_compile_complex_expr_with_perf_tricks_with_Arrays_and_Casts();
            Can_embed_normal_Expression_into_LightExpression_eg_as_Constructor_argument();
            Should_output_the_System_and_LightExpression_to_the_identical_construction_syntax();
            Should_output_the_System_and_LightExpression_to_the_identical_CSharp_syntax();
            Expression_produced_by_ToExpressionString_should_compile();
            Multiple_methods_in_block_should_be_aligned_when_output_to_csharp();
            Can_roundtrip_light_expression_through_flat_expression();
            Flat_expression_preserves_parameter_and_label_identity_and_collects_closure_constants();
            Can_convert_dynamic_runtime_variables_and_debug_info_to_light_expression_and_flat_expression();
            Can_build_flat_expression_directly_with_light_expression_like_api();
            Can_build_flat_expression_control_flow_directly();
            Can_property_test_generated_flat_expression_roundtrip_structurally();

            Flat_lambda_parameter_ref_before_decl_preserves_identity();
            Flat_lambda_multiple_parameter_refs_all_yield_same_identity();
            Flat_block_variables_and_refs_yield_same_identity();
            Flat_nested_lambda_captures_outer_parameter_identity();
            Flat_lambda_closure_parameter_usages_track_captured_outer_parameter_during_direct_construction();
            Flat_lambda_closure_parameter_usages_propagate_across_nested_lambdas_during_direct_construction();
            Flat_lambda_closure_parameter_usages_track_captured_block_variable_during_direct_construction();
            Flat_lambda_closure_parameter_usages_track_captures_from_expression_conversion();
            Flat_out_of_order_decl_block_in_lambda_compiles_correctly();
            Flat_enum_constant_stored_inline_roundtrip();
            Flat_metadata_uses_16_bit_idx_storage();
            Flat_lambda_nodes_tracks_all_lambdas_during_direct_construction();
            Flat_lambda_nodes_tracks_deeply_nested_lambdas_during_direct_construction();
            Flat_lambda_nodes_tracks_lambdas_from_expression_conversion();
            Flat_lambda_nodes_has_single_entry_for_root_only_lambda();
            Flat_blocks_with_variables_tracked_during_direct_construction();
            Flat_goto_and_label_nodes_tracked_during_direct_construction();
            Flat_try_catch_nodes_tracked_during_direct_construction();
            Flat_blocks_with_variables_tracked_from_expression_conversion();
            Flat_goto_and_label_nodes_tracked_from_expression_conversion();
            Flat_try_catch_nodes_tracked_from_expression_conversion();
            Flat_equal_lambdas_with_different_parameter_names_are_structurally_equal_and_hash_equal();
            Flat_equal_nested_lambdas_with_captures_are_structurally_equal_and_hash_equal();
            Flat_standalone_parameters_use_name_in_structural_equality();
            Flat_structural_hash_supports_dictionary_lookup();
            Can_compile_nullable_boolean_and();
            Can_compile_nullable_boolean_or();
            Nullable_boolean_logic_emits_the_same_IL_shape_as_system();
            Coalesce_type_is_the_same_as_in_system_expressions();
            Coalesce_with_nullable_left_emits_the_same_IL_shape_as_system();
            Can_compile_shifts_with_nullable_shift_count();
            Can_compile_right_shift_with_nullable_shift_count();
            Type_is_and_type_equal_emit_the_short_IL();
            Type_is_and_type_equal_with_the_known_value_type_still_evaluate_the_operand();
            Can_round_trip_more_expression_kinds_through_system_expressions();
            Can_compile_type_is_and_type_equal_the_same_as_system();
            IsLifted_and_IsLiftedToNull_are_the_same_as_in_system_expressions();
            Lifted_comparisons_with_null_operand_and_other_nullable_types_are_the_same_as_system();
            Binary_operators_with_methods_and_nullable_operands_are_the_same_as_system();
            Unary_operators_with_nullable_and_user_defined_operands_are_the_same_as_system();
            Convert_with_the_method_taking_ref_param_is_the_same_as_system();
            Negate_checked_of_short_emits_the_short_IL();
            Compound_assign_and_inc_dec_with_methods_are_the_same_as_system();
            Convert_and_ConvertChecked_between_primitives_nullables_and_enums_are_the_same_as_system();
#if NET7_0_OR_GREATER
            Half_Int128_and_UInt128_constants_and_conversions_are_the_same_as_system();
#endif
            Unary_operators_with_explicit_method_are_the_same_as_system();
            Lifted_comparisons_have_the_same_type_and_result_as_system();
            And_AndAlso_OrElse_take_the_type_from_the_left_operand();
            Can_compile_nullable_boolean_and_also();
            Can_compile_nullable_boolean_or_else();
            Can_compile_nullable_boolean_not();
            Can_make_goto_of_all_kinds_with_value_and_type();
            Can_compile_goto_with_value_of_all_kinds();
            Can_compile_loop_with_break_and_continue_with_value();
            Can_print_new_expression_kinds();
            Reference_equality_is_converted_to_system_reference_comparison();
            Can_print_reference_comparison();
            Can_round_trip_new_expression_kinds_through_system_expressions();

            return 77;
        }

        private static int _sideEffectCalls;
        private static bool? SideEffect(bool? value) { ++_sideEffectCalls; return value; }

        public void Can_make_goto_of_all_kinds_with_value_and_type()
        {
            var label = Label(typeof(int), "label");
            var voidLabel = Label("voidLabel");
            var value = Constant(42);
            var kinds = new[] { GotoExpressionKind.Goto, GotoExpressionKind.Return, GotoExpressionKind.Break, GotoExpressionKind.Continue };

            foreach (var kind in kinds)
            {
                var plain = MakeGoto(kind, voidLabel, null, null);
                Asserts.AreEqual(kind, plain.Kind);
                Asserts.AreEqual(null, plain.Value);
                Asserts.AreEqual(typeof(void), plain.Type);

                var typed = MakeGoto(kind, voidLabel, null, typeof(string));
                Asserts.AreEqual(kind, typed.Kind);
                Asserts.AreEqual(null, typed.Value);
                Asserts.AreEqual(typeof(string), typed.Type);

                var valued = MakeGoto(kind, label, value, null);
                Asserts.AreEqual(kind, valued.Kind);
                Asserts.IsNotNull(valued.Value);

                var typedValued = MakeGoto(kind, label, value, typeof(string));
                Asserts.AreEqual(kind, typedValued.Kind);
                Asserts.IsNotNull(typedValued.Value);
                Asserts.AreEqual(typeof(string), typedValued.Type);

                // converting to System expressions validates and preserves the kind, value and type
                foreach (var g in new[] { plain, typed, valued, typedValued })
                {
                    var sys = (System.Linq.Expressions.GotoExpression)g.ToExpression();
                    Asserts.AreEqual(kind, sys.Kind);
                    Asserts.AreEqual(g.Value != null, sys.Value != null);
                    Asserts.AreEqual(g.Type, sys.Type);
                }
            }

            // the public factories agree with MakeGoto
            Asserts.AreEqual(GotoExpressionKind.Continue, Continue(label).Kind);
            Asserts.AreEqual(typeof(string), Continue(label, typeof(string)).Type);
            Asserts.AreEqual(GotoExpressionKind.Continue, Continue(label, value).Kind);
            Asserts.IsNotNull(Continue(label, value).Value);
            Asserts.AreEqual(null, Continue(label, (Expression)null).Value);
            Asserts.AreEqual(GotoExpressionKind.Continue, Continue(label, value, typeof(string)).Kind);
            Asserts.AreEqual(typeof(string), Continue(label, value, typeof(string)).Type);
            Asserts.AreEqual(GotoExpressionKind.Break, Break(label, value).Kind);
            Asserts.AreEqual(GotoExpressionKind.Break, Break(label, value, typeof(string)).Kind);
            Asserts.AreEqual(GotoExpressionKind.Return, Return(label, value).Kind);
            Asserts.AreEqual(GotoExpressionKind.Return, Return(label, value, typeof(string)).Kind);
            Asserts.AreEqual(GotoExpressionKind.Goto, Goto(label, value).Kind);
            Asserts.AreEqual(GotoExpressionKind.Goto, Goto(label, value, typeof(string)).Kind);
        }

        public void Can_compile_goto_with_value_of_all_kinds()
        {
            foreach (var kind in new[] { GotoExpressionKind.Goto, GotoExpressionKind.Return, GotoExpressionKind.Break, GotoExpressionKind.Continue })
                foreach (var withType in new[] { false, true })
                {
                    var label = Label(typeof(int), "label");
                    var @goto = MakeGoto(kind, label, Constant(42), withType ? typeof(void) : null);
                    var lambda = Lambda<Func<int>>(Block(@goto, Label(label, Constant(0))));

                    var sys = lambda.CompileSys();
                    Asserts.AreEqual(42, sys());

                    var fast = lambda.CompileFast(true);
                    Asserts.IsNotNull(fast);
                    Asserts.AreEqual(42, fast());
                }
        }

        public void Can_compile_loop_with_break_and_continue_with_value()
        {
            // sum of odd numbers below the limit, the loop result is provided by the `Break` with value
            foreach (var continueKind in new[] { GotoExpressionKind.Continue, GotoExpressionKind.Goto })
            {
                var limit = Parameter(typeof(int), "limit");
                var i = Parameter(typeof(int), "i");
                var sum = Parameter(typeof(int), "sum");
                var breakLabel = Label(typeof(int), "break");
                var continueLabel = Label("continue");

                var body = Block(
                    IfThen(GreaterThanOrEqual(PreIncrementAssign(i), limit), Break(breakLabel, sum)),
                    IfThen(Equal(Modulo(i, Constant(2)), Constant(0)), MakeGoto(continueKind, continueLabel, null, null)),
                    AddAssign(sum, i));

                var lambda = Lambda<Func<int, int>>(
                    Block(new[] { i, sum }, Loop(body, breakLabel, continueLabel)),
                    limit);

                var sys = lambda.CompileSys();
                Asserts.AreEqual(1 + 3 + 5 + 7, sys(9));

                var fast = lambda.CompileFast(true);
                Asserts.IsNotNull(fast);
                Asserts.AreEqual(sys(9), fast(9));
                Asserts.AreEqual(sys(1), fast(1));
            }
        }

        public void Can_print_new_expression_kinds()
        {
            var intLabel = Label(typeof(int), "intLabel");
            var voidLabel = Label("voidLabel");
            var nl = Parameter(typeof(int?), "nl");
            var l = Parameter(typeof(bool?), "l");
            var r = Parameter(typeof(bool?), "r");

            // Continue with the value and Continue with the value and type
            var continueValue = Continue(intLabel, Constant(42));
            var continueTypedValue = Continue(intLabel, Constant(42), typeof(void));
            foreach (var c in new[] { continueValue, continueTypedValue })
            {
                var es = c.ToExpressionString();
                Asserts.Contains("MakeGoto(System.Linq.Expressions.GotoExpressionKind.Continue,", es);
                Asserts.Contains("Label(typeof(int), \"intLabel\")", es);
                Asserts.Contains("Constant(42)", es);
                // a value goto is printed as the return of the value, the same way as for the Break with value
                Asserts.AreEqual("return 42;", c.ToCSharpString());
            }
            Asserts.Contains("typeof(int));", continueValue.ToExpressionString());
            Asserts.Contains("typeof(void));", continueTypedValue.ToExpressionString());
            Asserts.AreEqual(Break(intLabel, Constant(42)).ToCSharpString(), continueValue.ToCSharpString());

            // typed Continue without the value is printed as goto
            var continueTyped = Continue(voidLabel, typeof(string));
            var typedEs = continueTyped.ToExpressionString();
            Asserts.Contains("MakeGoto(System.Linq.Expressions.GotoExpressionKind.Continue,", typedEs);
            Asserts.Contains("typeof(string));", typedEs);
            Asserts.Contains("goto voidLabel", continueTyped.ToCSharpString());

            // nullable shift via the TypedBinaryExpression
            var shift = LeftShift(Constant(16), nl);
            Asserts.AreEqual(typeof(int?), shift.Type);
            Asserts.Contains("MakeBinary(ExpressionType.LeftShift,", shift.ToExpressionString());
            Asserts.Contains("Parameter(typeof(int?), \"nl\")", shift.ToExpressionString());
            Asserts.AreEqual("16 << nl", shift.ToCSharpString());

            // nullable And, AndAlso, OrElse
            Asserts.Contains("MakeBinary(ExpressionType.And,", And(l, r).ToExpressionString());
            Asserts.Contains("MakeBinary(ExpressionType.AndAlso,", AndAlso(l, r).ToExpressionString());
            Asserts.Contains("MakeBinary(ExpressionType.OrElse,", OrElse(l, r).ToExpressionString());
            Asserts.AreEqual("l & r", And(l, r).ToCSharpString());
            Asserts.AreEqual("l && r", AndAlso(l, r).ToCSharpString());
            Asserts.AreEqual("l || r", OrElse(l, r).ToCSharpString());

            // nullable boolean Not is the logical `!` and not the bitwise `~`
            Asserts.AreEqual("!(l)", Not(l).ToCSharpString());
            Asserts.AreEqual("~(nl)", Not(nl).ToCSharpString());
            Asserts.AreEqual("!(b)", Not(Parameter(typeof(bool), "b")).ToCSharpString());
        }

        public void Reference_equality_is_converted_to_system_reference_comparison()
        {
            var a = Parameter(typeof(string), "a");
            var b = Parameter(typeof(string), "b");

            var refEq = ReferenceEqual(a, b);
            var refNotEq = ReferenceNotEqual(a, b);
            Asserts.AreEqual(typeof(bool), refEq.Type);

            // the plain Equal on strings must still go to the value comparison via the op_Equality
            var valueEq = (System.Linq.Expressions.BinaryExpression)Equal(a, b).ToExpression();
            Asserts.IsNotNull(valueEq.Method);

            var sysRefEq = (System.Linq.Expressions.BinaryExpression)refEq.ToExpression();
            Asserts.AreEqual(ExpressionType.Equal, sysRefEq.NodeType);
            Asserts.AreEqual(null, sysRefEq.Method);
            var sysRefNotEq = (System.Linq.Expressions.BinaryExpression)refNotEq.ToExpression();
            Asserts.AreEqual(ExpressionType.NotEqual, sysRefNotEq.NodeType);
            Asserts.AreEqual(null, sysRefNotEq.Method);

            var s1 = new string('x', 3);
            var s2 = new string('x', 3);
            Asserts.IsTrue(!ReferenceEquals(s1, s2) && s1 == s2);

            var eqLambda = Lambda<Func<string, string, bool>>(refEq, a, b);
            var notEqLambda = Lambda<Func<string, string, bool>>(refNotEq, a, b);
            var valueEqLambda = Lambda<Func<string, string, bool>>(Equal(a, b), a, b);

            var eqSys = eqLambda.CompileSys();
            Asserts.AreEqual(false, eqSys(s1, s2));
            Asserts.AreEqual(true, eqSys(s1, s1));
            var notEqSys = notEqLambda.CompileSys();
            Asserts.AreEqual(true, notEqSys(s1, s2));
            Asserts.AreEqual(false, notEqSys(s1, s1));
            Asserts.AreEqual(true, valueEqLambda.CompileSys()(s1, s2));

            var eqFast = eqLambda.CompileFast(true);
            Asserts.IsNotNull(eqFast);
            Asserts.AreEqual(false, eqFast(s1, s2));
            Asserts.AreEqual(true, eqFast(s1, s1));
            var notEqFast = notEqLambda.CompileFast(true);
            Asserts.IsNotNull(notEqFast);
            Asserts.AreEqual(true, notEqFast(s1, s2));
            Asserts.AreEqual(false, notEqFast(s1, s1));

            // the reference comparison with null
            var nullEq = Lambda<Func<string, bool>>(ReferenceEqual(a, Constant(null, typeof(string))), a);
            Asserts.AreEqual(true, nullEq.CompileSys()(null));
            Asserts.AreEqual(false, nullEq.CompileSys()(s1));
            Asserts.AreEqual(true, nullEq.CompileFast(true)(null));
            Asserts.AreEqual(false, nullEq.CompileFast(true)(s1));

            // converting System expressions back to Light
            var sysA = SysExpr.Parameter(typeof(string), "a");
            var sysB = SysExpr.Parameter(typeof(string), "b");
            var backRefEq = SysExpr.ReferenceEqual(sysA, sysB).ToLightExpression();
            var backRefNotEq = SysExpr.ReferenceNotEqual(sysA, sysB).ToLightExpression();
            var backValueEq = SysExpr.Equal(sysA, sysB).ToLightExpression();

            Asserts.AreEqual("object.ReferenceEquals(a, b)", backRefEq.ToCSharpString());
            Asserts.AreEqual("!object.ReferenceEquals(a, b)", backRefNotEq.ToCSharpString());
            Asserts.AreEqual("a == b", backValueEq.ToCSharpString());

            var backLambda = SysExpr.Lambda<Func<string, string, bool>>(SysExpr.ReferenceEqual(sysA, sysB), sysA, sysB)
                .ToLightExpression();
            Asserts.AreEqual(false, backLambda.CompileFast()(s1, s2));
            Asserts.AreEqual(true, backLambda.CompileFast()(s1, s1));

            // the round trip Light -> Sys -> Light keeps the reference comparison
            var roundTrip = refNotEq.ToExpression().ToLightExpression();
            Asserts.AreEqual("!object.ReferenceEquals(a, b)", roundTrip.ToCSharpString());
        }

        public void Can_round_trip_new_expression_kinds_through_system_expressions()
        {
            // Light -> Sys -> Light, then the round-tripped lambda must compile and behave like the original
            static Func<T1, T2, R> RoundTrip<T1, T2, R>(LambdaExpression light)
            {
                var back = (LambdaExpression)light.ToExpression().ToLightExpression();
                var expected = (Func<T1, T2, R>)light.CompileSys();
                var actual = back.CompileFast<Func<T1, T2, R>>(true);
                Asserts.IsNotNull(actual);
                return (x, y) =>
                {
                    Asserts.AreEqual(expected(x, y), actual(x, y));
                    return actual(x, y);
                };
            }

            var l = Parameter(typeof(bool?), "l");
            var r = Parameter(typeof(bool?), "r");
            bool?[] values = { null, true, false };

            var kinds = new Func<Expression, Expression, Expression>[]
            {
                (a, b) => And(a, b),
                (a, b) => Or(a, b),
                (a, b) => AndAlso(a, b),
                (a, b) => OrElse(a, b),
            };
            foreach (var make in kinds)
            {
                var f = RoundTrip<bool?, bool?, bool?>(Lambda<Func<bool?, bool?, bool?>>(make(l, r), l, r));
                foreach (var x in values)
                    foreach (var y in values)
                        f(x, y);
            }

            var not = RoundTrip<bool?, bool?, bool?>(Lambda<Func<bool?, bool?, bool?>>(Not(l), l, r));
            foreach (var x in values)
                not(x, null);

            var n = Parameter(typeof(int?), "n");
            var c = Parameter(typeof(int?), "c");
            var shl = RoundTrip<int?, int?, int?>(Lambda<Func<int?, int?, int?>>(LeftShift(n, c), n, c));
            var shr = RoundTrip<int?, int?, int?>(Lambda<Func<int?, int?, int?>>(RightShift(n, c), n, c));
            foreach (int? x in new int?[] { null, 1, 8 })
                foreach (int? y in new int?[] { null, 1, 2 })
                {
                    shl(x, y);
                    shr(x, y);
                }

            // goto with a value: Break and Continue to a typed label
            var p = Parameter(typeof(int), "p");
            var label = Label(typeof(int), "done");
            var breakWithValue = RoundTrip<int, int, int>(Lambda<Func<int, int, int>>(
                Block(
                    IfThen(Equal(p, Constant(0)), Break(label, Constant(42))),
                    Label(label, Constant(7))),
                p, Parameter(typeof(int), "q")));
            Asserts.AreEqual(42, breakWithValue(0, 0));
            Asserts.AreEqual(7, breakWithValue(1, 0));

            var continueWithValue = RoundTrip<int, int, int>(Lambda<Func<int, int, int>>(
                Block(
                    IfThen(Equal(p, Constant(0)), Continue(label, Constant(5))),
                    Label(label, Constant(9))),
                p, Parameter(typeof(int), "q")));
            Asserts.AreEqual(5, continueWithValue(0, 0));
            Asserts.AreEqual(9, continueWithValue(1, 0));
        }

        public void Can_print_reference_comparison()
        {
            var a = Parameter(typeof(string), "a");
            var b = Parameter(typeof(string), "b");

            var refEq = ReferenceEqual(a, b);
            var refNotEq = ReferenceNotEqual(a, b);
            var valueEq = Equal(a, b);

            var refEqEs = refEq.ToExpressionString();
            Asserts.Contains("ReferenceEqual(", refEqEs);
            Asserts.Contains("Parameter(typeof(string), \"a\")", refEqEs);
            Asserts.DoesNotContain("MakeBinary", refEqEs);
            Asserts.Contains("ReferenceNotEqual(", refNotEq.ToExpressionString());
            Asserts.DoesNotContain("MakeBinary", refNotEq.ToExpressionString());

            // the plain Equal is still printed as before
            Asserts.Contains("MakeBinary(ExpressionType.Equal,", valueEq.ToExpressionString());
            Asserts.DoesNotContain("ReferenceEqual", valueEq.ToExpressionString());

            Asserts.AreEqual("object.ReferenceEquals(a, b)", refEq.ToCSharpString());
            Asserts.AreEqual("!object.ReferenceEquals(a, b)", refNotEq.ToCSharpString());
            Asserts.AreEqual("a == b", valueEq.ToCSharpString());
        }

        private static int AndInts(int left, int right) => left & right;

        public void Can_compile_nullable_boolean_not()
        {
            var p = Parameter(typeof(bool?), "p");
            var not = Not(p);
            Asserts.AreEqual(typeof(bool?), not.Type);

            var lambda = Lambda<Func<bool?, bool?>>(not, p);
            var fast = lambda.CompileFast(true);
            Asserts.IsNotNull(fast);
            var sys = lambda.CompileSys();

            Asserts.AreEqual(false, sys(true));
            Asserts.AreEqual(true, sys(false));
            Asserts.AreEqual(null, sys(null));
            foreach (var v in new bool?[] { true, false, null })
                Asserts.AreEqual(sys(v), fast(v));
        }

        public void Can_compile_nullable_boolean_and_also() => CheckNullableShortCircuit(true);

        public void Can_compile_nullable_boolean_or_else() => CheckNullableShortCircuit(false);

        private void CheckNullableShortCircuit(bool isAnd)
        {
            Func<Expression, Expression, BinaryExpression> make = isAnd ? AndAlso : OrElse;
            Func<Expression, Expression, MethodInfo, BinaryExpression> makeWithMethod = isAnd ? AndAlso : OrElse;
            var expectedShortValue = !isAnd;

            var left = Parameter(typeof(bool?), "left");
            var right = Parameter(typeof(bool?), "right");
            var expr = make(left, right);
            Asserts.AreEqual(typeof(bool?), expr.Type);

            var lambda = Lambda<Func<bool?, bool?, bool?>>(expr, left, right);
            var fast = lambda.CompileFast(true);
            Asserts.IsNotNull(fast);
            var sys = lambda.CompileSys();

            bool? Expected(bool? l, bool? r) => isAnd
                ? (l == false || r == false ? false : l == null || r == null ? null : (bool?)true)
                : (l == true || r == true ? true : l == null || r == null ? null : (bool?)false);

            bool?[] values = { true, false, null };
            foreach (var l in values)
                foreach (var r in values)
                {
                    var sysResult = sys(l, r);
                    Asserts.AreEqual(Expected(l, r), sysResult);
                    Asserts.AreEqual(sysResult, fast(l, r));
                }

            // the right operand must not be evaluated when the left one decides the result
            var sideEffect = typeof(LightExpressionTests).GetMethod(nameof(SideEffect), BindingFlags.Static | BindingFlags.NonPublic);
            var sideEffectLambda = Lambda<Func<bool?, bool?>>(make(left, Call(sideEffect, Constant(null, typeof(bool?)))), left);
            var sideEffectFast = sideEffectLambda.CompileFast(true);
            Asserts.IsNotNull(sideEffectFast);
            _sideEffectCalls = 0;
            Asserts.AreEqual(expectedShortValue, sideEffectFast(expectedShortValue));
            Asserts.AreEqual(0, _sideEffectCalls);
            Asserts.AreEqual(null, sideEffectFast(null));
            Asserts.AreEqual(1, _sideEffectCalls);
            Asserts.AreEqual(null, sideEffectFast(!expectedShortValue));
            Asserts.AreEqual(2, _sideEffectCalls);

            var withNullMethod = makeWithMethod(left, right, null);
            Asserts.AreEqual(typeof(bool?), withNullMethod.Type);
            var nullMethodLambda = Lambda<Func<bool?, bool?, bool?>>(withNullMethod, left, right);
            var nullMethodFast = nullMethodLambda.CompileFast(true);
            Asserts.IsNotNull(nullMethodFast);
            var nullMethodSys = nullMethodLambda.CompileSys();
            foreach (var l in values)
                foreach (var r in values)
                    Asserts.AreEqual(nullMethodSys(l, r), nullMethodFast(l, r));

            Asserts.Throws<InvalidOperationException>(() =>
                makeWithMethod(Parameter(typeof(bool), "nonNullableLeft"), right, null));
            Asserts.Throws<InvalidOperationException>(() =>
                makeWithMethod(left, Parameter(typeof(int?), "unrelatedRight"), null));

            var incompatibleMethod = typeof(LightExpressionTests).GetMethod(
                nameof(AndInts), BindingFlags.Static | BindingFlags.NonPublic);
            Asserts.Throws<InvalidOperationException>(() => makeWithMethod(left, right, incompatibleMethod));
        }

        public void Can_compile_nullable_boolean_and()
        {
            var left = Parameter(typeof(bool?), "left");
            var right = Parameter(typeof(bool?), "right");
            var and = And(left, right);
            Asserts.AreEqual(typeof(bool?), and.Type);

            var lambda = Lambda<Func<bool?, bool?, bool?>>(and, left, right);
            var fast = lambda.CompileFast(true);
            Asserts.IsNotNull(fast);
            var sys = lambda.CompileSys();

            void AssertResult(bool? leftValue, bool? rightValue, bool? expected)
            {
                var sysResult = sys(leftValue, rightValue);
                Asserts.AreEqual(expected, sysResult);
                Asserts.AreEqual(sysResult, fast(leftValue, rightValue));
            }

            AssertResult(true, true, true);
            AssertResult(true, false, false);
            AssertResult(false, true, false);
            AssertResult(false, false, false);
            AssertResult(true, null, null);
            AssertResult(false, null, false);
            AssertResult(null, true, null);
            AssertResult(null, false, false);
            AssertResult(null, null, null);

            var andWithNullMethod = And(left, right, (MethodInfo)null);
            Asserts.AreEqual(typeof(bool?), andWithNullMethod.Type);
            var nullMethodLambda = Lambda<Func<bool?, bool?, bool?>>(andWithNullMethod, left, right);
            var nullMethodFast = nullMethodLambda.CompileFast(true);
            Asserts.IsNotNull(nullMethodFast);
            var nullMethodSys = nullMethodLambda.CompileSys();
            Asserts.AreEqual(nullMethodSys(false, null), nullMethodFast(false, null));

            var nonNullableLeft = Parameter(typeof(bool), "nonNullableLeft");
            Asserts.Throws<InvalidOperationException>(() =>
                And(nonNullableLeft, right, (MethodInfo)null));
            Asserts.Throws<InvalidOperationException>(() =>
                And(left, Parameter(typeof(int?), "unrelatedRight"), (MethodInfo)null));

            var incompatibleMethod = typeof(LightExpressionTests).GetMethod(
                nameof(AndInts), BindingFlags.Static | BindingFlags.NonPublic);
            Asserts.IsNotNull(incompatibleMethod);
            Asserts.Throws<InvalidOperationException>(() => And(left, right, incompatibleMethod));
        }

        public void Coalesce_with_nullable_left_emits_the_same_IL_shape_as_system()
        {
            var ni = Parameter(typeof(int?), "ni");
            var nl = Parameter(typeof(long?), "nl");

            Lambda<Func<int?, int>>(Coalesce(ni, Constant(5)), ni)
                .CompileFast(true, CompilerFlags.EnableDelegateDebugInfo).AssertOpCodes(
                    OpCodes.Ldarg_1, OpCodes.Stloc_0, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Brfalse,
                    OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Br, OpCodes.Ldc_I4_5, OpCodes.Ret);

            Lambda<Func<int?, long>>(Coalesce(ni, Constant(5L)), ni)
                .CompileFast(true, CompilerFlags.EnableDelegateDebugInfo).AssertOpCodes(
                    OpCodes.Ldarg_1, OpCodes.Stloc_0, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Brfalse,
                    OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Conv_I8, OpCodes.Br, OpCodes.Ldc_I8, OpCodes.Ret);

            Lambda<Func<int?, double>>(Coalesce(ni, Constant(5.5)), ni)
                .CompileFast(true, CompilerFlags.EnableDelegateDebugInfo).AssertOpCodes(
                    OpCodes.Ldarg_1, OpCodes.Stloc_0, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Brfalse,
                    OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Conv_R8, OpCodes.Br, OpCodes.Ldc_R8, OpCodes.Ret);

            Lambda<Func<long?, long>>(Coalesce(nl, Constant(5)), nl)
                .CompileFast(true, CompilerFlags.EnableDelegateDebugInfo).AssertOpCodes(
                    OpCodes.Ldarg_1, OpCodes.Stloc_0, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Brfalse,
                    OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Br, OpCodes.Ldc_I4_5, OpCodes.Conv_I8, OpCodes.Ret);

            Lambda<Func<int?, long?, long?>>(Coalesce(ni, nl), ni, nl)
                .CompileFast(true, CompilerFlags.EnableDelegateDebugInfo).AssertOpCodes(
                    OpCodes.Ldarg_1, OpCodes.Stloc_0, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Brfalse,
                    OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Conv_I8, OpCodes.Newobj, OpCodes.Br, OpCodes.Ldarg_2, OpCodes.Ret);
        }
        public void Coalesce_type_is_the_same_as_in_system_expressions()
        {
            var ni = Parameter(typeof(int?), "ni");
            var nl = Parameter(typeof(long?), "nl");
            var s = Parameter(typeof(string), "s");
            var sysNi = SysExpr.Parameter(typeof(int?), "ni");
            var sysNl = SysExpr.Parameter(typeof(long?), "nl");
            var sysS = SysExpr.Parameter(typeof(string), "s");

            // int? ?? int -> int, int? ?? long -> long (widening of the underlying type), int? ?? double -> double
            var intOrInt = Coalesce(ni, Constant(5));
            Asserts.AreEqual(SysExpr.Coalesce(sysNi, SysExpr.Constant(5)).Type, intOrInt.Type);
            Asserts.AreEqual(typeof(int), intOrInt.Type);

            var intOrLong = Coalesce(ni, Constant(5L));
            Asserts.AreEqual(SysExpr.Coalesce(sysNi, SysExpr.Constant(5L)).Type, intOrLong.Type);
            Asserts.AreEqual(typeof(long), intOrLong.Type);

            var intOrDouble = Coalesce(ni, Constant(5.5));
            Asserts.AreEqual(SysExpr.Coalesce(sysNi, SysExpr.Constant(5.5)).Type, intOrDouble.Type);
            Asserts.AreEqual(typeof(double), intOrDouble.Type);

            // int? ?? int? and long? ?? int stay nullable or widen as in BCL
            var intOrNullableInt = Coalesce(ni, Parameter(typeof(int?), "other"));
            Asserts.AreEqual(typeof(int?), intOrNullableInt.Type);

            var nullableLongOrInt = Coalesce(nl, Constant(5));
            Asserts.AreEqual(SysExpr.Coalesce(sysNl, SysExpr.Constant(5)).Type, nullableLongOrInt.Type);
            Asserts.AreEqual(typeof(long), nullableLongOrInt.Type);

            // int? ?? long? -> long?
            var intOrNullableLong = Coalesce(ni, nl);
            Asserts.AreEqual(SysExpr.Coalesce(sysNi, sysNl).Type, intOrNullableLong.Type);
            Asserts.AreEqual(typeof(long?), intOrNullableLong.Type);

            // reference types
            var strOrObj = Coalesce(s, Constant(new object()));
            Asserts.AreEqual(typeof(object), strOrObj.Type);
            var strOrStr = Coalesce(s, Constant("x"));
            Asserts.AreEqual(typeof(string), strOrStr.Type);

            // the result of compiled lambdas is the same as of the System ones
            var longLambda = Lambda<Func<int?, long>>(intOrLong, ni);
            var longSys = longLambda.CompileSys();
            var longFast = longLambda.CompileFast(true);
            Asserts.AreEqual(5L, longSys(null));
            Asserts.AreEqual(5L, longFast(null));
            Asserts.AreEqual(7L, longSys(7));
            Asserts.AreEqual(7L, longFast(7));

            var doubleLambda = Lambda<Func<int?, double>>(intOrDouble, ni);
            var doubleSys = doubleLambda.CompileSys();
            var doubleFast = doubleLambda.CompileFast(true);
            Asserts.AreEqual(5.5, doubleSys(null));
            Asserts.AreEqual(5.5, doubleFast(null));
            Asserts.AreEqual(7.0, doubleSys(7));
            Asserts.AreEqual(7.0, doubleFast(7));

            var nullableLongLambda = Lambda<Func<int?, long?, long?>>(intOrNullableLong, ni, nl);
            var nlSys = nullableLongLambda.CompileSys();
            var nlFast = nullableLongLambda.CompileFast(true);
            foreach (int? a in new int?[] { null, 3 })
                foreach (long? b in new long?[] { null, 9 })
                    Asserts.AreEqual(nlSys(a, b), nlFast(a, b));

            // the conversion to System expression keeps the type
            Asserts.AreEqual(typeof(long), intOrLong.ToExpression().Type);

            // incompatible operands still throw
            Asserts.Throws<ArgumentException>(() => Coalesce(ni, Constant("x")));
            Asserts.Throws<ArgumentException>(() => Coalesce(Parameter(typeof(DateTime?), "d"), Constant(1)));
        }

        public void Nullable_boolean_logic_emits_the_same_IL_shape_as_system()
        {
            var l = Parameter(typeof(bool?), "l");
            var r = Parameter(typeof(bool?), "r");

            Delegate Fast(Expression body) =>
                Lambda<Func<bool?, bool?, bool?>>(body, l, r).CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);

            Fast(And(l, r)).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Stloc_0, OpCodes.Ldarg_2, OpCodes.Stloc_1,
                OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Brtrue,
                OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Or, OpCodes.Brfalse,
                OpCodes.Ldloc_0, OpCodes.Br, OpCodes.Ldloc_1, OpCodes.Ret);

            Fast(Or(l, r)).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Stloc_0, OpCodes.Ldarg_2, OpCodes.Stloc_1,
                OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Brtrue,
                OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Or, OpCodes.Brfalse,
                OpCodes.Ldloc_1, OpCodes.Br, OpCodes.Ldloc_0, OpCodes.Ret);

            Fast(AndAlso(l, r)).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Stloc_0,
                OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Brtrue,
                OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Brtrue,
                OpCodes.Ldarg_2, OpCodes.Stloc_1,
                OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Brtrue,
                OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Brtrue,
                OpCodes.Ldloc_1, OpCodes.Br, OpCodes.Ldloc_0, OpCodes.Ret);

            Fast(OrElse(l, r)).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Stloc_0,
                OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Brtrue,
                OpCodes.Ldarg_2, OpCodes.Stloc_1,
                OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Or, OpCodes.Brfalse,
                OpCodes.Ldloc_1, OpCodes.Br, OpCodes.Ldloc_0, OpCodes.Ret);

            Fast(Not(l)).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Stloc_0,
                OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Brfalse,
                OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Ldc_I4_0, OpCodes.Ceq, OpCodes.Newobj, OpCodes.Br,
                OpCodes.Ldloc_0, OpCodes.Ret);
        }

        public void Can_compile_nullable_boolean_or()
        {
            var left = Parameter(typeof(bool?), "left");
            var right = Parameter(typeof(bool?), "right");
            var or = Or(left, right);
            Asserts.AreEqual(typeof(bool?), or.Type);

            var lambda = Lambda<Func<bool?, bool?, bool?>>(or, left, right);
            var fast = lambda.CompileFast(true);
            Asserts.IsNotNull(fast);
            var sys = lambda.CompileSys();

            void AssertResult(bool? l, bool? r, bool? expected)
            {
                Asserts.AreEqual(expected, sys(l, r));
                Asserts.AreEqual(expected, fast(l, r));
            }

            AssertResult(true, true, true);
            AssertResult(true, false, true);
            AssertResult(false, true, true);
            AssertResult(false, false, false);
            AssertResult(true, null, true);
            AssertResult(false, null, null);
            AssertResult(null, true, true);
            AssertResult(null, false, null);
            AssertResult(null, null, null);

            var withNullMethod = Or(left, right, (MethodInfo)null);
            var nullMethodFast = Lambda<Func<bool?, bool?, bool?>>(withNullMethod, left, right).CompileFast(true);
            Asserts.AreEqual(true, nullMethodFast(null, true));
            Asserts.AreEqual(null, nullMethodFast(null, false));
        }

        public void And_AndAlso_OrElse_take_the_type_from_the_left_operand()
        {
            var i = Parameter(typeof(int), "i");
            var j = Parameter(typeof(int), "j");
            var b = Parameter(typeof(bool), "b");
            var c = Parameter(typeof(bool), "c");
            var sysI = SysExpr.Parameter(typeof(int), "i");
            var sysJ = SysExpr.Parameter(typeof(int), "j");

            // bitwise And/Or on integers is of the integer type, as in System
            Asserts.AreEqual(SysExpr.And(sysI, sysJ).Type, And(i, j).Type);
            Asserts.AreEqual(typeof(int), And(i, j).Type);
            Asserts.AreEqual(typeof(int), Or(i, j).Type);
            Asserts.AreEqual(typeof(int), And(i, j).ToExpression().Type);

            var andInts = Lambda<Func<int, int, int>>(And(i, j), i, j);
            Asserts.AreEqual(andInts.CompileSys()(12, 10), andInts.CompileFast(true)(12, 10));
            Asserts.AreEqual(8, andInts.CompileFast(true)(12, 10));

            var longs = Parameter(typeof(long), "l");
            Asserts.AreEqual(typeof(long), And(longs, longs).Type);

            // bool operands stay logical
            Asserts.AreEqual(typeof(bool), And(b, c).Type);
            Asserts.AreEqual(typeof(bool), AndAlso(b, c).Type);
            Asserts.AreEqual(typeof(bool), OrElse(b, c).Type);
            var andBools = Lambda<Func<bool, bool, bool>>(And(b, c), b, c);
            Asserts.AreEqual(false, andBools.CompileFast(true)(true, false));
            Asserts.AreEqual(true, andBools.CompileFast(true)(true, true));

            // bool? operands are lifted
            var nb = Parameter(typeof(bool?), "nb");
            Asserts.AreEqual(typeof(bool?), And(nb, nb).Type);
            Asserts.AreEqual(typeof(bool?), AndAlso(nb, nb).Type);
            Asserts.AreEqual(typeof(bool?), OrElse(nb, nb).Type);
        }

        public struct LiftMoney
        {
            public int V;
            public LiftMoney(int v) { V = v; }
            public static bool operator ==(LiftMoney a, LiftMoney b) => a.V == b.V;
            public static bool operator !=(LiftMoney a, LiftMoney b) => a.V != b.V;
            public static bool operator <(LiftMoney a, LiftMoney b) => a.V < b.V;
            public static bool operator >(LiftMoney a, LiftMoney b) => a.V > b.V;
            public static bool operator <=(LiftMoney a, LiftMoney b) => a.V <= b.V;
            public static bool operator >=(LiftMoney a, LiftMoney b) => a.V >= b.V;
            public override bool Equals(object o) => o is LiftMoney m && m.V == V;
            public override int GetHashCode() => V;
        }
        public enum LiftEnum { A, B, C }

        public void Lifted_comparisons_with_null_operand_and_other_nullable_types_are_the_same_as_system()
        {
            var names = new[] { "Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual" };
            var cases = new (Type, object, object)[]
            {
                (typeof(int?), 1, 2), (typeof(long?), 1L, 2L), (typeof(double?), 1.0, 2.0), (typeof(uint?), 1u, 2u),
                (typeof(char?), 'a', 'b'), (typeof(LiftEnum?), LiftEnum.A, LiftEnum.B), (typeof(bool?), true, false),
                (typeof(decimal?), 1m, 2m), (typeof(DateTime?), new DateTime(2000, 1, 1), new DateTime(2001, 1, 1)),
                (typeof(LiftMoney?), new LiftMoney(1), new LiftMoney(2)),
            };
            var sig = new[] { typeof(SysExpr), typeof(SysExpr), typeof(bool), typeof(MethodInfo) };
            var lsig = new[] { typeof(Expression), typeof(Expression), typeof(bool), typeof(MethodInfo) };
            var checkedCount = 0;
            foreach (var (t, v1, v2) in cases)
                foreach (var name in names)
                    foreach (var lift in new[] { false, true })
                        foreach (var nullSide in new[] { 0, 1, 2 }) // none, left, right
                        {
                            var what = $"{t.Name} {name} lift={lift} nullSide={nullSide}";
                            Delegate sysD;
                            try
                            {
                                var sa = SysExpr.Parameter(t, "a"); var sb = SysExpr.Parameter(t, "b");
                                var sl = nullSide == 1 ? SysExpr.Constant(null, t) : (SysExpr)sa;
                                var sr = nullSide == 2 ? SysExpr.Constant(null, t) : (SysExpr)sb;
                                var se = (SysExpr)typeof(SysExpr).GetMethod(name, sig).Invoke(null, new object[] { sl, sr, lift, null });
                                sysD = SysExpr.Lambda(typeof(Func<,,>).MakeGenericType(t, t, se.Type), se, sa, sb).Compile();
                            }
                            catch (System.Reflection.TargetInvocationException) { continue; } // the operator is not defined for the type

                            var la = Parameter(t, "a"); var lb = Parameter(t, "b");
                            var ll = nullSide == 1 ? Constant(null, t) : (Expression)la;
                            var lr = nullSide == 2 ? Constant(null, t) : (Expression)lb;
                            var le = (Expression)typeof(Expression).GetMethod(name, lsig).Invoke(null, new object[] { ll, lr, lift, null });
                            var fastD = Lambda(typeof(Func<,,>).MakeGenericType(t, t, le.Type), le, la, lb).CompileFast(true);
                            Asserts.IsNotNull(fastD, what);
                            ++checkedCount;

                            foreach (var x in new[] { null, v1, v2 })
                                foreach (var y in new[] { null, v1, v2 })
                                    Asserts.AreEqual(sysD.DynamicInvoke(x, y), fastD.DynamicInvoke(x, y), $"{what} ({x}, {y})");
                        }
            Asserts.IsTrue(checkedCount > 100);

            // both operands are constants, as in the ported dotnet/runtime tests
            foreach (var (t, v1, v2) in cases)
                foreach (var name in names)
                    foreach (var lift in new[] { false, true })
                        foreach (var x in new[] { null, v1, v2 })
                            foreach (var y in new[] { null, v1, v2 })
                            {
                                object expected;
                                try
                                {
                                    var se = (SysExpr)typeof(SysExpr).GetMethod(name, sig).Invoke(null, new object[] { SysExpr.Constant(x, t), SysExpr.Constant(y, t), lift, null });
                                    expected = SysExpr.Lambda(typeof(Func<>).MakeGenericType(se.Type), se).Compile().DynamicInvoke();
                                }
                                catch (System.Reflection.TargetInvocationException) { continue; }

                                var le = (Expression)typeof(Expression).GetMethod(name, lsig).Invoke(null, new object[] { Constant(x, t), Constant(y, t), lift, null });
                                var fastD = Lambda(typeof(Func<>).MakeGenericType(le.Type), le).CompileFast(true);
                                var what = $"{t.Name} {name} lift={lift} ({x}, {y})";
                                Asserts.IsNotNull(fastD, what);
                                Asserts.AreEqual(expected, fastD.DynamicInvoke(), what);
                            }

            // unsigned operands above the signed range
            var ua = Parameter(typeof(uint?), "ua"); var ub = Parameter(typeof(uint?), "ub");
            var ult = Lambda<Func<uint?, uint?, bool>>(LessThan(ua, ub), ua, ub).CompileFast(true);
            Asserts.AreEqual(true, ult(1u, uint.MaxValue));
            Asserts.AreEqual(false, ult(uint.MaxValue, 1u));
            var ugt = Lambda<Func<uint?, uint?, bool?>>(GreaterThan(ua, ub, true, null), ua, ub).CompileFast(true);
            Asserts.AreEqual(true, ugt(uint.MaxValue, 1u));
            Asserts.AreEqual(null, ugt(null, 1u));
            var pu = Parameter(typeof(ulong), "pu"); var pv = Parameter(typeof(ulong), "pv");
            Asserts.AreEqual(true, Lambda<Func<ulong, ulong, bool>>(GreaterThan(pu, pv), pu, pv).CompileFast(true)(ulong.MaxValue, 1UL));
            // the other operand is still evaluated when the result is known to be null
            var calls = 0;
            Func<int?> sideEffect = () => { ++calls; return 1; };
            var lt = LessThan(Invoke(Constant(sideEffect)), Constant(null, typeof(int?)), true, null);
            Asserts.AreEqual(typeof(bool?), lt.Type);
            Asserts.AreEqual(null, Lambda<Func<bool?>>(lt).CompileFast(true)());
            var gt = GreaterThan(Constant(null, typeof(int?)), Invoke(Constant(sideEffect)), true, null);
            Asserts.AreEqual(null, Lambda<Func<bool?>>(gt).CompileFast(true)());
            Asserts.AreEqual(2, calls);

            // and the result is usable as a nested operand
            var nb = Parameter(typeof(bool?), "nb");
            var withOr = Lambda<Func<bool?, bool?>>(Or(nb, lt), nb).CompileFast(true);
            Asserts.AreEqual(true, withOr(true));
            Asserts.AreEqual(null, withOr(false));
        }
        public struct ArMoney
        {
            public int V;
            public ArMoney(int v) { V = v; }
            public static ArMoney operator +(ArMoney a, ArMoney b) => new ArMoney(a.V + b.V);
            public static ArMoney operator -(ArMoney a, ArMoney b) => new ArMoney(a.V - b.V);
            public static ArMoney operator *(ArMoney a, ArMoney b) => new ArMoney(a.V * b.V);
            public static ArMoney operator /(ArMoney a, ArMoney b) => new ArMoney(a.V / b.V);
            public static ArMoney operator %(ArMoney a, ArMoney b) => new ArMoney(a.V % b.V);
            public static ArMoney operator &(ArMoney a, ArMoney b) => new ArMoney(a.V & b.V);
            public static ArMoney operator |(ArMoney a, ArMoney b) => new ArMoney(a.V | b.V);
            public static ArMoney operator ^(ArMoney a, ArMoney b) => new ArMoney(a.V ^ b.V);
            public static ArMoney operator ++(ArMoney a) => new ArMoney(a.V + 1);
            public static ArMoney operator --(ArMoney a) => new ArMoney(a.V - 1);
            public override bool Equals(object o) => o is ArMoney m && m.V == V;
            public override int GetHashCode() => V;
            public override string ToString() => "M" + V;
        }
        public static int ArCustomInt(int a, int b) => a * 100 + b;
        public static decimal ArCustomDec(decimal a, decimal b) => a * 100 + b;
        public static ArMoney ArCustomMoney(ArMoney a, ArMoney b) => new ArMoney(a.V * 100 + b.V);

        public void Binary_operators_with_methods_and_nullable_operands_are_the_same_as_system()
        {
            var names = new[] { "Add", "Subtract", "Multiply", "Divide", "Modulo", "And", "Or", "ExclusiveOr", "AddChecked", "SubtractChecked", "MultiplyChecked" };
            var cases = new (Type, object, object, MethodInfo)[]
            {
                (typeof(int?), 1, 2, null), (typeof(int?), 1, 2, typeof(LightExpressionTests).GetMethod("ArCustomInt")),
                (typeof(long?), 1L, 2L, null), (typeof(byte?), (byte)1, (byte)2, null), (typeof(double?), 1.0, 2.0, null), (typeof(uint?), 1u, 2u, null),
                (typeof(decimal?), 1m, 2m, null), (typeof(decimal?), 1m, 2m, typeof(LightExpressionTests).GetMethod("ArCustomDec")),
                (typeof(decimal), 1m, 2m, null), (typeof(DateTime?), new DateTime(2000, 1, 1), new DateTime(2001, 1, 1), null),
                (typeof(ArMoney?), new ArMoney(1), new ArMoney(2), null), (typeof(ArMoney?), new ArMoney(1), new ArMoney(2), typeof(LightExpressionTests).GetMethod("ArCustomMoney")),
                (typeof(ArMoney), new ArMoney(1), new ArMoney(2), null), (typeof(ArMoney), new ArMoney(1), new ArMoney(2), typeof(LightExpressionTests).GetMethod("ArCustomMoney")),
                (typeof(int), 1, 2, typeof(LightExpressionTests).GetMethod("ArCustomInt")),
            };
            var failures = new System.Collections.Generic.List<string>();
            foreach (var (t, v1, v2, m) in cases)
                foreach (var name in names)
                {
                    var what = $"{t.Name} {name} method={m?.Name}";
                    Delegate sysD, fastD; Type rt;
                    try
                    {
                        var sa = SysExpr.Parameter(t, "a"); var sb = SysExpr.Parameter(t, "b");
                        var se = (SysExpr)typeof(SysExpr).GetMethod(name, new[] { typeof(SysExpr), typeof(SysExpr), typeof(MethodInfo) }).Invoke(null, new object[] { sa, sb, m });
                        rt = se.Type;
                        sysD = SysExpr.Lambda(typeof(Func<,,>).MakeGenericType(t, t, rt), se, sa, sb).Compile();
                    }
                    catch (Exception) { continue; }
                    try
                    {
                        var la = Parameter(t, "a"); var lb = Parameter(t, "b");
                        var le = (Expression)typeof(Expression).GetMethod(name, new[] { typeof(Expression), typeof(Expression), typeof(MethodInfo) }).Invoke(null, new object[] { la, lb, m });
                        if (le.Type != rt) { failures.Add($"type: {what} sys={rt.Name} light={le.Type.Name}"); continue; }
                        fastD = Lambda(typeof(Func<,,>).MakeGenericType(t, t, rt), le, la, lb).CompileFast(true);
                        if (fastD == null) { failures.Add("not compiled: " + what); continue; }
                    }
                    catch (Exception ex) { failures.Add("exception: " + what + " " + (ex.InnerException ?? ex).Message.Split('\n')[0]); continue; }
                    var xs = t.IsValueType && Nullable.GetUnderlyingType(t) == null ? new[] { v1, v2 } : new[] { null, v1, v2 };
                    foreach (var x in xs)
                        foreach (var y in xs)
                        {
                            object e, a;
                            try { e = sysD.DynamicInvoke(x, y); } catch (Exception ex) { e = "ex:" + ex.InnerException?.GetType().Name; }
                            try { a = fastD.DynamicInvoke(x, y); } catch (Exception ex) { a = "ex:" + ex.InnerException?.GetType().Name; }
                            if (!Equals(e, a)) { failures.Add($"value: {what} ({x},{y}) sys={e} fec={a}"); break; }
                        }
                }
            Asserts.AreEqual(0, failures.Count, string.Join("; ", failures));
        }

        public struct UnMoney
        {
            public int V;
            public UnMoney(int v) { V = v; }
            public static UnMoney operator -(UnMoney a) => new UnMoney(-a.V);
            public static UnMoney operator +(UnMoney a) => new UnMoney(a.V + 1000);
            public static UnMoney operator ~(UnMoney a) => new UnMoney(~a.V);
            public static UnMoney operator !(UnMoney a) => new UnMoney(a.V + 5000);
            public static UnMoney operator ++(UnMoney a) => new UnMoney(a.V + 1);
            public static UnMoney operator --(UnMoney a) => new UnMoney(a.V - 1);
            public static bool operator true(UnMoney a) => a.V != 0;
            public static bool operator false(UnMoney a) => a.V == 0;
            public override bool Equals(object o) => o is UnMoney m && m.V == V;
            public override int GetHashCode() => V;
            public override string ToString() => "U" + V;
        }

        public void Unary_operators_with_nullable_and_user_defined_operands_are_the_same_as_system()
        {
            var names = new[] { "Negate", "NegateChecked", "UnaryPlus", "OnesComplement", "Increment", "Decrement", "Not", "IsTrue", "IsFalse" };
            var cases = new (Type, object[])[]
            {
                (typeof(int), new object[] { 0, 1, int.MinValue, int.MaxValue }),
                (typeof(int?), new object[] { null, 0, 1, int.MinValue, int.MaxValue }),
                (typeof(long?), new object[] { null, 0L, 1L, long.MinValue, long.MaxValue }),
                (typeof(short?), new object[] { null, (short)0, (short)1, short.MinValue, short.MaxValue }),
                (typeof(ushort?), new object[] { null, (ushort)0, (ushort)1, ushort.MaxValue }),
                (typeof(byte?), new object[] { null, (byte)0, (byte)1, byte.MaxValue }),
                (typeof(sbyte?), new object[] { null, (sbyte)0, (sbyte)1, sbyte.MinValue, sbyte.MaxValue }),
                (typeof(uint?), new object[] { null, 0u, 1u, uint.MaxValue }),
                (typeof(ulong?), new object[] { null, 0UL, 1UL, ulong.MaxValue }),
                (typeof(double?), new object[] { null, 0.0, 1.5, double.MaxValue }),
                (typeof(float?), new object[] { null, 0f, 1.5f, float.MaxValue }),
                (typeof(decimal), new object[] { 0m, 1m, decimal.MinValue }),
                (typeof(decimal?), new object[] { null, 0m, 1m, decimal.MinValue }),
                (typeof(bool), new object[] { true, false }),
                (typeof(bool?), new object[] { null, true, false }),
                (typeof(UnMoney), new object[] { new UnMoney(0), new UnMoney(3) }),
                (typeof(UnMoney?), new object[] { null, new UnMoney(0), new UnMoney(3) }),
                (typeof(LiftEnum), new object[] { LiftEnum.A, LiftEnum.C }),
                (typeof(LiftEnum?), new object[] { null, LiftEnum.A, LiftEnum.C }),
            };
            var failures = new System.Collections.Generic.List<string>();
            foreach (var (t, xs) in cases)
                foreach (var name in names)
                {
                    var what = $"{t.Name} {name}";
                    Delegate sysD, fastD; Type rt;
                    try
                    {
                        var sa = SysExpr.Parameter(t, "a");
                        var se = (SysExpr)typeof(SysExpr).GetMethod(name, new[] { typeof(SysExpr) }).Invoke(null, new object[] { sa });
                        rt = se.Type;
                        sysD = SysExpr.Lambda(typeof(Func<,>).MakeGenericType(t, rt), se, sa).Compile();
                    }
                    catch (Exception) { continue; }
                    try
                    {
                        var la = Parameter(t, "a");
                        var le = (Expression)typeof(Expression).GetMethod(name, new[] { typeof(Expression) }).Invoke(null, new object[] { la });
                        if (le.Type != rt) { failures.Add($"type: {what} sys={rt.Name} light={le.Type.Name}"); continue; }
                        fastD = Lambda(typeof(Func<,>).MakeGenericType(t, rt), le, la).CompileFast(true);
                        if (fastD == null) { failures.Add("not compiled: " + what); continue; }
                    }
                    catch (Exception ex) { failures.Add("exception: " + what + " " + (ex.InnerException ?? ex).Message.Split('\n')[0]); continue; }

                    // the same operation with the ignored result
                    try
                    {
                        var ia = Parameter(t, "a");
                        var ie = (Expression)typeof(Expression).GetMethod(name, new[] { typeof(Expression) }).Invoke(null, new object[] { ia });
                        var ignoredD = Lambda(typeof(Func<,>).MakeGenericType(t, typeof(int)), Block(ie, Constant(1)), ia).CompileFast(true);
                        if (ignoredD == null) failures.Add("ignored not compiled: " + what);
                        else
                            foreach (var x in xs)
                                try { ignoredD.DynamicInvoke(x); } catch (Exception ex) when (ex.InnerException is OverflowException) { }
                    }
                    catch (Exception ex) { failures.Add("ignored exception: " + what + " " + (ex.InnerException ?? ex).Message.Split('\n')[0]); }

                    foreach (var x in xs)
                    {
                        object e, a;
                        try { e = sysD.DynamicInvoke(x); } catch (Exception ex) { e = "ex:" + ex.InnerException?.GetType().Name; }
                        try { a = fastD.DynamicInvoke(x); } catch (Exception ex) { a = "ex:" + ex.InnerException?.GetType().Name; }
                        if (!Equals(e, a)) { failures.Add($"value: {what} ({x}) sys={e} fec={a}"); break; }
                    }
                }
            Asserts.AreEqual(0, failures.Count, string.Join("\n", failures));
        }
        public static class UnMethods
        {
            public static int Custom(int x) => x * 7 + 1;
            public static int? CustomNullable(int? x) => x == null ? -1 : x * 7 + 1;
            public static long Widen(int x) => (long)x * 1000;
            public static string Show(int x) => "S" + x;
            public static bool Positive(int x) => x > 0;
        }

        public void Unary_operators_with_explicit_method_are_the_same_as_system()
        {
            var names = new[] { "Negate", "NegateChecked", "UnaryPlus", "OnesComplement", "Increment", "Decrement", "Not", "IsTrue", "IsFalse" };
            var opNames = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Negate", "op_UnaryNegation" }, { "NegateChecked", "op_UnaryNegation" }, { "UnaryPlus", "op_UnaryPlus" },
                { "OnesComplement", "op_OnesComplement" }, { "Increment", "op_Increment" }, { "Decrement", "op_Decrement" },
                { "Not", "op_LogicalNot" }, { "IsTrue", "op_True" }, { "IsFalse", "op_False" },
            };
            var ints = new object[] { 0, 3, -5 };
            var nullableInts = new object[] { null, 0, 3, -5 };
            var money = new object[] { new UnMoney(0), new UnMoney(3) };
            var nullableMoney = new object[] { null, new UnMoney(0), new UnMoney(3) };
            var decimals = new object[] { 0m, 1.5m };
            var nullableDecimals = new object[] { null, 0m, 1.5m };

            var cases = new System.Collections.Generic.List<(Type, MethodInfo, object[])>
            {
                (typeof(int), typeof(UnMethods).GetMethod(nameof(UnMethods.Custom)), ints),
                (typeof(int?), typeof(UnMethods).GetMethod(nameof(UnMethods.Custom)), nullableInts), // lifted
                (typeof(int?), typeof(UnMethods).GetMethod(nameof(UnMethods.CustomNullable)), nullableInts), // takes the nullable
                (typeof(int), typeof(UnMethods).GetMethod(nameof(UnMethods.Widen)), ints),
                (typeof(int?), typeof(UnMethods).GetMethod(nameof(UnMethods.Widen)), nullableInts), // the lifted type differs from the operand
                (typeof(int), typeof(UnMethods).GetMethod(nameof(UnMethods.Show)), ints), // reference type result
                (typeof(int?), typeof(UnMethods).GetMethod(nameof(UnMethods.Positive)), nullableInts), // lifted bool
                (typeof(decimal), typeof(decimal).GetMethod(nameof(decimal.Negate)), decimals),
                (typeof(decimal?), typeof(decimal).GetMethod(nameof(decimal.Negate)), nullableDecimals),
            };
            foreach (var name in names)
            {
                cases.Add((typeof(UnMoney), typeof(UnMoney).GetMethod(opNames[name]), money));
                cases.Add((typeof(UnMoney?), typeof(UnMoney).GetMethod(opNames[name]), nullableMoney));
            }

            var failures = new System.Collections.Generic.List<string>();
            var compared = 0;
            foreach (var (t, method, xs) in cases)
                foreach (var name in names)
                {
                    var what = $"{t.Name} {name} {method.Name}";
                    Delegate sysD, fastD, lightSysD; Type rt;
                    try
                    {
                        var sa = SysExpr.Parameter(t, "a");
                        var se = (SysExpr)typeof(SysExpr).GetMethod(name, new[] { typeof(SysExpr), typeof(MethodInfo) }).Invoke(null, new object[] { sa, method });
                        rt = se.Type;
                        sysD = SysExpr.Lambda(typeof(Func<,>).MakeGenericType(t, rt), se, sa).Compile();
                    }
                    catch (Exception) { continue; }

                    try
                    {
                        var la = Parameter(t, "a");
                        var le = (Expression)typeof(Expression).GetMethod(name, new[] { typeof(Expression), typeof(MethodInfo) }).Invoke(null, new object[] { la, method });
                        Asserts.AreSame(method, ((UnaryExpression)le).Method);
                        if (le.Type != rt) { failures.Add($"type: {what} sys={rt.Name} light={le.Type.Name}"); continue; }
                        var lambda = Lambda(typeof(Func<,>).MakeGenericType(t, rt), le, la);
                        fastD = lambda.CompileFast(true);
                        lightSysD = lambda.CompileSys();
                        if (fastD == null) { failures.Add("not compiled: " + what); continue; }

                        // the same operation with the ignored result
                        var ia = Parameter(t, "a");
                        var ie = (Expression)typeof(Expression).GetMethod(name, new[] { typeof(Expression), typeof(MethodInfo) }).Invoke(null, new object[] { ia, method });
                        var ignoredD = Lambda(typeof(Func<,>).MakeGenericType(t, typeof(int)), Block(ie, Constant(1)), ia).CompileFast(true);
                        if (ignoredD == null) { failures.Add("ignored not compiled: " + what); continue; }
                        foreach (var x in xs)
                            Asserts.AreEqual(1, ignoredD.DynamicInvoke(x));
                    }
                    catch (Exception ex) { failures.Add("exception: " + what + " " + (ex.InnerException ?? ex).Message.Split('\n')[0]); continue; }

                    compared++;
                    foreach (var x in xs)
                    {
                        object e, a, b;
                        try { e = sysD.DynamicInvoke(x); } catch (Exception ex) { e = "ex:" + ex.InnerException?.GetType().Name; }
                        try { a = fastD.DynamicInvoke(x); } catch (Exception ex) { a = "ex:" + ex.InnerException?.GetType().Name; }
                        try { b = lightSysD.DynamicInvoke(x); } catch (Exception ex) { b = "ex:" + ex.InnerException?.GetType().Name; }
                        if (!Equals(e, a) || !Equals(e, b)) { failures.Add($"value: {what} ({x}) sys={e} fec={a} light-to-sys={b}"); break; }
                    }
                }
            Asserts.AreEqual(0, failures.Count, string.Join("\n", failures));
            Asserts.IsTrue(compared > 30, "compared " + compared);
        }
        public class CaHolder
        {
            public ArMoney F = new ArMoney(10);
            public ArMoney P { get; set; } = new ArMoney(20);
            public ArMoney? N;
            public ArMoney G;
            public ArMoney[] Arr = { new ArMoney(1), new ArMoney(2), new ArMoney(3) };
            public int Calls;
            public CaHolder(bool hasN) { if (hasN) N = new ArMoney(30); }
            public int NextIndex() { Calls++; return 1; }
            public CaHolder Self() { Calls++; return this; }
            public ArMoney this[int i] { get => Arr[i]; set => Arr[i] = value; }
            public string State() => $"{F} {P} {N} {G} {Arr[0]},{Arr[1]},{Arr[2]} calls={Calls}";
        }

        public void Compound_assign_and_inc_dec_with_methods_are_the_same_as_system()
        {
            var money = typeof(ArMoney);
            var ops = new (string, Func<Expression, Expression>)[]
            {
                ("PreInc", t => PreIncrementAssign(t, money.GetMethod("op_Increment"))),
                ("PostInc", t => PostIncrementAssign(t, money.GetMethod("op_Increment"))),
                ("PreDec", t => PreDecrementAssign(t, money.GetMethod("op_Decrement"))),
                ("PostDec", t => PostDecrementAssign(t, money.GetMethod("op_Decrement"))),
                ("AddAssign", t => MakeBinary(ExpressionType.AddAssign, t, Constant(new ArMoney(5), t.Type), false, money.GetMethod("op_Addition"))),
                ("SubtractAssign", t => MakeBinary(ExpressionType.SubtractAssign, t, Constant(new ArMoney(5), t.Type), false, money.GetMethod("op_Subtraction"))),
                ("OrAssign", t => MakeBinary(ExpressionType.OrAssign, t, Constant(new ArMoney(5), t.Type), false, money.GetMethod("op_BitwiseOr"))),
                ("CustomMultiplyAssign", t => MakeBinary(ExpressionType.MultiplyAssign, t, Constant(new ArMoney(5), t.Type), false, typeof(LightExpressionTests).GetMethod(nameof(ArCustomMoney)))),
            };

            var indexer = typeof(CaHolder).GetProperty("Item");
            var targets = new (string, Func<ParameterExpression, Expression>)[]
            {
                ("field", h => Field(h, "F")),
                ("property", h => Property(h, "P")),
                ("nullable field", h => Field(h, "N")),
                ("array element", h => ArrayAccess(Field(h, "Arr"), Constant(1))),
                ("array element with the side effect index", h => ArrayAccess(Field(h, "Arr"), Call(h, typeof(CaHolder).GetMethod("NextIndex")))),
                ("indexer", h => Property(h, indexer, Constant(2))),
                ("field of the side effect instance", h => Field(Call(h, typeof(CaHolder).GetMethod("Self")), "F")),
                ("property of the side effect instance", h => Property(Call(h, typeof(CaHolder).GetMethod("Self")), "P")),
            };

            var failures = new System.Collections.Generic.List<string>();
            var compared = 0;
            foreach (var (opName, op) in ops)
                foreach (var (targetName, target) in targets)
                    for (var variant = 0; variant < 3; ++variant) // 0 - the used result, 1 - the ignored result, 2 - the local variable
                    {
                        var what = $"{opName} {targetName} variant {variant}";
                        Func<CaHolder, object> sysF, fastF;
                        try
                        {
                            var h = Parameter(typeof(CaHolder), "h");
                            Expression body;
                            if (variant == 2)
                            {
                                if (targetName != "field")
                                    continue;
                                var v = Variable(typeof(ArMoney), "v");
                                var r = Variable(typeof(object), "r");
                                body = Block(new[] { v, r },
                                    Assign(v, Field(h, "F")),
                                    Assign(r, Convert(op(v), typeof(object))),
                                    Assign(Field(h, "G"), v),
                                    r);
                            }
                            else
                            {
                                var e = op(target(h));
                                body = variant == 0 ? (Expression)Convert(e, typeof(object)) : Block(e, Constant(1, typeof(object)));
                            }
                            var lambda = Lambda<Func<CaHolder, object>>(body, h);
                            try { sysF = lambda.CompileSys(); }
                            catch (Exception) { continue; } // not valid for System, so nothing to compare to
                            fastF = lambda.CompileFast(true);
                        }
                        catch (Exception ex) { failures.Add("exception: " + what + " " + (ex.InnerException ?? ex).Message.Split('\n')[0]); continue; }

                        compared++;
                        foreach (var hasN in new[] { true, false })
                        {
                            var sysH = new CaHolder(hasN);
                            var fastH = new CaHolder(hasN);
                            object e1, e2;
                            try { e1 = sysF(sysH); } catch (Exception ex) { e1 = "ex:" + ex.GetType().Name; }
                            try { e2 = fastF(fastH); } catch (Exception ex) { e2 = "ex:" + ex.GetType().Name; }
                            if (!Equals(e1, e2) || sysH.State() != fastH.State())
                                failures.Add($"{what} hasN={hasN}: sys={e1} {sysH.State()} fec={e2} {fastH.State()}");
                        }
                    }
            Asserts.AreEqual(0, failures.Count, string.Join("\n", failures));
            Asserts.IsTrue(compared > 100, "compared " + compared);
        }
        public enum ConvLongEnum : long { A = 1, B = long.MaxValue }
        public enum ConvUIntEnum : uint { A = 1, B = uint.MaxValue }

        public void Convert_and_ConvertChecked_between_primitives_nullables_and_enums_are_the_same_as_system()
        {
            var types = new[]
            {
                typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(char), typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(ConvLongEnum), typeof(ConvUIntEnum)
            };
            var values = new object[] { 0, 1, 127, 128, 255, 256, -1, -128, 32767, 32768, 65535, 65536, int.MaxValue, int.MinValue, 2147483648L, 4294967295L, 4294967296L, long.MaxValue, long.MinValue, 9223372036854775808UL, ulong.MaxValue, 1.5, -1.5, 3e10 };

            var failures = new System.Collections.Generic.List<string>();
            var compared = 0;
            foreach (var checkedConv in new[] { false, true })
                foreach (var from in types)
                    foreach (var fromNullable in new[] { false, true })
                        foreach (var to in types)
                            foreach (var toNullable in new[] { false, true })
                            {
                                var srcType = fromNullable ? typeof(Nullable<>).MakeGenericType(from) : from;
                                var dstType = toNullable ? typeof(Nullable<>).MakeGenericType(to) : to;
                                if (srcType == dstType)
                                    continue;

                                Delegate sysD, fastD;
                                try
                                {
                                    var sp = SysExpr.Parameter(srcType, "a");
                                    var se = checkedConv ? SysExpr.ConvertChecked(sp, dstType) : SysExpr.Convert(sp, dstType);
                                    sysD = SysExpr.Lambda(typeof(Func<,>).MakeGenericType(srcType, dstType), se, sp).Compile();
                                    var lp = Parameter(srcType, "a");
                                    var le = checkedConv ? ConvertChecked(lp, dstType) : Convert(lp, dstType);
                                    fastD = Lambda(typeof(Func<,>).MakeGenericType(srcType, dstType), le, lp).CompileFast(true);
                                    if (fastD == null) { failures.Add($"not compiled {(checkedConv ? "checked " : "")}{srcType.Name} -> {dstType.Name}"); continue; }
                                }
                                catch (Exception) { continue; }

                                var inputs = new System.Collections.Generic.List<object>();
                                foreach (var v in values)
                                {
                                    try
                                    {
                                        // go through the unchecked System conversion to get the value of the source type in a normal shape
                                        var cp = SysExpr.Parameter(typeof(object), "o");
                                        var cv = SysExpr.Lambda<Func<object, object>>(SysExpr.Convert(SysExpr.Convert(SysExpr.Convert(cp, v.GetType()), from), typeof(object)), cp).Compile()(v);
                                        inputs.Add(cv);
                                    }
                                    catch (Exception) { }
                                }
                                if (fromNullable) inputs.Add(null);

                                compared++;
                                foreach (var x in inputs)
                                {
                                    object e, a;
                                    try { e = sysD.DynamicInvoke(x); } catch (Exception ex) { e = "ex:" + ex.InnerException?.GetType().Name; }
                                    try { a = fastD.DynamicInvoke(x); } catch (Exception ex) { a = "ex:" + ex.InnerException?.GetType().Name; }
                                    if (!Equals(e, a))
                                    {
                                        failures.Add($"{(checkedConv ? "checked " : "")}{srcType.Name} -> {dstType.Name} ({x}) sys={e} fec={a}");
                                        break;
                                    }
                                }
                            }
            Asserts.AreEqual(0, failures.Count, failures.Count + " failures, e.g.\n" + string.Join("\n", failures.Take(15)));
            Asserts.IsTrue(compared > 500, "compared " + compared);
        }
#if NET7_0_OR_GREATER
        public void Half_Int128_and_UInt128_constants_and_conversions_are_the_same_as_system()
        {
            var h = Parameter(typeof(Half), "h");
            var hc = Lambda<Func<Half, double>>(Convert(Add(h, Constant((Half)2.5)), typeof(double)), h);
            Asserts.AreEqual(4.0, hc.CompileSys()((Half)1.5));
            Asserts.AreEqual(4.0, hc.CompileFast(true)((Half)1.5));

            var i = Parameter(typeof(Int128), "i");
            var big = (Int128)long.MaxValue * 1000;
            var ic = Lambda<Func<Int128, Int128>>(Add(i, Constant(big)), i);
            Asserts.AreEqual(big + 7, ic.CompileSys()(7));
            Asserts.AreEqual(big + 7, ic.CompileFast(true)(7));

            var u = Parameter(typeof(long), "u");
            var uc = Lambda<Func<long, UInt128>>(Add(Convert(u, typeof(UInt128)), Constant((UInt128)ulong.MaxValue)), u);
            Asserts.AreEqual((UInt128)ulong.MaxValue + 5, uc.CompileSys()(5));
            Asserts.AreEqual((UInt128)ulong.MaxValue + 5, uc.CompileFast(true)(5));
        }
#endif
        public delegate T RefConvFunc<T, R>(ref R r);
        public class RefConv
        {
            public int Value;
            public static RefConv FromRef(ref int x) => new RefConv { Value = x++ };
        }

        public void Convert_with_the_method_taking_ref_param_is_the_same_as_system()
        {
            var method = typeof(RefConv).GetMethod(nameof(RefConv.FromRef));

            var p = Parameter(typeof(int).MakeByRefType(), "p");
            var e = Lambda<RefConvFunc<RefConv, int>>(Convert(p, typeof(RefConv), method), p);
            int x = 5;
            Asserts.AreEqual(5, e.CompileFast(true)(ref x).Value);
            Asserts.AreEqual(6, x);

            var np = Parameter(typeof(int?).MakeByRefType(), "p");
            var ne = Lambda<RefConvFunc<RefConv, int?>>(Convert(np, typeof(RefConv), method), np);
            int? nx = 5;
            Asserts.AreEqual(5, ne.CompileFast(true)(ref nx).Value);
            Asserts.AreEqual(5, nx); // the refness is lost on lifting, same as in System
        }

        public void Negate_checked_of_short_emits_the_short_IL()
        {
            var s = Parameter(typeof(short), "s"); var ns = Parameter(typeof(short?), "s");
            Lambda<Func<short, short>>(NegateChecked(s), s).CompileFast(true).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Neg, OpCodes.Conv_Ovf_I2, OpCodes.Ret);
            Lambda<Func<short?, short?>>(NegateChecked(ns), ns).CompileFast(true).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Stloc_0, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Brfalse,
                OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Neg, OpCodes.Conv_Ovf_I2, OpCodes.Newobj, OpCodes.Br,
                OpCodes.Ldloca_S, OpCodes.Initobj, OpCodes.Ldloc_1, OpCodes.Ret);
        }
        public void Lifted_comparisons_have_the_same_type_and_result_as_system()
        {
            var ip = Parameter(typeof(int), "a"); var iq = Parameter(typeof(int), "b");
            var np = Parameter(typeof(int?), "a"); var nq = Parameter(typeof(int?), "b");

            var eqInt = Equal(ip, iq, true, null);
            Asserts.AreEqual(typeof(bool), eqInt.Type);
            Asserts.AreEqual(true, Lambda<Func<int, int, bool>>(eqInt, ip, iq).CompileFast(true)(1, 1));
            Asserts.AreEqual(true, Lambda<Func<int, int, bool>>(LessThan(ip, iq, true, null), ip, iq).CompileFast(true)(1, 2));

            // lifted to null: the result is null when any operand is null, so null == null is null and not true
            var eqNull = Equal(np, nq, true, null);
            Asserts.AreEqual(typeof(bool?), eqNull.Type);
            var liftedOps = new Func<Expression, Expression, bool, MethodInfo, BinaryExpression>[]
                { Equal, NotEqual, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual };
            var sysLiftedOps = new Func<SysExpr, SysExpr, bool, MethodInfo, System.Linq.Expressions.BinaryExpression>[]
                { SysExpr.Equal, SysExpr.NotEqual, SysExpr.GreaterThan, SysExpr.GreaterThanOrEqual, SysExpr.LessThan, SysExpr.LessThanOrEqual };
            var values = new int?[] { null, 0, 1, 2, -1 };
            for (var i = 0; i < liftedOps.Length; ++i)
            {
                var f = Lambda<Func<int?, int?, bool?>>(liftedOps[i](np, nq, true, null), np, nq);
                var sa = SysExpr.Parameter(typeof(int?)); var sb = SysExpr.Parameter(typeof(int?));
                var fs = SysExpr.Lambda<Func<int?, int?, bool?>>(sysLiftedOps[i](sa, sb, true, null), sa, sb).Compile();
                var ff = f.CompileFast(true);
                Asserts.IsNotNull(ff);
                foreach (var x in values)
                    foreach (var y in values)
                        Asserts.AreEqual(fs(x, y), ff(x, y));

                // the right operand is a complex expression
                var fc = Lambda<Func<int?, int?, bool?>>(liftedOps[i](np, Block(nq), true, null), np, nq).CompileFast(true);
                Asserts.IsNotNull(fc);
                foreach (var x in values)
                    foreach (var y in values)
                        Asserts.AreEqual(fs(x, y), fc(x, y));
            }

            // all comparison factories, with and without lifting, for the nullable and non-nullable operands
            var names = new[] { "Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual" };
            foreach (var name in names)
                foreach (var t in new[] { typeof(int), typeof(int?), typeof(bool), typeof(bool?), typeof(double?) })
                    foreach (var lift in new[] { false, true })
                    {
                        System.Linq.Expressions.BinaryExpression s = null;
                        try
                        {
                            s = (System.Linq.Expressions.BinaryExpression)typeof(SysExpr).GetMethod(name, new[] { typeof(SysExpr), typeof(SysExpr), typeof(bool), typeof(System.Reflection.MethodInfo) })
                                .Invoke(null, new object[] { SysExpr.Parameter(t), SysExpr.Parameter(t), lift, null });
                        }
                        catch (System.Reflection.TargetInvocationException) { }
                        if (s == null)
                            continue;

                        var l = (BinaryExpression)typeof(Expression).GetMethod(name, new[] { typeof(Expression), typeof(Expression), typeof(bool), typeof(System.Reflection.MethodInfo) })
                            .Invoke(null, new object[] { Parameter(t), Parameter(t), lift, null });
                        var what = $"{name}({t.Name}, lift: {lift})";
                        Asserts.AreEqual(s.Type, l.Type, what + " Type");
                        Asserts.AreEqual(s.IsLifted, l.IsLifted, what + " IsLifted");
                        Asserts.AreEqual(s.IsLiftedToNull, l.IsLiftedToNull, what + " IsLiftedToNull");
                    }
        }
        public void IsLifted_and_IsLiftedToNull_are_the_same_as_in_system_expressions()
        {
            var types = new[] { typeof(int), typeof(int?), typeof(long), typeof(bool), typeof(bool?), typeof(double?), typeof(uint?) };
            var names = new[] { "Add", "Subtract", "Multiply", "Divide", "Modulo", "And", "Or", "ExclusiveOr", "LeftShift", "RightShift",
                "AndAlso", "OrElse", "Equal", "NotEqual", "LessThan", "GreaterThanOrEqual", "Coalesce" };
            var checkedCount = 0;
            foreach (var name in names)
                foreach (var lt in types)
                    foreach (var rt in types)
                    {
                        System.Linq.Expressions.BinaryExpression s = null;
                        try
                        {
                            s = (System.Linq.Expressions.BinaryExpression)typeof(SysExpr).GetMethod(name, new[] { typeof(SysExpr), typeof(SysExpr) })
                                .Invoke(null, new object[] { SysExpr.Parameter(lt), SysExpr.Parameter(rt) });
                        }
                        catch (System.Reflection.TargetInvocationException) { }
                        if (s == null)
                            continue;

                        var l = (BinaryExpression)typeof(Expression).GetMethod(name, new[] { typeof(Expression), typeof(Expression) })
                            .Invoke(null, new object[] { Parameter(lt), Parameter(rt) });
                        var what = $"{name}({lt.Name}, {rt.Name})";
                        Asserts.AreEqual(s.Type, l.Type, what + " Type");
                        Asserts.AreEqual(s.IsLifted, l.IsLifted, what + " IsLifted");
                        Asserts.AreEqual(s.IsLiftedToNull, l.IsLiftedToNull, what + " IsLiftedToNull");
                        ++checkedCount;
                    }
            // the number of the combinations accepted by System differs slightly between the runtimes, so this is just a sanity check
            Asserts.IsTrue(checkedCount > 50);
        }

        public void Can_round_trip_more_expression_kinds_through_system_expressions()
        {
            var a = Parameter(typeof(int), "a");
            var o = Parameter(typeof(object), "o");
            var v = Variable(typeof(int), "v");
            var arr = Variable(typeof(int[]), "arr");
            var d = Variable(typeof(double), "d");
            var cases = new List<(string, Expression)>
            {
                ("TypeIs string", Convert(TypeIs(o, typeof(string)), typeof(object))),
                ("TypeEqual string", Convert(TypeEqual(o, typeof(string)), typeof(object))),
                ("TypeIs int on int", Convert(TypeIs(a, typeof(int)), typeof(object))),
                ("TypeIs object on int", Convert(TypeIs(a, typeof(object)), typeof(object))),
                ("TypeEqual int boxed", Convert(TypeEqual(Convert(a, typeof(object)), typeof(int)), typeof(object))),
                ("ConvertChecked", Convert(ConvertChecked(a, typeof(long)), typeof(object))),
                ("TypeAs", TypeAs(o, typeof(string))),
                ("Unbox", Convert(Unbox(o, typeof(int)), typeof(object))),
                ("Negate", Convert(Negate(a), typeof(object))),
                ("NegateChecked", Convert(NegateChecked(a), typeof(object))),
                ("OnesComplement", Convert(OnesComplement(a), typeof(object))),
                ("UnaryPlus", Convert(UnaryPlus(a), typeof(object))),
                ("Increment", Convert(Increment(a), typeof(object))),
                ("Decrement", Convert(Decrement(a), typeof(object))),
                ("IsTrue", Convert(IsTrue(Equal(a, Constant(1))), typeof(object))),
                ("IsFalse", Convert(IsFalse(Equal(a, Constant(1))), typeof(object))),
                ("Not bool", Convert(Not(Equal(a, Constant(1))), typeof(object))),
                ("Not int", Convert(Not(a), typeof(object))),
                ("ExclusiveOr", Convert(ExclusiveOr(a, Constant(3)), typeof(object))),
                ("LeftShift", Convert(LeftShift(a, Constant(2)), typeof(object))),
                ("Power", Convert(Power(Convert(a, typeof(double)), Constant(2.0)), typeof(object))),
                ("AddAssign", Block(new[] { v }, Assign(v, a), AddAssign(v, Constant(2)), Convert(v, typeof(object)))),
                ("SubtractAssign", Block(new[] { v }, Assign(v, a), SubtractAssign(v, Constant(2)), Convert(v, typeof(object)))),
                ("MultiplyAssign", Block(new[] { v }, Assign(v, a), MultiplyAssign(v, Constant(2)), Convert(v, typeof(object)))),
                ("DivideAssign", Block(new[] { v }, Assign(v, a), DivideAssign(v, Constant(2)), Convert(v, typeof(object)))),
                ("ModuloAssign", Block(new[] { v }, Assign(v, a), ModuloAssign(v, Constant(2)), Convert(v, typeof(object)))),
                ("AndAssign", Block(new[] { v }, Assign(v, a), AndAssign(v, Constant(2)), Convert(v, typeof(object)))),
                ("OrAssign", Block(new[] { v }, Assign(v, a), OrAssign(v, Constant(2)), Convert(v, typeof(object)))),
                ("ExclusiveOrAssign", Block(new[] { v }, Assign(v, a), ExclusiveOrAssign(v, Constant(2)), Convert(v, typeof(object)))),
                ("LeftShiftAssign", Block(new[] { v }, Assign(v, a), LeftShiftAssign(v, Constant(2)), Convert(v, typeof(object)))),
                ("RightShiftAssign", Block(new[] { v }, Assign(v, a), RightShiftAssign(v, Constant(1)), Convert(v, typeof(object)))),
                ("PowerAssign", Block(new[] { d }, Assign(d, Convert(a, typeof(double))), PowerAssign(d, Constant(2.0)), Convert(d, typeof(object)))),
                ("AddAssignChecked", Block(new[] { v }, Assign(v, a), AddAssignChecked(v, Constant(2)), Convert(v, typeof(object)))),
                ("PreIncrementAssign", Block(new[] { v }, Assign(v, a), Convert(PreIncrementAssign(v), typeof(object)))),
                ("PostIncrementAssign", Block(new[] { v }, Assign(v, a), Convert(PostIncrementAssign(v), typeof(object)))),
                ("PreDecrementAssign", Block(new[] { v }, Assign(v, a), Convert(PreDecrementAssign(v), typeof(object)))),
                ("PostDecrementAssign", Block(new[] { v }, Assign(v, a), Convert(PostDecrementAssign(v), typeof(object)))),
                ("ArrayIndex", Block(new[] { arr }, Assign(arr, NewArrayInit(typeof(int), a, Constant(5))), Convert(ArrayIndex(arr, Constant(1)), typeof(object)))),
                ("ArrayAccess", Block(new[] { arr }, Assign(arr, NewArrayBounds(typeof(int), Constant(3))), Assign(ArrayAccess(arr, Constant(1)), a), Convert(ArrayAccess(arr, Constant(1)), typeof(object)))),
                ("ArrayLength", Block(new[] { arr }, Assign(arr, NewArrayBounds(typeof(int), a)), Convert(ArrayLength(arr), typeof(object)))),
                ("Switch", Convert(Switch(a, Constant(0), SwitchCase(Constant(10), Constant(1)), SwitchCase(Constant(20), Constant(2))), typeof(object))),
                ("TryCatch", Convert(TryCatch(Constant(1), Catch(typeof(Exception), Constant(2))), typeof(object))),
                ("TryFinally", Convert(TryFinally(Constant(1), Empty()), typeof(object))),
                ("Condition", Convert(Condition(Equal(a, Constant(1)), Constant(10), Constant(20)), typeof(object))),
                ("Coalesce", Coalesce(o, Constant("x"))),
                ("Lift Invoke", Convert(Invoke(Lambda<Func<int, int>>(Add(a, Constant(1)), a), a), typeof(object))),
                ("Default", Convert(Default(typeof(int)), typeof(object))),
                ("New", Convert(New(typeof(DateTime).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) }), Constant(2000), Constant(1), Constant(2)), typeof(object))),
                ("Call", Call(typeof(Math).GetMethod("Abs", new[] { typeof(int) }), a) is var c ? Convert(c, typeof(object)) : null),
            };

            foreach (var (name, body) in cases)
            {
                var lambda = Lambda<Func<int, object, object>>(body, a, o);
                var back = lambda.ToLambdaExpression().ToLightExpression();
                var backSys = back.ToLambdaExpression().Compile();
                var expected = lambda.CompileSys();
                var backFast = (Func<int, object, object>)back.CompileFast(true);
                Asserts.IsNotNull(backFast, name);
                var backCompiledSys = (Func<int, object, object>)back.CompileSys();

                foreach (var (x, y) in new (int, object)[] { (1, "s"), (2, 5), (3, null) })
                {
                    var what = $"{name} ({x}, {y})";
                    var r0 = Run(expected, x, y);
                    Asserts.AreEqual(r0, Run(backFast, x, y), what + " back CompileFast");
                    Asserts.AreEqual(r0, Run(backCompiledSys, x, y), what + " back CompileSys");
                    Asserts.AreEqual(r0, Run(backSys, x, y), what + " back to System");
                }
            }
        }

        // the exception type name stands for the thrown result, so the failing cases are compared too
        private static object Run(Func<int, object, object> f, int x, object y)
        {
            try { return f(x, y); }
            catch (Exception ex) { return ex.GetType().Name; }
        }

        public void Can_compile_type_is_and_type_equal_the_same_as_system()
        {
            var o = Parameter(typeof(object), "o");
            var n = Parameter(typeof(int?), "n");
            var i = Parameter(typeof(int), "i");
            var s = Parameter(typeof(string), "s");
            var baseObj = Parameter(typeof(TypeTestBase), "b");

            var typeOperands = new[] { typeof(int), typeof(int?), typeof(long), typeof(string), typeof(object), typeof(ValueType),
                typeof(TypeTestBase), typeof(TypeTestDerived), typeof(IDisposable), typeof(int[]), typeof(DayOfWeek) };
            var operands = new (ParameterExpression, object[])[]
            {
                (o, new object[] { null, 1, 1L, "s", DayOfWeek.Monday, new TypeTestBase(), new TypeTestDerived(), new int[0] }),
                (n, new object[] { null, 5 }),
                (i, new object[] { 7 }),
                (s, new object[] { null, "s" }),
                (baseObj, new object[] { null, new TypeTestBase(), new TypeTestDerived() }),
            };

            foreach (var isEqual in new[] { false, true })
                foreach (var t in typeOperands)
                    foreach (var (operandParam, values) in operands)
                    {
                        var p = Parameter(operandParam.Type, "p"); // not shared with the outer lambda parameter
                        var body = isEqual ? TypeEqual(p, t) : TypeIs(p, t);
                        Asserts.AreEqual(isEqual ? ExpressionType.TypeEqual : ExpressionType.TypeIs, body.NodeType);
                        Asserts.AreEqual(typeof(bool), body.Type);

                        var lambda = Lambda<Func<object, bool>>(Invoke(Lambda(body, p), Convert(o, p.Type)), o);
                        var what = $"{(isEqual ? "TypeEqual" : "TypeIs")}({p.Type.Name}, {t.Name})";
                        var fast = lambda.CompileFast(true);
                        Asserts.IsNotNull(fast, what);
                        // System on .NET Framework fails to compile `TypeEqual` for a nullable parameter, so the expected value is computed by the definition
                        Func<object, bool> sys;
                        try { sys = lambda.CompileSys(); }
                        catch (InvalidOperationException) when (isEqual && p.Type.IsValueType)
                        {
                            sys = v => v != null && v.GetType() == t.GetNonNullableOrSelf();
                        }
                        var back = lambda.ToLambdaExpression().ToLightExpression();
                        var backFast = (Func<object, bool>)back.CompileFast(true);
                        Asserts.IsNotNull(backFast, what + " after the round trip");

                        foreach (var v in values)
                        {
                            Asserts.AreEqual(sys(v), fast(v), what + " with " + (v ?? "null"));
                            Asserts.AreEqual(sys(v), backFast(v), what + " round trip with " + (v ?? "null"));
                        }
                    }

            // the result is ignored but the operand is still evaluated
            var calls = 0;
            Func<object> side = () => { ++calls; return "x"; };
            var ignored = Lambda<Action>(TypeEqual(Invoke(Constant(side)), typeof(string))).CompileFast(true);
            ignored();
            Asserts.AreEqual(1, calls);
        }

        public class TypeTestBase { }
        public class TypeTestDerived : TypeTestBase { }

        public void Type_is_and_type_equal_emit_the_short_IL()
        {
            var o = Parameter(typeof(object), "o");
            var s = Parameter(typeof(string), "s");
            var i = Parameter(typeof(int), "i");
            var n = Parameter(typeof(int?), "n");
            const CompilerFlags flags = CompilerFlags.EnableDelegateDebugInfo;

            Lambda<Func<object, bool>>(TypeIs(o, typeof(string)), o).CompileFast(true, flags).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Isinst, OpCodes.Ldnull, OpCodes.Cgt_Un, OpCodes.Ret);

            // exact check of the sealed class does not need the GetType call
            Lambda<Func<object, bool>>(TypeEqual(o, typeof(string)), o).CompileFast(true, flags).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Isinst, OpCodes.Ldnull, OpCodes.Cgt_Un, OpCodes.Ret);

            // the value type is known at the compile time
            Lambda<Func<int, bool>>(TypeIs(i, typeof(object)), i).CompileFast(true, flags).AssertOpCodes(OpCodes.Ldc_I4_1, OpCodes.Ret);
            Lambda<Func<int, bool>>(TypeIs(i, typeof(string)), i).CompileFast(true, flags).AssertOpCodes(OpCodes.Ldc_I4_0, OpCodes.Ret);
            Lambda<Func<int, bool>>(TypeEqual(i, typeof(int)), i).CompileFast(true, flags).AssertOpCodes(OpCodes.Ldc_I4_1, OpCodes.Ret);
            Lambda<Func<int, bool>>(TypeEqual(i, typeof(long)), i).CompileFast(true, flags).AssertOpCodes(OpCodes.Ldc_I4_0, OpCodes.Ret);

            // the nullable has a matching type whenever it has a value
            Lambda<Func<int?, bool>>(TypeIs(n, typeof(int)), n).CompileFast(true, flags).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Stloc_0, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Ret);
            Lambda<Func<int?, bool>>(TypeEqual(n, typeof(int)), n).CompileFast(true, flags).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Stloc_0, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Ret);

            // the operand is already of the type, so it is just the null check
            Lambda<Func<string, bool>>(TypeIs(s, typeof(object)), s).CompileFast(true, flags).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Ldnull, OpCodes.Cgt_Un, OpCodes.Ret);
        }

        public void Type_is_and_type_equal_with_the_known_value_type_still_evaluate_the_operand()
        {
            var calls = 0;
            Func<int> side = () => { ++calls; return 42; };
            Func<int?> sideNullable = () => { ++calls; return null; };

            Asserts.AreEqual(true, Lambda<Func<bool>>(TypeIs(Invoke(Constant(side)), typeof(object))).CompileFast(true)());
            Asserts.AreEqual(false, Lambda<Func<bool>>(TypeEqual(Invoke(Constant(side)), typeof(long))).CompileFast(true)());
            Asserts.AreEqual(false, Lambda<Func<bool>>(TypeIs(Invoke(Constant(sideNullable)), typeof(int))).CompileFast(true)());
            Asserts.AreEqual(false, Lambda<Func<bool>>(TypeEqual(Invoke(Constant(sideNullable)), typeof(int))).CompileFast(true)());
            Asserts.AreEqual(4, calls);
        }
        public void Can_compile_right_shift_with_nullable_shift_count()
        {
            var value = Parameter(typeof(int), "value");
            var count = Parameter(typeof(int?), "count");
            var nullableValue = Parameter(typeof(int?), "nullableValue");
            var plainCount = Parameter(typeof(int), "plainCount");

            // the non-nullable left with the nullable count is the `TypedBinaryExpression` path, the result is nullable
            foreach (var nodeType in new[] { ExpressionType.RightShift, ExpressionType.LeftShift })
            {
                var shift = nodeType == ExpressionType.RightShift ? RightShift(value, count) : LeftShift(value, count);
                Asserts.AreEqual(typeof(int?), shift.Type);
                Asserts.AreEqual(typeof(int?), shift.ToExpression().Type);

                var lambda = Lambda<Func<int, int?, int?>>(shift, value, count);
                var fast = lambda.CompileFast(true);
                Asserts.IsNotNull(fast);
                var sys = lambda.CompileSys();
                foreach (var v in new[] { 64, -64, 1 })
                    foreach (int? c in new int?[] { null, 0, 2, 5 })
                        Asserts.AreEqual(sys(v, c), fast(v, c));

                // the nullable left keeps the left typed path
                var nullableLeft = nodeType == ExpressionType.RightShift ? RightShift(nullableValue, count) : LeftShift(nullableValue, count);
                var nullableLambda = Lambda<Func<int?, int?, int?>>(nullableLeft, nullableValue, count);
                var nullableFast = nullableLambda.CompileFast(true);
                Asserts.IsNotNull(nullableFast);
                var nullableSys = nullableLambda.CompileSys();
                foreach (int? v in new int?[] { null, 64, -64 })
                    foreach (int? c in new int?[] { null, 2 })
                        Asserts.AreEqual(nullableSys(v, c), nullableFast(v, c));

                // the plain operands keep the left typed path with the plain result
                var plain = nodeType == ExpressionType.RightShift ? RightShift(value, plainCount) : LeftShift(value, plainCount);
                Asserts.AreEqual(typeof(int), plain.Type);
                var plainLambda = Lambda<Func<int, int, int>>(plain, value, plainCount);
                Asserts.AreEqual(plainLambda.CompileSys()(64, 2), plainLambda.CompileFast(true)(64, 2));
            }

            // the unsigned value is shifted logically
            var uvalue = Parameter(typeof(uint), "uvalue");
            var ushift = RightShift(uvalue, count);
            Asserts.AreEqual(typeof(uint?), ushift.Type);
            var ulambda = Lambda<Func<uint, int?, uint?>>(ushift, uvalue, count);
            var ufast = ulambda.CompileFast(true);
            Asserts.IsNotNull(ufast);
            var usys = ulambda.CompileSys();
            Asserts.AreEqual(usys(0x80000000u, 4), ufast(0x80000000u, 4));
            Asserts.AreEqual(usys(0x80000000u, null), ufast(0x80000000u, null));

            // the shift with a compatible method is built by the method based operator, an incompatible one is rejected
            var shiftMethod = typeof(LightExpressionTests).GetMethod(nameof(AndInts), BindingFlags.Static | BindingFlags.NonPublic);
            var withMethod = RightShift(value, plainCount, shiftMethod);
            Asserts.AreEqual(shiftMethod, withMethod.Method);
            Asserts.AreEqual(typeof(int), withMethod.Type);
            Asserts.Throws<InvalidOperationException>(() => RightShift(Constant("a"), plainCount, shiftMethod));
        }

        public void Can_compile_shifts_with_nullable_shift_count()
        {
            var shift = Parameter(typeof(int?), "shift");

            var leftShift = LeftShift(Constant(16), shift);
            Asserts.AreEqual(typeof(int?), leftShift.Type);
            var lambda = Lambda<Func<int?, int?>>(leftShift, shift);
            var fast = lambda.CompileFast(true);
            Asserts.IsNotNull(fast);
            var sys = lambda.CompileSys();

            Asserts.AreEqual(64, fast(2));
            Asserts.AreEqual(64, sys(2));
            Asserts.AreEqual(null, fast(null));
            Asserts.AreEqual(null, sys(null));
        }

        public void Can_compile_lambda_without_converting_to_expression()
        {
            var funcExpr = Lambda(
                    New(typeof(X).GetTypeInfo().GetConstructors()[0],
                        New(typeof(Y).GetTypeInfo().GetConstructors()[0])));

            var func = funcExpr.CompileFast<Func<X>>(true);
            Asserts.IsNotNull(func);

            var x = func();
            Asserts.IsInstanceOf<X>(x);
        }

        public class Y { }
        public class X
        {
            public Y Y { get; }
            public X(Y y)
            {
                Y = y;
            }
        }


        public void Can_compile_lambda_with_property()
        {
            var thisType = GetType().GetTypeInfo();
            var funcExpr = Lambda(Property(thisType.GetProperty(nameof(PropX))));

            var func = funcExpr.CompileFast<Func<X>>(true);
            Asserts.IsNotNull(func);

            var x = func();
            Asserts.IsInstanceOf<X>(x);
        }


        public void Can_compile_lambda_with_call_and_property()
        {
            var thisType = GetType().GetTypeInfo();
            var funcExpr =
                Lambda(Call(thisType.GetMethod(nameof(GetX)),
                    Property(thisType.GetProperty(nameof(PropX)))));

            var func = funcExpr.CompileFast<Func<X>>(true);
            Asserts.IsNotNull(func);

            var x = func();
            Asserts.IsInstanceOf<X>(x);
        }

        public static X PropX => new X(new Y());
        public static X GetX(X x) => x;


        public void Nested_Func_using_outer_parameter()
        {
            // The same hoisted expression: 
            //Expression<Func<string, string>> expr = a => GetS(() => a);

            var aParam = Parameter(typeof(string), "a");
            var expr = Lambda(
                Call(GetType().GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(GetS)),
                    Lambda(aParam)),
                aParam);

            var f = expr.CompileFast<Func<string, string>>();

            Asserts.AreEqual("a", f("a"));
        }

        public static string GetS(Func<string> getS)
        {
            return getS();
        }


        public void Nested_Action_using_outer_parameter_and_closed_value()
        {
            //Expression<Func<Action<string>>> expr = () => a => s.SetValue(a);

            var s = new S();
            var aParam = Parameter(typeof(string), "a");
            var expr = Lambda(
                Lambda(
                    Call(
                        Constant(s),
                        typeof(S).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(S.SetValue)),
                        aParam),
                    aParam)
                );

            var f = expr.CompileFast<Func<Action<string>>>();

            f()("a");
            Asserts.AreEqual("a", s.Value);
        }

        public class S
        {
            public string Value;

            public void SetValue(string s)
            {
                Value = s;
            }
        }

        // Result delegate to be created by CreateComplexExpression
        public object CreateA(object[] state)
        {
            return new A(
                new B(),
                (string)state[11],
                new ID[] { new D1(), new D2() }
            )
            {
                Prop = new P(new B()),
                Bop = new B()
            };
        }

        private static ConstructorInfo _ctorOfA = typeof(A).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfB = typeof(B).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfP = typeof(P).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfD1 = typeof(D1).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfD2 = typeof(D2).GetTypeInfo().DeclaredConstructors.First();
        private static PropertyInfo _propAProp = typeof(A).GetTypeInfo().DeclaredProperties.First(p => p.Name == "Prop");
        private static FieldInfo _fieldABop = typeof(A).GetTypeInfo().DeclaredFields.First(p => p.Name == "Bop");

        public static System.Linq.Expressions.Expression<Func<object[], object>> CreateComplexExpression(string p = null)
        {
            var stateParamExpr = SysExpr.Parameter(typeof(object[]), p);

            var expr = SysExpr.Lambda<Func<object[], object>>(
                SysExpr.MemberInit(
                    SysExpr.New(_ctorOfA,
                        SysExpr.New(_ctorOfB),
                        SysExpr.Convert(SysExpr.ArrayIndex(stateParamExpr, SysExpr.Constant(11)), typeof(string)),
                        SysExpr.NewArrayInit(typeof(ID),
                            SysExpr.New(_ctorOfD1),
                            SysExpr.New(_ctorOfD2))),
                    SysExpr.Bind(_propAProp,
                        SysExpr.New(_ctorOfP,
                            SysExpr.New(_ctorOfB))),
                    SysExpr.Bind(_fieldABop,
                        SysExpr.New(_ctorOfB))),
                stateParamExpr);

            return expr;
        }

        public static Expression<Func<object[], object>> CreateComplexLightExpression(string p = null)
        {
            var stateParamExpr = ParameterOf<object[]>(p);

            var expr = Lambda<Func<object[], object>>(
                MemberInit(
                    New(_ctorOfA,
                        New(_ctorOfB),
                        Convert(
                            ArrayIndex(stateParamExpr, ConstantInt(11)),
                            typeof(string)),
                        NewArrayInit(typeof(ID),
                            New(_ctorOfD1),
                            New(_ctorOfD2))),
                    Bind(_propAProp,
                        New(_ctorOfP,
                            New(_ctorOfB))),
                    Bind(_fieldABop,
                        New(_ctorOfB))),
                stateParamExpr);

            return expr;
        }

        public static Expression<Func<object[], object>> CreateComplexLightExpression_with_intrinsics(string p = null)
        {
            var stateParamExpr = ParameterOf<object[]>(p);

            var expr = Lambda<Func<object[], object>>(
                MemberInit(
                    NewNoByRefArgs(_ctorOfA,
                        New(_ctorOfB),
                        TryConvertIntrinsic<string>(
                            ArrayIndex(stateParamExpr, ConstantInt(11))),
                        NewArrayInit(typeof(ID),
                            New(_ctorOfD1),
                            New(_ctorOfD2))),
                    Bind(_propAProp,
                        NewNoByRefArgs(_ctorOfP,
                            New(_ctorOfB))),
                    Bind(_fieldABop,
                        New(_ctorOfB))),
                stateParamExpr);

            return expr;
        }

        public static ExprTree CreateComplexFlatExpression(string parameterName = null)
        {
            var fe = default(ExprTree);
            var stateParamExpr = fe.ParameterOf<object[]>(parameterName);
            var body = fe.MemberInit(
                fe.New(_ctorOfA,
                    fe.New(_ctorOfB),
                    fe.Convert(
                        fe.ArrayIndex(stateParamExpr, fe.ConstantInt(11)),
                        typeof(string)),
                    fe.NewArrayInit(typeof(ID),
                        fe.New(_ctorOfD1),
                        fe.New(_ctorOfD2))),
                fe.Bind(_propAProp,
                    fe.New(_ctorOfP,
                        fe.New(_ctorOfB))),
                fe.Bind(_fieldABop,
                    fe.New(_ctorOfB)));
            fe.RootIdx = fe.Lambda<Func<object[], object>>(body, stateParamExpr);
            return fe;
        }

        public void Can_compile_complex_expr_with_Arrays_and_Casts()
        {
            var expr = CreateComplexLightExpression();

            var func = expr.CompileFast(true);

            var input = new object[12];
            for (int i = 0; i < input.Length; i++)
                input[i] = i + "";
            var x = func(input);

            Asserts.AreEqual("11", ((A)x).Sop);
        }


        public void Can_compile_complex_expr_with_perf_tricks_with_Arrays_and_Casts()
        {
            var expr = CreateComplexLightExpression_with_intrinsics();

            var func = expr.CompileFast(true);

            var input = new object[12];
            for (int i = 0; i < input.Length; i++)
                input[i] = i + "";
            var x = func(input);

            Asserts.AreEqual("11", ((A)x).Sop);
        }


        public void Should_output_the_System_and_LightExpression_to_the_identical_construction_syntax()
        {
            var se = CreateComplexExpression("p");
            var le = CreateComplexLightExpression("p");

            var ses = se.FromSysExpression().ToExpressionString();
            var les = le.ToExpressionString();

            Asserts.Contains("MemberInit", ses);
            Asserts.AreEqual(ses, les);
        }


        public void Should_output_the_System_and_LightExpression_to_the_identical_CSharp_syntax()
        {
            var se = CreateComplexExpression("p");
            var le = CreateComplexLightExpression("p");

            var ses = se.FromSysExpression().ToCSharpString();
            var les = le.ToCSharpString();

            Asserts.Contains("Prop = new LightExpressionTests.P(new LightExpressionTests.B())", ses);
            Asserts.AreEqual(ses, les);
        }


        public void Expression_produced_by_ToExpressionString_should_compile()
        {
            var p = new ParameterExpression[1]; // the parameter expressions 
            var e = new Expression[12]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda( // $
            typeof(System.Func<object[], object>),
            e[0] = MemberInit(
                e[1] = New(/*3 args*/
                typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.A).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                e[2] = New(/*0 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.B).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]),
                e[3] = Convert(
                    e[4] = MakeBinary(ExpressionType.ArrayIndex,
                    p[0] = Parameter(typeof(object[])),
                    e[5] = Constant((int)11)),
                    typeof(string)),
                e[6] = NewArrayInit(
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.ID),
                    e[7] = New(/*0 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.D1).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]),
                    e[8] = New(/*0 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.D2).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))),
                Bind(
                typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.A).GetTypeInfo().GetDeclaredProperty("Prop"),
                e[9] = New(/*1 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.P).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                    e[10] = New(/*0 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.B).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))),
                Bind(
                typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.A).GetTypeInfo().GetDeclaredField("Bop"),
                e[11] = New(/*0 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.B).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))),
            p[0 // (object[] object_arr__38113962)
                ]);

            var f = (System.Func<object[], object>)expr.CompileFast();
            f(new object[22]);
        }


        public void Multiple_methods_in_block_should_be_aligned_when_output_to_csharp()
        {
            var sayHi = GetType().GetMethod(nameof(SayHi));
            var p = Parameter(typeof(int), "i");
            var e = Lambda<Action<int>>(Block(Call(sayHi, p, p), Call(sayHi, p, p), Call(sayHi, p, p)), p);

            var s = e.ToCSharpString();
            Asserts.Contains("SayHi", s);
        }

        public static void SayHi(int i, int j) { }

        public void Can_roundtrip_light_expression_through_flat_expression()
        {
            var expr = CreateComplexExpression("state");

            ExprTree flat = default;
            flat.FromSysExpr(expr);

            Asserts.IsTrue(flat.Nodes.Count > 0);
            Asserts.AreEqual(0, flat.ClosureConstants.Count);

            var roundtrip = (LambdaExpression)flat.ToLightExpression();
            var func = roundtrip.CompileFast<Func<object[], object>>(true);
            var state = new object[12];
            state[11] = "flat";
            var a = (A)func(state);

            Asserts.AreEqual("flat", a.Sop);
            Asserts.IsInstanceOf<P>(a.Prop);
            Asserts.AreEqual(2, a.Dop.Count());
        }

        public void Flat_expression_preserves_parameter_and_label_identity_and_collects_closure_constants()
        {
            var valueHolder = new S();
            var valueField = typeof(S).GetField(nameof(S.Value));
            var constExpr = Lambda<Func<string>>(Field(Constant(valueHolder), valueField));
            ExprTree constFlat = default;
            constFlat.FromLightExpr(constExpr);

            Asserts.AreEqual(1, constFlat.ClosureConstants.Count);
            Asserts.AreSame(valueHolder, constFlat.ClosureConstants[0]);
            Asserts.AreEqual(null, ((LambdaExpression)constFlat.ToLightExpression()).CompileFast<Func<string>>(true)());
        }

        public void Can_convert_dynamic_runtime_variables_and_debug_info_to_light_expression_and_flat_expression()
        {
            var runtimeParameter = SysExpr.Parameter(typeof(int), "runtime");
            var runtimeVariables = SysExpr.RuntimeVariables(runtimeParameter);
            var runtimeVariablesLight = runtimeVariables.ToLightExpression();
            var runtimeVariablesRoundtrip = runtimeVariablesLight.ToFlatExpression().ToLightExpression();

            Asserts.AreEqual(ExpressionType.RuntimeVariables, runtimeVariablesLight.NodeType);
            Asserts.AreEqual(ExpressionType.RuntimeVariables, runtimeVariablesRoundtrip.NodeType);

            var document = SysExpr.SymbolDocument("flat-expression.cs");
            var debugInfo = SysExpr.DebugInfo(document, 1, 1, 1, 10);
            var debugInfoLight = debugInfo.ToLightExpression();
            var debugInfoRoundtrip = debugInfoLight.ToFlatExpression().ToLightExpression();

            Asserts.AreEqual(ExpressionType.DebugInfo, debugInfoLight.NodeType);
            Asserts.AreEqual(ExpressionType.DebugInfo, debugInfoRoundtrip.NodeType);

            var dynamicArgument = SysExpr.Parameter(typeof(object), "arg");
            var binder = Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags.None, "Length", typeof(LightExpressionTests),
                new[] { Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.None, null) });
            var dynamicExpression = SysExpr.MakeDynamic(typeof(Func<CallSite, object, object>), binder, new[] { dynamicArgument });

            var dynamicLight = dynamicExpression.ToLightExpression();
            var dynamicRoundtrip = dynamicLight.ToFlatExpression().ToLightExpression();

            Asserts.AreEqual(ExpressionType.Dynamic, dynamicLight.NodeType);
            Asserts.AreEqual(ExpressionType.Dynamic, dynamicRoundtrip.NodeType);
            Asserts.AreEqual(ExpressionType.Dynamic, dynamicLight.ToFlatExpression().ToExpression().NodeType);
        }

        public void Can_build_flat_expression_directly_with_light_expression_like_api()
        {
            var fe = CreateComplexFlatExpression("state");
            var lambda = (LambdaExpression)fe.ToLightExpression();
            var func = lambda.CompileFast<Func<object[], object>>(true);
            var runtimeState = new object[12];
            runtimeState[11] = "direct";

            var a = (A)func(runtimeState);

            Asserts.AreEqual("direct", a.Sop);
            Asserts.IsInstanceOf<P>(a.Prop);
            Asserts.AreEqual(2, a.Dop.Count());
        }

        public void Can_build_flat_expression_control_flow_directly()
        {
            var fe = default(ExprTree);
            var p = fe.Parameter(typeof(int), "p");
            var target = fe.Label(typeof(int), "done");
            fe.RootIdx = fe.Lambda<Func<int, int>>(
                fe.Block(
                    fe.Goto(target, p, typeof(int)),
                    fe.Label(target, fe.ConstantInt(0))),
                p);

            var sysLambda = (System.Linq.Expressions.LambdaExpression)fe.ToExpression();
            var block = (System.Linq.Expressions.BlockExpression)sysLambda.Body;
            var gotoExpr = (System.Linq.Expressions.GotoExpression)block.Expressions[0];
            var label = (System.Linq.Expressions.LabelExpression)block.Expressions[1];

            Asserts.AreSame(sysLambda.Parameters[0], gotoExpr.Value);
            Asserts.AreSame(gotoExpr.Target, label.Target);
        }

        public class A
        {
            public P Prop { get; set; }
            public B Bop;
            public string Sop;
            public IEnumerable<ID> Dop;

            public A(B b, string s, IEnumerable<ID> ds)
            {
                Bop = b;
                Sop = s;
                Dop = ds;
            }
        }

        public class B { }

        public class P { public P(B b) { } }

        public interface ID { }
        public class D1 : ID { }
        public class D2 : ID { }


        public void Can_embed_normal_Expression_into_LightExpression_eg_as_Constructor_argument()
        {
            var func = Lambda(New(_ctorOfP, New(_ctorOfB))).CompileFast<Func<P>>();

            Asserts.IsInstanceOf<P>(func());
        }

        // Tests for decl vs ref nodes and out-of-order decl in lambdas/blocks

        /// <summary>
        /// In the flat encoding, a lambda stores body first then parameters.
        /// So when reading, parameter refs in the body are encountered BEFORE
        /// the parameter decl node in the parameter list (out-of-order decl).
        /// Both should resolve to the exact same SysParameterExpression.
        /// </summary>
        public void Flat_lambda_parameter_ref_before_decl_preserves_identity()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            // body uses p: ref nodes come first when the lambda is encoded/read
            fe.RootIdx = fe.Lambda<Func<int, int>>(fe.Add(p, fe.ConstantInt(1)), p);

            var sysLambda = (System.Linq.Expressions.LambdaExpression)fe.ToExpression();
            var add = (System.Linq.Expressions.BinaryExpression)sysLambda.Body;

            // The parameter in the params list and its ref in the body must be the same object
            Asserts.AreSame(sysLambda.Parameters[0], add.Left);
        }

        /// <summary>
        /// A parameter referenced more than once in a lambda body (all refs are
        /// out-of-order relative to the single decl at the end of the child list)
        /// must all resolve to the same SysParameterExpression.
        /// </summary>
        public void Flat_lambda_multiple_parameter_refs_all_yield_same_identity()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            // p * p + p: three independent refs to the same parameter
            fe.RootIdx = fe.Lambda<Func<int, int>>(
                fe.Add(fe.MakeBinary(System.Linq.Expressions.ExpressionType.Multiply, p, p), p),
                p);

            var sysLambda = (System.Linq.Expressions.LambdaExpression)fe.ToExpression();
            var add = (System.Linq.Expressions.BinaryExpression)sysLambda.Body;
            var mul = (System.Linq.Expressions.BinaryExpression)add.Left;
            var paramDecl = sysLambda.Parameters[0];

            Asserts.AreSame(paramDecl, mul.Left);
            Asserts.AreSame(paramDecl, mul.Right);
            Asserts.AreSame(paramDecl, add.Right);
        }

        public void Flat_repeated_child_references_do_not_create_self_cycle()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            fe.RootIdx = fe.Lambda<Func<int, int>>(fe.Add(p, p), p);

            var sysLambda = (System.Linq.Expressions.LambdaExpression)fe.ToExpression();
            var add = (System.Linq.Expressions.BinaryExpression)sysLambda.Body;

            Asserts.AreSame(sysLambda.Parameters[0], add.Left);
            Asserts.AreSame(sysLambda.Parameters[0], add.Right);
        }

        /// <summary>
        /// Block variables are read before body expressions (normal order),
        /// but each variable idx is cloned whenever it appears as a child.
        /// All clones must resolve to the same SysParameterExpression.
        /// </summary>
        public void Flat_block_variables_and_refs_yield_same_identity()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            var v1 = fe.Variable(typeof(int), "v1");
            var v2 = fe.Variable(typeof(int), "v2");
            // { int v1, v2; v1 = p; v2 = v1 + 1; v2 }
            var block = fe.Block(typeof(int),
                new[] { v1, v2 },
                fe.Assign(v1, p),
                fe.Assign(v2, fe.Add(v1, fe.ConstantInt(1))),
                v2);
            fe.RootIdx = fe.Lambda<Func<int, int>>(block, p);

            var sysLambda = (System.Linq.Expressions.LambdaExpression)fe.ToExpression();
            var sysBlock = (System.Linq.Expressions.BlockExpression)sysLambda.Body;
            var assign1 = (System.Linq.Expressions.BinaryExpression)sysBlock.Expressions[0]; // v1 = p
            var assign2 = (System.Linq.Expressions.BinaryExpression)sysBlock.Expressions[1]; // v2 = v1 + 1
            var addExpr = (System.Linq.Expressions.BinaryExpression)assign2.Right;            // v1 + 1

            // v1 decl and its ref on the left of assign1 are the same object
            Asserts.AreSame(sysBlock.Variables[0], assign1.Left);
            // v1 decl and its ref inside the add expression are the same object
            Asserts.AreSame(sysBlock.Variables[0], addExpr.Left);
            // v2 decl and its ref on the left of assign2 are the same object
            Asserts.AreSame(sysBlock.Variables[1], assign2.Left);
            // v2 decl and the final block result expression are the same object
            Asserts.AreSame(sysBlock.Variables[1], sysBlock.Expressions[2]);
        }

        /// <summary>
        /// An outer lambda parameter captured in a nested lambda body creates
        /// a ref node in the nested lambda scope. All three occurrences -
        /// the outer params list, the inner body, and any outer body usage -
        /// must resolve to the exact same SysParameterExpression.
        /// </summary>
        public void Flat_nested_lambda_captures_outer_parameter_identity()
        {
            var fe = default(ExprTree);
            var x = fe.ParameterOf<int>("x");
            // outer: x => () => x  (inner lambda closes over outer param)
            var inner = fe.Lambda<Func<int>>(x);
            fe.RootIdx = fe.Lambda<Func<int, Func<int>>>(inner, x);

            var sysOuter = (System.Linq.Expressions.LambdaExpression)fe.ToExpression();
            var sysInner = (System.Linq.Expressions.LambdaExpression)sysOuter.Body;

            // The inner lambda body (the x ref) must be the same object as the outer param decl
            Asserts.AreSame(sysOuter.Parameters[0], sysInner.Body);
        }

        public void Flat_lambda_closure_parameter_usages_track_captured_outer_parameter_during_direct_construction()
        {
            var fe = default(ExprTree);
            var x = fe.ParameterOf<int>("x");
            var inner = fe.Lambda<Func<int>>(x);
            fe.RootIdx = fe.Lambda<Func<int, Func<int>>>(inner, x);

            Asserts.AreEqual(1, fe.LambdaClosureParameterUsages.Count);
            Asserts.AreEqual(inner, fe.LambdaClosureParameterUsages[0].LambdaIdx);
            Asserts.AreEqual(fe.Nodes[x].ChildIdx, fe.LambdaClosureParameterUsages[0].ParameterId);
        }

        public void Flat_lambda_closure_parameter_usages_propagate_across_nested_lambdas_during_direct_construction()
        {
            var fe = default(ExprTree);
            var x = fe.ParameterOf<int>("x");
            var inner = fe.Lambda<Func<int>>(x);
            var middle = fe.Lambda<Func<Func<int>>>(inner);
            fe.RootIdx = fe.Lambda<Func<int, Func<Func<int>>>>(middle, x);

            Asserts.AreEqual(2, fe.LambdaClosureParameterUsages.Count);

            var foundInner = false;
            var foundMiddle = false;
            for (var i = 0; i < fe.LambdaClosureParameterUsages.Count; ++i)
            {
                ref var usage = ref fe.LambdaClosureParameterUsages[i];
                Asserts.AreEqual(fe.Nodes[x].ChildIdx, usage.ParameterId);
                if (usage.LambdaIdx == inner) foundInner = true;
                if (usage.LambdaIdx == middle) foundMiddle = true;
            }

            Asserts.IsTrue(foundInner);
            Asserts.IsTrue(foundMiddle);
        }

        public void Flat_lambda_closure_parameter_usages_track_captured_block_variable_during_direct_construction()
        {
            var fe = default(ExprTree);
            var v = fe.Variable(typeof(int), "v");
            var inner = fe.Lambda<Func<int>>(v);
            fe.RootIdx = fe.Lambda<Func<Func<int>>>(
                fe.Block(typeof(Func<int>), new[] { v },
                    fe.Assign(v, fe.ConstantInt(42)),
                    inner));

            Asserts.AreEqual(1, fe.LambdaClosureParameterUsages.Count);
            Asserts.AreEqual(inner, fe.LambdaClosureParameterUsages[0].LambdaIdx);
            Asserts.AreEqual(fe.Nodes[v].ChildIdx, fe.LambdaClosureParameterUsages[0].ParameterId);
        }

        public void Flat_lambda_closure_parameter_usages_track_captures_from_expression_conversion()
        {
            var x = SysExpr.Parameter(typeof(int), "x");
            var sysLambda = SysExpr.Lambda<Func<int, Func<int>>>(
                SysExpr.Lambda<Func<int>>(x),
                x);

            var fe = sysLambda.ToFlatExpression();

            Asserts.AreEqual(1, fe.LambdaClosureParameterUsages.Count);
            Asserts.AreEqual(fe.Nodes[fe.LambdaClosureParameterUsages[0].ParameterIdx].ChildIdx, fe.LambdaClosureParameterUsages[0].ParameterId);

            var nestedLambdaCount = 0;
            for (var i = 0; i < fe.LambdaNodes.Count; ++i)
                if (fe.LambdaNodes[i] != fe.RootIdx)
                {
                    ++nestedLambdaCount;
                    Asserts.AreEqual(fe.LambdaNodes[i], fe.LambdaClosureParameterUsages[0].LambdaIdx);
                }

            Asserts.AreEqual(1, nestedLambdaCount);
        }

        /// <summary>
        /// End-to-end compile-and-run test with a block containing two variables,
        /// verifying that out-of-order parameter decls and variable refs produce
        /// a correctly executing delegate.
        /// </summary>
        public void Flat_out_of_order_decl_block_in_lambda_compiles_correctly()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            var v1 = fe.Variable(typeof(int), "v1");
            var v2 = fe.Variable(typeof(int), "v2");
            // (int p) => { int v1 = p * 2; int v2 = v1 + p; v2 }
            var block = fe.Block(typeof(int),
                new[] { v1, v2 },
                fe.Assign(v1, fe.MakeBinary(System.Linq.Expressions.ExpressionType.Multiply, p, fe.ConstantInt(2))),
                fe.Assign(v2, fe.Add(v1, p)),
                v2);
            fe.RootIdx = fe.Lambda<Func<int, int>>(block, p);

            var func = (Func<int, int>)((System.Linq.Expressions.LambdaExpression)fe.ToExpression()).Compile();
            // p=3 -> v1 = 3*2=6, v2 = 6+3=9
            Asserts.AreEqual(9, func(3));
            // p=0 -> v1 = 0, v2 = 0
            Asserts.AreEqual(0, func(0));
        }

        enum ByteEnum : byte { A = 1, B = 200 }
        enum SByteEnum : sbyte { A = -1, B = 50 }
        enum ShortEnum : short { A = -1000, B = 30000 }
        enum UShortEnum : ushort { A = 0, B = 60000 }
        enum IntEnum : int { A = int.MinValue, B = 42 }
        enum UIntEnum : uint { A = 0, B = uint.MaxValue }

        public void Flat_enum_constant_stored_inline_roundtrip()
        {
            // Verify that enum constants with <=32-bit underlying types are stored inline
            // (no ClosureConstants entry, no boxing) and round-trip correctly.
            void Check<TEnum>(TEnum enumValue) where TEnum : Enum
            {
                var fe = default(ExprTree);
                var idx = fe.Constant(enumValue, typeof(TEnum));
                Asserts.AreEqual(0, fe.ClosureConstants.Count,
                    $"{typeof(TEnum).Name}.{enumValue} should be inline (no ClosureConstants), but got {fe.ClosureConstants.Count}");
                fe.RootIdx = fe.Lambda<Func<TEnum>>(idx);
                var result = (TEnum)((System.Linq.Expressions.LambdaExpression)fe.ToExpression()).Compile().DynamicInvoke()!;
                Asserts.AreEqual(enumValue, result, $"Round-trip failed for {typeof(TEnum).Name}.{enumValue}");
            }

            Check(ByteEnum.A);
            Check(ByteEnum.B);
            Check(SByteEnum.A);
            Check(SByteEnum.B);
            Check(ShortEnum.A);
            Check(ShortEnum.B);
            Check(UShortEnum.A);
            Check(UShortEnum.B);
            Check(IntEnum.A);
            Check(IntEnum.B);
            Check(UIntEnum.A);
            Check(UIntEnum.B);
        }

        public void Flat_metadata_uses_16_bit_idx_storage()
        {
            Asserts.AreEqual(24, typeof(ExprNode).StructLayoutAttribute.Size);
            Asserts.AreEqual(6, typeof(LambdaClosureParameterUsage).StructLayoutAttribute.Size);

            var fe = default(ExprTree);
            // Index 0 is reserved as the absent-child sentinel, so MaxValue-1 real nodes fill storage.
            for (var i = 1; i < ushort.MaxValue; ++i)
                fe.Default(typeof(int));

            Asserts.AreEqual(ushort.MaxValue, fe.Nodes.Count);
            Asserts.Throws<OverflowException>(() => fe.ParameterOf<int>("overflow"));
        }

        /// <summary>
        /// When building a flat expression directly, calling Lambda() for a nested lambda
        /// and then for the root lambda should result in both indices recorded in LambdaNodes.
        /// The root is identified by RootIdx; all others are nested.
        /// </summary>
        public void Flat_lambda_nodes_tracks_all_lambdas_during_direct_construction()
        {
            var fe = default(ExprTree);
            var x = fe.ParameterOf<int>("x");

            // Build: outer: x => () => x
            var inner = fe.Lambda<Func<int>>(x);
            fe.RootIdx = fe.Lambda<Func<int, Func<int>>>(inner, x);

            // Both the root and nested lambda indices should be recorded
            Asserts.AreEqual(2, fe.LambdaNodes.Count);

            // Check that inner and root are both in LambdaNodes
            var foundInner = false;
            var foundRoot = false;
            for (var i = 0; i < fe.LambdaNodes.Count; i++)
            {
                if (fe.LambdaNodes[i] == inner) foundInner = true;
                if (fe.LambdaNodes[i] == fe.RootIdx) foundRoot = true;
            }
            Asserts.IsTrue(foundInner);
            Asserts.IsTrue(foundRoot);

            // Nested lambdas are all LambdaNodes entries that are not the root
            var nestedCount = 0;
            for (var i = 0; i < fe.LambdaNodes.Count; i++)
                if (fe.LambdaNodes[i] != fe.RootIdx)
                    ++nestedCount;
            Asserts.AreEqual(1, nestedCount);
        }

        /// <summary>
        /// When building a flat expression with multiple levels of nesting,
        /// all lambda node indices are captured in LambdaNodes.
        /// </summary>
        public void Flat_lambda_nodes_tracks_deeply_nested_lambdas_during_direct_construction()
        {
            var fe = default(ExprTree);
            var x = fe.ParameterOf<int>("x");

            // Build: outer: x => (() => (() => x))
            var innermost = fe.Lambda<Func<int>>(x);
            var middle = fe.Lambda<Func<Func<int>>>(innermost);
            fe.RootIdx = fe.Lambda<Func<int, Func<Func<int>>>>(middle, x);

            // All three lambda nodes should be recorded
            Asserts.AreEqual(3, fe.LambdaNodes.Count);

            // Count nested (non-root) lambdas
            var nestedCount = 0;
            for (var i = 0; i < fe.LambdaNodes.Count; i++)
                if (fe.LambdaNodes[i] != fe.RootIdx)
                    ++nestedCount;
            Asserts.AreEqual(2, nestedCount);
        }

        /// <summary>
        /// When converting a System.Linq expression tree with nested lambdas via FromExpression,
        /// the resulting ExprTree should have all lambda indices populated in LambdaNodes.
        /// </summary>
        public void Flat_lambda_nodes_tracks_lambdas_from_expression_conversion()
        {
            var p = SysExpr.Parameter(typeof(int), "p");
            // Build: p => () => p  using System.Linq.Expressions
            var sysLambda = SysExpr.Lambda<Func<int, Func<int>>>(
                SysExpr.Lambda<Func<int>>(p),
                p);

            var fe = sysLambda.ToFlatExpression();

            // Both root and nested lambda indices should be recorded
            Asserts.AreEqual(2, fe.LambdaNodes.Count);

            // The root lambda must be in the list
            var foundRoot = false;
            for (var i = 0; i < fe.LambdaNodes.Count; i++)
                if (fe.LambdaNodes[i] == fe.RootIdx) { foundRoot = true; break; }
            Asserts.IsTrue(foundRoot);

            // Exactly one nested lambda
            var nestedCount = 0;
            for (var i = 0; i < fe.LambdaNodes.Count; i++)
                if (fe.LambdaNodes[i] != fe.RootIdx)
                    ++nestedCount;
            Asserts.AreEqual(1, nestedCount);
        }

        /// <summary>
        /// A flat expression with no nested lambdas (root-only) should have exactly one
        /// entry in LambdaNodes (the root itself).
        /// </summary>
        public void Flat_lambda_nodes_has_single_entry_for_root_only_lambda()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            fe.RootIdx = fe.Lambda<Func<int, int>>(fe.Add(p, fe.ConstantInt(1)), p);

            Asserts.AreEqual(1, fe.LambdaNodes.Count);
            Asserts.AreEqual(fe.RootIdx, fe.LambdaNodes[0]);
        }

        /// <summary>
        /// Block nodes with explicit variable declarations are recorded in BlocksWithVariables;
        /// blocks without variables produce no entry.
        /// </summary>
        public void Flat_blocks_with_variables_tracked_during_direct_construction()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            var v = fe.Variable(typeof(int), "v");

            // Block with one variable: should be tracked
            var blockWithVar = fe.Block(typeof(int), new[] { v }, fe.Assign(v, p), v);
            // Block without variables: should NOT be tracked
            var blockNoVar = fe.Block(fe.Add(p, fe.ConstantInt(1)));

            fe.RootIdx = fe.Lambda<Func<int, int>>(fe.Block(blockWithVar, blockNoVar), p);

            Asserts.AreEqual(1, fe.BlocksWithVariables.Count);
            Asserts.AreEqual(blockWithVar, fe.BlocksWithVariables[0]);
        }

        /// <summary>
        /// Goto and label expression nodes are recorded in GotoNodes and LabelNodes respectively.
        /// </summary>
        public void Flat_goto_and_label_nodes_tracked_during_direct_construction()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            var target = fe.Label(typeof(int), "done");

            var gotoNode = fe.Goto(target, p, typeof(int));
            var labelNode = fe.Label(target, fe.ConstantInt(0));

            fe.RootIdx = fe.Lambda<Func<int, int>>(fe.Block(gotoNode, labelNode), p);

            Asserts.AreEqual(1, fe.GotoNodes.Count);
            Asserts.AreEqual(gotoNode, fe.GotoNodes[0]);

            Asserts.AreEqual(1, fe.LabelNodes.Count);
            Asserts.AreEqual(labelNode, fe.LabelNodes[0]);
        }

        /// <summary>
        /// Try/catch, try/finally and try/fault node indices are all recorded in TryCatchNodes.
        /// </summary>
        public void Flat_try_catch_nodes_tracked_during_direct_construction()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");

            var tryCatchNode = fe.TryCatch(
                fe.Add(p, fe.ConstantInt(1)),
                fe.Catch(typeof(Exception), fe.ConstantInt(-1)));

            var tryFinallyNode = fe.TryFinally(
                fe.Add(p, fe.ConstantInt(2)),
                fe.Default(typeof(void)));

            fe.RootIdx = fe.Lambda<Func<int, int>>(
                fe.Block(tryCatchNode, tryFinallyNode), p);

            Asserts.AreEqual(2, fe.TryCatchNodes.Count);

            var foundTryCatch = false;
            var foundTryFinally = false;
            for (var i = 0; i < fe.TryCatchNodes.Count; i++)
            {
                if (fe.TryCatchNodes[i] == tryCatchNode) foundTryCatch = true;
                if (fe.TryCatchNodes[i] == tryFinallyNode) foundTryFinally = true;
            }
            Asserts.IsTrue(foundTryCatch);
            Asserts.IsTrue(foundTryFinally);
        }

        /// <summary>
        /// When converting a System.Linq expression tree, blocks with variables are
        /// recorded in BlocksWithVariables; plain blocks are not.
        /// </summary>
        public void Flat_blocks_with_variables_tracked_from_expression_conversion()
        {
            var p = SysExpr.Parameter(typeof(int), "p");
            var v = SysExpr.Variable(typeof(int), "v");
            // block with variable
            var sysBlock = SysExpr.Block(new[] { v }, SysExpr.Assign(v, p), v);
            var sysLambda = SysExpr.Lambda<Func<int, int>>(sysBlock, p);

            var fe = sysLambda.ToFlatExpression();

            Asserts.AreEqual(1, fe.BlocksWithVariables.Count);
        }

        /// <summary>
        /// When converting a System.Linq expression tree with goto/label, both
        /// GotoNodes and LabelNodes are populated.
        /// </summary>
        public void Flat_goto_and_label_nodes_tracked_from_expression_conversion()
        {
            var p = SysExpr.Parameter(typeof(int), "p");
            var target = SysExpr.Label(typeof(int), "done");
            var sysLambda = SysExpr.Lambda<Func<int, int>>(
                SysExpr.Block(
                    SysExpr.Goto(target, p, typeof(int)),
                    SysExpr.Label(target, SysExpr.Constant(0))),
                p);

            var fe = sysLambda.ToFlatExpression();

            Asserts.AreEqual(1, fe.GotoNodes.Count);
            Asserts.AreEqual(1, fe.LabelNodes.Count);
        }

        /// <summary>
        /// When converting a System.Linq expression tree with a try/catch,
        /// TryCatchNodes is populated.
        /// </summary>
        public void Flat_try_catch_nodes_tracked_from_expression_conversion()
        {
            var p = SysExpr.Parameter(typeof(int), "p");
            var sysLambda = SysExpr.Lambda<Func<int, int>>(
                SysExpr.TryCatch(
                    SysExpr.Add(p, SysExpr.Constant(1)),
                    SysExpr.Catch(typeof(Exception), SysExpr.Constant(-1))),
                p);

            var fe = sysLambda.ToFlatExpression();

            Asserts.AreEqual(1, fe.TryCatchNodes.Count);
        }

        public void Flat_equal_lambdas_with_different_parameter_names_are_structurally_equal_and_hash_equal()
        {
            var x = Parameter(typeof(int), "x");
            var left = Lambda<Func<int, int>>(Add(x, Constant(1)), x).ToFlatExpression();

            var y = Parameter(typeof(int), "y");
            var right = Lambda<Func<int, int>>(Add(y, Constant(1)), y).ToFlatExpression();

            Asserts.IsTrue(left.Equals(right));
            Asserts.IsTrue(left == right);
            Asserts.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        public void Flat_equal_nested_lambdas_with_captures_are_structurally_equal_and_hash_equal()
        {
            var x = Parameter(typeof(int), "x");
            var left = Lambda<Func<int, Func<int>>>(
                Lambda<Func<int>>(Add(x, Constant(1))),
                x).ToFlatExpression();

            var y = Parameter(typeof(int), "value");
            var right = Lambda<Func<int, Func<int>>>(
                Lambda<Func<int>>(Add(y, Constant(1))),
                y).ToFlatExpression();

            Asserts.IsTrue(left.Equals(right));
            Asserts.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        public void Flat_standalone_parameters_use_name_in_structural_equality()
        {
            var left = Parameter(typeof(int), "x").ToFlatExpression();
            var right = Parameter(typeof(int), "y").ToFlatExpression();

            Asserts.IsFalse(left.Equals(right));
        }

        public void Flat_structural_hash_supports_dictionary_lookup()
        {
            var x = Parameter(typeof(int), "x");
            var key = Lambda<Func<int, int>>(Add(x, Constant(1)), x).ToFlatExpression();
            var dict = new Dictionary<ExprTree, string> { [key] = "found" };

            var lookup = default(ExprTree);
            var y = lookup.ParameterOf<int>("arg");
            lookup.RootIdx = lookup.Lambda<Func<int, int>>(lookup.Add(y, lookup.ConstantInt(1)), y);

            Asserts.IsTrue(dict.TryGetValue(lookup, out var value));
            Asserts.AreEqual("found", value);
        }
    }
}

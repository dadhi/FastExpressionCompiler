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
            Light_loop_with_continue_label_and_no_break_label_keeps_the_continue_label_in_the_system_expression();
            Expression_produced_by_ToExpressionString_should_compile();
            Multiple_methods_in_block_should_be_aligned_when_output_to_csharp();
            Can_roundtrip_light_expression_through_flat_expression();
            Flat_expression_preserves_parameter_and_label_identity_and_collects_closure_constants();
            Can_convert_dynamic_runtime_variables_and_debug_info_to_light_expression_and_flat_expression();
            Can_build_flat_expression_directly_with_light_expression_like_api();
            Can_build_flat_expression_control_flow_directly();
            Can_property_test_generated_flat_expression_roundtrip_structurally();
            Can_fuzz_optional_children_presence_in_flat_expression();
            Can_compile_generated_flat_expression_natively();
            Flat_compile_handles_bool_result_and_falls_back_outside_the_subset();
            Experiment_compare_flat_and_light_compile_phases();

            Flat_lambda_parameter_ref_before_decl_preserves_identity();
            Flat_lambda_multiple_parameter_refs_all_yield_same_identity();
            Flat_block_variables_and_refs_yield_same_identity();
            Flat_nested_lambda_captures_outer_parameter_identity();
            Flat_out_of_order_decl_block_in_lambda_compiles_correctly();
            Flat_enum_constant_stored_inline_roundtrip();
            Flat_metadata_uses_16_bit_idx_storage();
            Flat_equal_lambdas_with_different_parameter_names_are_structurally_equal_and_hash_equal();
            Flat_equal_nested_lambdas_with_captures_are_structurally_equal_and_hash_equal();
            Flat_standalone_parameters_use_name_in_structural_equality();
            Flat_structural_hash_supports_dictionary_lookup();
            Can_round_trip_more_expression_kinds_through_system_expressions();
            Can_make_goto_of_all_kinds_with_value_and_type();
            Can_print_new_expression_kinds();
            Reference_equality_is_converted_to_system_reference_comparison();
            Can_print_reference_comparison();
            Can_round_trip_new_expression_kinds_through_system_expressions();
            return 50;
        }

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


        public void Light_loop_with_continue_label_and_no_break_label_keeps_the_continue_label_in_the_system_expression()
        {
            var cont = Label("cont");
            var le = Lambda<Action>(Loop(Block(Continue(cont)), null, cont));
            var loop = (System.Linq.Expressions.LoopExpression)le.ToLambdaExpression().Body;
            Asserts.AreEqual(null, loop.BreakLabel);
            Asserts.AreEqual("cont", loop.ContinueLabel?.Name);
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

            var fe = default(ExprTree);
            // Index 0 is reserved as the absent-child sentinel, so MaxValue-1 real nodes fill storage.
            for (var i = 1; i < ushort.MaxValue; ++i)
                fe.Default(typeof(int));

            Asserts.AreEqual(ushort.MaxValue, fe.Nodes.Count);
            Asserts.Throws<OverflowException>(() => fe.ParameterOf<int>("overflow"));
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
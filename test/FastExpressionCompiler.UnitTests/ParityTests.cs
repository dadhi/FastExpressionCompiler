using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
using System.Linq.Expressions;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    /// <summary>Compiles with the FEC and with the System and compares the results, runs for both the System and the LightExpression</summary>
    public struct ParityTests : ITestX
    {
        public void Run(TestRun t)
        {
            Can_compile_nullable_boolean_and(t);
            Can_compile_nullable_boolean_or(t);
            Nullable_boolean_logic_emits_the_same_IL_shape_as_system(t);
            Coalesce_type_is_the_same_as_in_system_expressions(t);
            Coalesce_with_nullable_left_emits_the_same_IL_shape_as_system(t);
            Can_compile_shifts_with_nullable_shift_count(t);
            Can_compile_right_shift_with_nullable_shift_count(t);
            Type_is_and_type_equal_emit_the_short_IL(t);
            Type_is_and_type_equal_with_the_known_value_type_still_evaluate_the_operand(t);
            Can_compile_type_is_and_type_equal_the_same_as_system(t);
            IsLifted_and_IsLiftedToNull_are_the_same_as_in_system_expressions(t);
            Lifted_comparisons_with_null_operand_and_other_nullable_types_are_the_same_as_system(t);
            Binary_operators_with_methods_and_nullable_operands_are_the_same_as_system(t);
            Unary_operators_with_nullable_and_user_defined_operands_are_the_same_as_system(t);
            Convert_with_the_method_taking_ref_param_is_the_same_as_system(t);
            Negate_checked_of_short_emits_the_short_IL(t);
            Compound_assign_and_inc_dec_with_methods_are_the_same_as_system(t);
            Convert_and_ConvertChecked_between_primitives_nullables_and_enums_are_the_same_as_system(t);
            Arithmetic_on_nullable_by_ref_parameter_is_the_same_as_system(t);
            Compound_assign_with_the_method_and_conversion_lambda_is_the_same_as_system(t);
            Coalesce_with_the_conversion_and_widening_to_decimal_is_the_same_as_system(t);
            Goto_with_the_value_to_the_void_label_is_the_same_as_system(t);
            Goto_and_return_with_the_value_to_the_label_in_the_middle_of_block_are_the_same_as_system(t);
            Label_rules_are_the_same_as_system(t);
            Switch_with_duplicate_test_values_and_void_type_are_the_same_as_system(t);
            Label_and_rethrow_validation_walks_all_node_kinds_the_same_as_system(t);
            Catch_filter_non_exception_catch_and_rethrow_rules_are_the_same_as_system(t);
            Inc_dec_assign_of_primitives_and_nullables_are_the_same_as_system(t);
            Inc_dec_assign_of_block_variable_compared_and_returned_via_label_are_the_same_as_system(t);
#if NET7_0_OR_GREATER
            Half_Int128_and_UInt128_constants_and_conversions_are_the_same_as_system(t);
#endif
            Unary_operators_with_explicit_method_are_the_same_as_system(t);
            Lifted_comparisons_have_the_same_type_and_result_as_system(t);
            And_AndAlso_OrElse_take_the_type_from_the_left_operand(t);
            Can_compile_nullable_boolean_and_also(t);
            Can_compile_nullable_boolean_or_else(t);
            Can_compile_nullable_boolean_not(t);
            Can_compile_goto_with_value_of_all_kinds(t);
            Can_compile_loop_with_break_and_continue_with_value(t);
            Member_and_list_bindings_are_the_same_as_system(t);
            Member_bindings_on_the_struct_property_and_inside_try_are_the_same_as_system(t);
            Reference_equality_of_the_boxed_values_and_equal_objects_is_the_same_as_system(t);
            Type_is_and_equal_with_the_void(t);
        }

        private static int _sideEffectCalls;
        private static bool? SideEffect(bool? value) { ++_sideEffectCalls; return value; }

        public void Can_compile_goto_with_value_of_all_kinds(TestContext t)
        {
            foreach (var kind in new[] { GotoExpressionKind.Goto, GotoExpressionKind.Return, GotoExpressionKind.Break, GotoExpressionKind.Continue })
                foreach (var withType in new[] { false, true })
                {
                    var label = Label(typeof(int), "label");
                    var @goto = MakeGoto(kind, label, Constant(42), withType ? typeof(void) : null);
                    var lambda = Lambda<Func<int>>(Block(@goto, Label(label, Constant(0))));

                    var sys = lambda.CompileSys();
                    t.AreEqual(42, sys());

                    var fast = lambda.CompileFast(true);
                    t.IsNotNull(fast);
                    t.AreEqual(42, fast());
                }
        }

        public void Can_compile_loop_with_break_and_continue_with_value(TestContext t)
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
                t.AreEqual(1 + 3 + 5 + 7, sys(9));

                var fast = lambda.CompileFast(true);
                t.IsNotNull(fast);
                t.AreEqual(sys(9), fast(9));
                t.AreEqual(sys(1), fast(1));
            }
        }
        private static int AndInts(int left, int right) => left & right;

        public void Can_compile_nullable_boolean_not(TestContext t)
        {
            var p = Parameter(typeof(bool?), "p");
            var not = Not(p);
            t.AreEqual(typeof(bool?), not.Type);

            var lambda = Lambda<Func<bool?, bool?>>(not, p);
            var fast = lambda.CompileFast(true);
            t.IsNotNull(fast);
            var sys = lambda.CompileSys();

            t.AreEqual(false, sys(true));
            t.AreEqual(true, sys(false));
            t.AreEqual(null, sys(null));
            foreach (var v in new bool?[] { true, false, null })
                t.AreEqual(sys(v), fast(v));
        }

        public void Can_compile_nullable_boolean_and_also(TestContext t) => CheckNullableShortCircuit(t, true);

        public void Can_compile_nullable_boolean_or_else(TestContext t) => CheckNullableShortCircuit(t, false);

        private void CheckNullableShortCircuit(TestContext t, bool isAnd)
        {
            Func<Expression, Expression, BinaryExpression> make = isAnd ? AndAlso : OrElse;
            Func<Expression, Expression, MethodInfo, BinaryExpression> makeWithMethod = isAnd ? AndAlso : OrElse;
            var expectedShortValue = !isAnd;

            var left = Parameter(typeof(bool?), "left");
            var right = Parameter(typeof(bool?), "right");
            var expr = make(left, right);
            t.AreEqual(typeof(bool?), expr.Type);

            var lambda = Lambda<Func<bool?, bool?, bool?>>(expr, left, right);
            var fast = lambda.CompileFast(true);
            t.IsNotNull(fast);
            var sys = lambda.CompileSys();

            bool? Expected(bool? l, bool? r) => isAnd
                ? (l == false || r == false ? false : l == null || r == null ? null : (bool?)true)
                : (l == true || r == true ? true : l == null || r == null ? null : (bool?)false);

            bool?[] values = { true, false, null };
            foreach (var l in values)
                foreach (var r in values)
                {
                    var sysResult = sys(l, r);
                    t.AreEqual(Expected(l, r), sysResult);
                    t.AreEqual(sysResult, fast(l, r));
                }

            // the right operand must not be evaluated when the left one decides the result
            var sideEffect = typeof(ParityTests).GetMethod(nameof(SideEffect), BindingFlags.Static | BindingFlags.NonPublic);
            var sideEffectLambda = Lambda<Func<bool?, bool?>>(make(left, Call(sideEffect, Constant(null, typeof(bool?)))), left);
            var sideEffectFast = sideEffectLambda.CompileFast(true);
            t.IsNotNull(sideEffectFast);
            _sideEffectCalls = 0;
            t.AreEqual(expectedShortValue, sideEffectFast(expectedShortValue));
            t.AreEqual(0, _sideEffectCalls);
            t.AreEqual(null, sideEffectFast(null));
            t.AreEqual(1, _sideEffectCalls);
            t.AreEqual(null, sideEffectFast(!expectedShortValue));
            t.AreEqual(2, _sideEffectCalls);

            var withNullMethod = makeWithMethod(left, right, null);
            t.AreEqual(typeof(bool?), withNullMethod.Type);
            var nullMethodLambda = Lambda<Func<bool?, bool?, bool?>>(withNullMethod, left, right);
            var nullMethodFast = nullMethodLambda.CompileFast(true);
            t.IsNotNull(nullMethodFast);
            var nullMethodSys = nullMethodLambda.CompileSys();
            foreach (var l in values)
                foreach (var r in values)
                    t.AreEqual(nullMethodSys(l, r), nullMethodFast(l, r));

            t.Throws<InvalidOperationException>(() =>
                makeWithMethod(Parameter(typeof(bool), "nonNullableLeft"), right, null));
            t.Throws<InvalidOperationException>(() =>
                makeWithMethod(left, Parameter(typeof(int?), "unrelatedRight"), null));

            var incompatibleMethod = typeof(ParityTests).GetMethod(
                nameof(AndInts), BindingFlags.Static | BindingFlags.NonPublic);
            t.Throws<InvalidOperationException>(() => makeWithMethod(left, right, incompatibleMethod));
        }

        public void Can_compile_nullable_boolean_and(TestContext t)
        {
            var left = Parameter(typeof(bool?), "left");
            var right = Parameter(typeof(bool?), "right");
            var and = And(left, right);
            t.AreEqual(typeof(bool?), and.Type);

            var lambda = Lambda<Func<bool?, bool?, bool?>>(and, left, right);
            var fast = lambda.CompileFast(true);
            t.IsNotNull(fast);
            var sys = lambda.CompileSys();

            void AssertResult(bool? leftValue, bool? rightValue, bool? expected)
            {
                var sysResult = sys(leftValue, rightValue);
                t.AreEqual(expected, sysResult);
                t.AreEqual(sysResult, fast(leftValue, rightValue));
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
            t.AreEqual(typeof(bool?), andWithNullMethod.Type);
            var nullMethodLambda = Lambda<Func<bool?, bool?, bool?>>(andWithNullMethod, left, right);
            var nullMethodFast = nullMethodLambda.CompileFast(true);
            t.IsNotNull(nullMethodFast);
            var nullMethodSys = nullMethodLambda.CompileSys();
            t.AreEqual(nullMethodSys(false, null), nullMethodFast(false, null));

            var nonNullableLeft = Parameter(typeof(bool), "nonNullableLeft");
            t.Throws<InvalidOperationException>(() =>
                And(nonNullableLeft, right, (MethodInfo)null));
            t.Throws<InvalidOperationException>(() =>
                And(left, Parameter(typeof(int?), "unrelatedRight"), (MethodInfo)null));

            var incompatibleMethod = typeof(ParityTests).GetMethod(
                nameof(AndInts), BindingFlags.Static | BindingFlags.NonPublic);
            t.IsNotNull(incompatibleMethod);
            t.Throws<InvalidOperationException>(() => And(left, right, incompatibleMethod));
        }

        public void Coalesce_with_nullable_left_emits_the_same_IL_shape_as_system(TestContext t)
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
        public void Coalesce_type_is_the_same_as_in_system_expressions(TestContext t)
        {
            var ni = Parameter(typeof(int?), "ni");
            var nl = Parameter(typeof(long?), "nl");
            var s = Parameter(typeof(string), "s");
            var sysNi = SysExpr.Parameter(typeof(int?), "ni");
            var sysNl = SysExpr.Parameter(typeof(long?), "nl");
            var sysS = SysExpr.Parameter(typeof(string), "s");

            // int? ?? int -> int, int? ?? long -> long (widening of the underlying type), int? ?? double -> double
            var intOrInt = Coalesce(ni, Constant(5));
            t.AreEqual(SysExpr.Coalesce(sysNi, SysExpr.Constant(5)).Type, intOrInt.Type);
            t.AreEqual(typeof(int), intOrInt.Type);

            var intOrLong = Coalesce(ni, Constant(5L));
            t.AreEqual(SysExpr.Coalesce(sysNi, SysExpr.Constant(5L)).Type, intOrLong.Type);
            t.AreEqual(typeof(long), intOrLong.Type);

            var intOrDouble = Coalesce(ni, Constant(5.5));
            t.AreEqual(SysExpr.Coalesce(sysNi, SysExpr.Constant(5.5)).Type, intOrDouble.Type);
            t.AreEqual(typeof(double), intOrDouble.Type);

            // int? ?? int? and long? ?? int stay nullable or widen as in BCL
            var intOrNullableInt = Coalesce(ni, Parameter(typeof(int?), "other"));
            t.AreEqual(typeof(int?), intOrNullableInt.Type);

            var nullableLongOrInt = Coalesce(nl, Constant(5));
            t.AreEqual(SysExpr.Coalesce(sysNl, SysExpr.Constant(5)).Type, nullableLongOrInt.Type);
            t.AreEqual(typeof(long), nullableLongOrInt.Type);

            // int? ?? long? -> long?
            var intOrNullableLong = Coalesce(ni, nl);
            t.AreEqual(SysExpr.Coalesce(sysNi, sysNl).Type, intOrNullableLong.Type);
            t.AreEqual(typeof(long?), intOrNullableLong.Type);

            // reference types
            var strOrObj = Coalesce(s, Constant(new object()));
            t.AreEqual(typeof(object), strOrObj.Type);
            var strOrStr = Coalesce(s, Constant("x"));
            t.AreEqual(typeof(string), strOrStr.Type);

            // the result of compiled lambdas is the same as of the System ones
            var longLambda = Lambda<Func<int?, long>>(intOrLong, ni);
            var longSys = longLambda.CompileSys();
            var longFast = longLambda.CompileFast(true);
            t.AreEqual(5L, longSys(null));
            t.AreEqual(5L, longFast(null));
            t.AreEqual(7L, longSys(7));
            t.AreEqual(7L, longFast(7));

            var doubleLambda = Lambda<Func<int?, double>>(intOrDouble, ni);
            var doubleSys = doubleLambda.CompileSys();
            var doubleFast = doubleLambda.CompileFast(true);
            t.AreEqual(5.5, doubleSys(null));
            t.AreEqual(5.5, doubleFast(null));
            t.AreEqual(7.0, doubleSys(7));
            t.AreEqual(7.0, doubleFast(7));

            var nullableLongLambda = Lambda<Func<int?, long?, long?>>(intOrNullableLong, ni, nl);
            var nlSys = nullableLongLambda.CompileSys();
            var nlFast = nullableLongLambda.CompileFast(true);
            foreach (int? a in new int?[] { null, 3 })
                foreach (long? b in new long?[] { null, 9 })
                    t.AreEqual(nlSys(a, b), nlFast(a, b));

            // the conversion to System expression keeps the type
#if LIGHT_EXPRESSION
            t.AreEqual(typeof(long), intOrLong.ToExpression().Type);
#endif

            // incompatible operands still throw
            t.Throws<ArgumentException>(() => Coalesce(ni, Constant("x")));
            t.Throws<ArgumentException>(() => Coalesce(Parameter(typeof(DateTime?), "d"), Constant(1)));
        }

        public void Nullable_boolean_logic_emits_the_same_IL_shape_as_system(TestContext t)
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

        public void Can_compile_nullable_boolean_or(TestContext t)
        {
            var left = Parameter(typeof(bool?), "left");
            var right = Parameter(typeof(bool?), "right");
            var or = Or(left, right);
            t.AreEqual(typeof(bool?), or.Type);

            var lambda = Lambda<Func<bool?, bool?, bool?>>(or, left, right);
            var fast = lambda.CompileFast(true);
            t.IsNotNull(fast);
            var sys = lambda.CompileSys();

            void AssertResult(bool? l, bool? r, bool? expected)
            {
                t.AreEqual(expected, sys(l, r));
                t.AreEqual(expected, fast(l, r));
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
            t.AreEqual(true, nullMethodFast(null, true));
            t.AreEqual(null, nullMethodFast(null, false));
        }

        public void And_AndAlso_OrElse_take_the_type_from_the_left_operand(TestContext t)
        {
            var i = Parameter(typeof(int), "i");
            var j = Parameter(typeof(int), "j");
            var b = Parameter(typeof(bool), "b");
            var c = Parameter(typeof(bool), "c");
            var sysI = SysExpr.Parameter(typeof(int), "i");
            var sysJ = SysExpr.Parameter(typeof(int), "j");

            // bitwise And/Or on integers is of the integer type, as in System
            t.AreEqual(SysExpr.And(sysI, sysJ).Type, And(i, j).Type);
            t.AreEqual(typeof(int), And(i, j).Type);
            t.AreEqual(typeof(int), Or(i, j).Type);
#if LIGHT_EXPRESSION
            t.AreEqual(typeof(int), And(i, j).ToExpression().Type);
#endif

            var andInts = Lambda<Func<int, int, int>>(And(i, j), i, j);
            t.AreEqual(andInts.CompileSys()(12, 10), andInts.CompileFast(true)(12, 10));
            t.AreEqual(8, andInts.CompileFast(true)(12, 10));

            var longs = Parameter(typeof(long), "l");
            t.AreEqual(typeof(long), And(longs, longs).Type);

            // bool operands stay logical
            t.AreEqual(typeof(bool), And(b, c).Type);
            t.AreEqual(typeof(bool), AndAlso(b, c).Type);
            t.AreEqual(typeof(bool), OrElse(b, c).Type);
            var andBools = Lambda<Func<bool, bool, bool>>(And(b, c), b, c);
            t.AreEqual(false, andBools.CompileFast(true)(true, false));
            t.AreEqual(true, andBools.CompileFast(true)(true, true));

            // bool? operands are lifted
            var nb = Parameter(typeof(bool?), "nb");
            t.AreEqual(typeof(bool?), And(nb, nb).Type);
            t.AreEqual(typeof(bool?), AndAlso(nb, nb).Type);
            t.AreEqual(typeof(bool?), OrElse(nb, nb).Type);
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

        public void Lifted_comparisons_with_null_operand_and_other_nullable_types_are_the_same_as_system(TestContext t)
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
            foreach (var (ty, v1, v2) in cases)
                foreach (var name in names)
                    foreach (var lift in new[] { false, true })
                        foreach (var nullSide in new[] { 0, 1, 2 }) // none, left, right
                        {
                            var what = $"{ty.Name} {name} lift={lift} nullSide={nullSide}";
                            Delegate sysD;
                            try
                            {
                                var sa = SysExpr.Parameter(ty, "a"); var sb = SysExpr.Parameter(ty, "b");
                                var sl = nullSide == 1 ? SysExpr.Constant(null, ty) : (SysExpr)sa;
                                var sr = nullSide == 2 ? SysExpr.Constant(null, ty) : (SysExpr)sb;
                                var se = (SysExpr)typeof(SysExpr).GetMethod(name, sig).Invoke(null, new object[] { sl, sr, lift, null });
                                sysD = SysExpr.Lambda(typeof(Func<,,>).MakeGenericType(ty, ty, se.Type), se, sa, sb).Compile();
                            }
                            catch (System.Reflection.TargetInvocationException) { continue; } // the operator is not defined for the type

                            var la = Parameter(ty, "a"); var lb = Parameter(ty, "b");
                            var ll = nullSide == 1 ? Constant(null, ty) : (Expression)la;
                            var lr = nullSide == 2 ? Constant(null, ty) : (Expression)lb;
                            var le = (Expression)typeof(Expression).GetMethod(name, lsig).Invoke(null, new object[] { ll, lr, lift, null });
                            var fastD = Lambda(typeof(Func<,,>).MakeGenericType(ty, ty, le.Type), le, la, lb).CompileFast(true);
                            t.IsNotNull(fastD, what);
                            ++checkedCount;

                            foreach (var x in new[] { null, v1, v2 })
                                foreach (var y in new[] { null, v1, v2 })
                                    t.AreEqual(sysD.DynamicInvoke(x, y), fastD.DynamicInvoke(x, y), $"{what} ({x}, {y})");
                        }
            t.IsTrue(checkedCount > 100);

            // both operands are constants, as in the ported dotnet/runtime tests
            foreach (var (ty, v1, v2) in cases)
                foreach (var name in names)
                    foreach (var lift in new[] { false, true })
                        foreach (var x in new[] { null, v1, v2 })
                            foreach (var y in new[] { null, v1, v2 })
                            {
                                object expected;
                                try
                                {
                                    var se = (SysExpr)typeof(SysExpr).GetMethod(name, sig).Invoke(null, new object[] { SysExpr.Constant(x, ty), SysExpr.Constant(y, ty), lift, null });
                                    expected = SysExpr.Lambda(typeof(Func<>).MakeGenericType(se.Type), se).Compile().DynamicInvoke();
                                }
                                catch (System.Reflection.TargetInvocationException) { continue; }

                                var le = (Expression)typeof(Expression).GetMethod(name, lsig).Invoke(null, new object[] { Constant(x, ty), Constant(y, ty), lift, null });
                                var fastD = Lambda(typeof(Func<>).MakeGenericType(le.Type), le).CompileFast(true);
                                var what = $"{ty.Name} {name} lift={lift} ({x}, {y})";
                                t.IsNotNull(fastD, what);
                                t.AreEqual(expected, fastD.DynamicInvoke(), what);
                            }

            // unsigned operands above the signed range
            var ua = Parameter(typeof(uint?), "ua"); var ub = Parameter(typeof(uint?), "ub");
            var ult = Lambda<Func<uint?, uint?, bool>>(LessThan(ua, ub), ua, ub).CompileFast(true);
            t.AreEqual(true, ult(1u, uint.MaxValue));
            t.AreEqual(false, ult(uint.MaxValue, 1u));
            var ugt = Lambda<Func<uint?, uint?, bool?>>(GreaterThan(ua, ub, true, null), ua, ub).CompileFast(true);
            t.AreEqual(true, ugt(uint.MaxValue, 1u));
            t.AreEqual(null, ugt(null, 1u));
            var pu = Parameter(typeof(ulong), "pu"); var pv = Parameter(typeof(ulong), "pv");
            t.AreEqual(true, Lambda<Func<ulong, ulong, bool>>(GreaterThan(pu, pv), pu, pv).CompileFast(true)(ulong.MaxValue, 1UL));
            // the other operand is still evaluated when the result is known to be null
            var calls = 0;
            Func<int?> sideEffect = () => { ++calls; return 1; };
            var lt = LessThan(Invoke(Constant(sideEffect)), Constant(null, typeof(int?)), true, null);
            t.AreEqual(typeof(bool?), lt.Type);
            t.AreEqual(null, Lambda<Func<bool?>>(lt).CompileFast(true)());
            var gt = GreaterThan(Constant(null, typeof(int?)), Invoke(Constant(sideEffect)), true, null);
            t.AreEqual(null, Lambda<Func<bool?>>(gt).CompileFast(true)());
            t.AreEqual(2, calls);

            // and the result is usable as a nested operand
            var nb = Parameter(typeof(bool?), "nb");
            var withOr = Lambda<Func<bool?, bool?>>(Or(nb, lt), nb).CompileFast(true);
            t.AreEqual(true, withOr(true));
            t.AreEqual(null, withOr(false));
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

        public void Binary_operators_with_methods_and_nullable_operands_are_the_same_as_system(TestContext t)
        {
            var names = new[] { "Add", "Subtract", "Multiply", "Divide", "Modulo", "And", "Or", "ExclusiveOr", "AddChecked", "SubtractChecked", "MultiplyChecked" };
            var cases = new (Type, object, object, MethodInfo)[]
            {
                (typeof(int?), 1, 2, null), (typeof(int?), 1, 2, typeof(ParityTests).GetMethod("ArCustomInt")),
                (typeof(long?), 1L, 2L, null), (typeof(byte?), (byte)1, (byte)2, null), (typeof(double?), 1.0, 2.0, null), (typeof(uint?), 1u, 2u, null),
                (typeof(decimal?), 1m, 2m, null), (typeof(decimal?), 1m, 2m, typeof(ParityTests).GetMethod("ArCustomDec")),
                (typeof(decimal), 1m, 2m, null), (typeof(DateTime?), new DateTime(2000, 1, 1), new DateTime(2001, 1, 1), null),
                (typeof(ArMoney?), new ArMoney(1), new ArMoney(2), null), (typeof(ArMoney?), new ArMoney(1), new ArMoney(2), typeof(ParityTests).GetMethod("ArCustomMoney")),
                (typeof(ArMoney), new ArMoney(1), new ArMoney(2), null), (typeof(ArMoney), new ArMoney(1), new ArMoney(2), typeof(ParityTests).GetMethod("ArCustomMoney")),
                (typeof(int), 1, 2, typeof(ParityTests).GetMethod("ArCustomInt")),
            };
            var failures = new System.Collections.Generic.List<string>();
            foreach (var (ty, v1, v2, m) in cases)
                foreach (var name in names)
                {
                    var what = $"{ty.Name} {name} method={m?.Name}";
                    Delegate sysD, fastD; Type rt;
                    try
                    {
                        var sa = SysExpr.Parameter(ty, "a"); var sb = SysExpr.Parameter(ty, "b");
                        var se = (SysExpr)typeof(SysExpr).GetMethod(name, new[] { typeof(SysExpr), typeof(SysExpr), typeof(MethodInfo) }).Invoke(null, new object[] { sa, sb, m });
                        rt = se.Type;
                        sysD = SysExpr.Lambda(typeof(Func<,,>).MakeGenericType(ty, ty, rt), se, sa, sb).Compile();
                    }
                    catch (Exception) { continue; }
                    try
                    {
                        var la = Parameter(ty, "a"); var lb = Parameter(ty, "b");
                        var le = (Expression)typeof(Expression).GetMethod(name, new[] { typeof(Expression), typeof(Expression), typeof(MethodInfo) }).Invoke(null, new object[] { la, lb, m });
                        if (le.Type != rt) { failures.Add($"type: {what} sys={rt.Name} light={le.Type.Name}"); continue; }
                        fastD = Lambda(typeof(Func<,,>).MakeGenericType(ty, ty, rt), le, la, lb).CompileFast(true);
                        if (fastD == null) { failures.Add("not compiled: " + what); continue; }
                    }
                    catch (Exception ex) { failures.Add("exception: " + what + " " + (ex.InnerException ?? ex).Message.Split('\n')[0]); continue; }
                    var xs = ty.IsValueType && Nullable.GetUnderlyingType(ty) == null ? new[] { v1, v2 } : new[] { null, v1, v2 };
                    foreach (var x in xs)
                        foreach (var y in xs)
                        {
                            object e, a;
                            try { e = sysD.DynamicInvoke(x, y); } catch (Exception ex) { e = "ex:" + ex.InnerException?.GetType().Name; }
                            try { a = fastD.DynamicInvoke(x, y); } catch (Exception ex) { a = "ex:" + ex.InnerException?.GetType().Name; }
                            if (!Equals(e, a)) { failures.Add($"value: {what} ({x},{y}) sys={e} fec={a}"); break; }
                        }
                }
            t.AreEqual(0, failures.Count, string.Join("; ", failures));
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

        public void Unary_operators_with_nullable_and_user_defined_operands_are_the_same_as_system(TestContext t)
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
            foreach (var (ty, xs) in cases)
                foreach (var name in names)
                {
                    var what = $"{ty.Name} {name}";
                    Delegate sysD, fastD; Type rt;
                    try
                    {
                        var sa = SysExpr.Parameter(ty, "a");
                        var se = (SysExpr)typeof(SysExpr).GetMethod(name, new[] { typeof(SysExpr) }).Invoke(null, new object[] { sa });
                        rt = se.Type;
                        sysD = SysExpr.Lambda(typeof(Func<,>).MakeGenericType(ty, rt), se, sa).Compile();
                    }
                    catch (Exception) { continue; }
                    try
                    {
                        var la = Parameter(ty, "a");
                        var le = (Expression)typeof(Expression).GetMethod(name, new[] { typeof(Expression) }).Invoke(null, new object[] { la });
                        if (le.Type != rt) { failures.Add($"type: {what} sys={rt.Name} light={le.Type.Name}"); continue; }
                        fastD = Lambda(typeof(Func<,>).MakeGenericType(ty, rt), le, la).CompileFast(true);
                        if (fastD == null) { failures.Add("not compiled: " + what); continue; }
                    }
                    catch (Exception ex) { failures.Add("exception: " + what + " " + (ex.InnerException ?? ex).Message.Split('\n')[0]); continue; }

                    // the same operation with the ignored result
                    try
                    {
                        var ia = Parameter(ty, "a");
                        var ie = (Expression)typeof(Expression).GetMethod(name, new[] { typeof(Expression) }).Invoke(null, new object[] { ia });
                        var ignoredD = Lambda(typeof(Func<,>).MakeGenericType(ty, typeof(int)), Block(ie, Constant(1)), ia).CompileFast(true);
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
            t.AreEqual(0, failures.Count, string.Join("\n", failures));
        }
        public static class UnMethods
        {
            public static int Custom(int x) => x * 7 + 1;
            public static int? CustomNullable(int? x) => x == null ? -1 : x * 7 + 1;
            public static long Widen(int x) => (long)x * 1000;
            public static string Show(int x) => "S" + x;
            public static bool Positive(int x) => x > 0;
        }

        public void Unary_operators_with_explicit_method_are_the_same_as_system(TestContext t)
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
            foreach (var (ty, method, xs) in cases)
                foreach (var name in names)
                {
                    var what = $"{ty.Name} {name} {method.Name}";
                    Delegate sysD, fastD, lightSysD; Type rt;
                    try
                    {
                        var sa = SysExpr.Parameter(ty, "a");
                        var se = (SysExpr)typeof(SysExpr).GetMethod(name, new[] { typeof(SysExpr), typeof(MethodInfo) }).Invoke(null, new object[] { sa, method });
                        rt = se.Type;
                        sysD = SysExpr.Lambda(typeof(Func<,>).MakeGenericType(ty, rt), se, sa).Compile();
                    }
                    catch (Exception) { continue; }

                    try
                    {
                        var la = Parameter(ty, "a");
                        var le = (Expression)typeof(Expression).GetMethod(name, new[] { typeof(Expression), typeof(MethodInfo) }).Invoke(null, new object[] { la, method });
                        t.AreSame(method, ((UnaryExpression)le).Method);
                        if (le.Type != rt) { failures.Add($"type: {what} sys={rt.Name} light={le.Type.Name}"); continue; }
                        var lambda = Lambda(typeof(Func<,>).MakeGenericType(ty, rt), le, la);
                        fastD = lambda.CompileFast(true);
                        lightSysD = lambda.CompileSys();
                        if (fastD == null) { failures.Add("not compiled: " + what); continue; }

                        // the same operation with the ignored result
                        var ia = Parameter(ty, "a");
                        var ie = (Expression)typeof(Expression).GetMethod(name, new[] { typeof(Expression), typeof(MethodInfo) }).Invoke(null, new object[] { ia, method });
                        var ignoredD = Lambda(typeof(Func<,>).MakeGenericType(ty, typeof(int)), Block(ie, Constant(1)), ia).CompileFast(true);
                        if (ignoredD == null) { failures.Add("ignored not compiled: " + what); continue; }
                        foreach (var x in xs)
                            t.AreEqual(1, ignoredD.DynamicInvoke(x));
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
            t.AreEqual(0, failures.Count, string.Join("\n", failures));
            t.IsTrue(compared > 30, "compared " + compared);
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

        public void Compound_assign_and_inc_dec_with_methods_are_the_same_as_system(TestContext t)
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
                ("CustomMultiplyAssign", t => MakeBinary(ExpressionType.MultiplyAssign, t, Constant(new ArMoney(5), t.Type), false, typeof(ParityTests).GetMethod(nameof(ArCustomMoney)))),
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
            t.AreEqual(0, failures.Count, string.Join("\n", failures));
            t.IsTrue(compared > 100, "compared " + compared);
        }
        public enum ConvLongEnum : long { A = 1, B = long.MaxValue }
        public enum ConvUIntEnum : uint { A = 1, B = uint.MaxValue }

        public void Convert_and_ConvertChecked_between_primitives_nullables_and_enums_are_the_same_as_system(TestContext t)
        {
            var types = new[]
            {
                typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(char), typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(ConvLongEnum), typeof(ConvUIntEnum)
            };
            var values = new object[] { 0, 1, 127, 128, 255, 256, -1, -128, 32767, 32768, 65535, 65536, int.MaxValue, int.MinValue, 2147483648L, 4294967295L, 4294967296L, long.MaxValue, long.MinValue, 9223372036854775808UL, ulong.MaxValue, 1.5, -1.5, 3e10 };

            var failures = new System.Collections.Generic.List<string>();
            var compared = 0;

            // the inputs do not depend on the target type, so convert them once per source type, via the unchecked System conversion to get the value in a normal shape
            var fromInputs = new System.Collections.Generic.Dictionary<Type, System.Collections.Generic.List<object>>();
            foreach (var from in types)
            {
                var list = fromInputs[from] = new System.Collections.Generic.List<object>();
                foreach (var v in values)
                    try
                    {
                        var cp = SysExpr.Parameter(typeof(object), "o");
                        list.Add(SysExpr.Lambda<Func<object, object>>(SysExpr.Convert(SysExpr.Convert(SysExpr.Convert(cp, v.GetType()), from), typeof(object)), cp).Compile()(v));
                    }
                    catch (Exception) { }
            }

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

                                var inputs = new System.Collections.Generic.List<object>(fromInputs[from]);
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
            t.AreEqual(0, failures.Count, failures.Count + " failures, e.g.\n" + string.Join("\n", failures.Take(15)));
            t.IsTrue(compared > 500, "compared " + compared);
        }
#if NET7_0_OR_GREATER
        public void Half_Int128_and_UInt128_constants_and_conversions_are_the_same_as_system(TestContext t)
        {
            var h = Parameter(typeof(Half), "h");
            var hc = Lambda<Func<Half, double>>(Convert(Add(h, Constant((Half)2.5)), typeof(double)), h);
            t.AreEqual(4.0, hc.CompileSys()((Half)1.5));
            t.AreEqual(4.0, hc.CompileFast(true)((Half)1.5));

            var i = Parameter(typeof(Int128), "i");
            var big = (Int128)long.MaxValue * 1000;
            var ic = Lambda<Func<Int128, Int128>>(Add(i, Constant(big)), i);
            t.AreEqual(big + 7, ic.CompileSys()(7));
            t.AreEqual(big + 7, ic.CompileFast(true)(7));

            var u = Parameter(typeof(long), "u");
            var uc = Lambda<Func<long, UInt128>>(Add(Convert(u, typeof(UInt128)), Constant((UInt128)ulong.MaxValue)), u);
            t.AreEqual((UInt128)ulong.MaxValue + 5, uc.CompileSys()(5));
            t.AreEqual((UInt128)ulong.MaxValue + 5, uc.CompileFast(true)(5));
        }
#endif

        public class IncDecHolder<T>
        {
            public T Field;
            public T[] Array = new T[1];
        }

        public void Inc_dec_assign_of_primitives_and_nullables_are_the_same_as_system(TestContext t)
        {
            var types = new[]
            {
                typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(decimal), typeof(char)
            };
            var nodes = new[]
            {
                ExpressionType.PreIncrementAssign, ExpressionType.PreDecrementAssign,
                ExpressionType.PostIncrementAssign, ExpressionType.PostDecrementAssign
            };
            var values = new object[] { 0, 1, 2, 127, 128, 255, 32767, 65535, int.MaxValue, long.MaxValue, 1.5, -1 };

            var failures = new System.Collections.Generic.List<string>();
            var compared = 0;
            foreach (var type in types)
                foreach (var nullable in new[] { false, true })
                    foreach (var node in nodes)
                        foreach (var target in new[] { "param", "field", "array" })
                        {
                            var ty = nullable ? typeof(Nullable<>).MakeGenericType(type) : type;
                            var inputs = new System.Collections.Generic.List<object>();
                            foreach (var v in values)
                                try { inputs.Add(System.Convert.ChangeType(v, type)); } catch (Exception) { }
                            if (nullable) inputs.Add(null);

                            var sysResults = RunIncDec(ty, node, target, inputs, false);
                            var fastResults = RunIncDec(ty, node, target, inputs, true);
                            for (var i = 0; i < inputs.Count; ++i)
                            {
                                var x = inputs[i];
                                var e = sysResults[i];
                                var a = fastResults[i];
                                ++compared;
                                if (!Equals(e, a))
                                {
                                    failures.Add($"{node} {target} {ty.Name}({x}) sys={e} fec={a}");
                                    break;
                                }
                            }
                        }

            t.AreEqual(0, failures.Count, failures.Count + " failures, e.g.\n" + string.Join("\n", failures.Take(15)));
            t.IsTrue(compared > 1000, "compared " + compared);
        }

        // Compiles once and returns "operation result;target value after" per input, to compare both the result of the expression and the changed target
        private static object[] RunIncDec(Type t, ExpressionType node, string target, System.Collections.Generic.List<object> inputs, bool fast)
        {
            var holderType = typeof(IncDecHolder<>).MakeGenericType(t);
            var holderParam = SysExpr.Parameter(holderType, "h");
            var param = SysExpr.Parameter(t, "p");
            var field = holderType.GetField("Field");
            var array = holderType.GetField("Array");

            SysExpr targetExpr = target switch
            {
                "param" => param,
                "field" => SysExpr.Field(holderParam, field),
                _ => SysExpr.ArrayAccess(SysExpr.Field(holderParam, array), SysExpr.Constant(0))
            };

            var results = new object[inputs.Count];
            Delegate d;
            try
            {
                var inc = node switch
                {
                    ExpressionType.PreIncrementAssign => SysExpr.PreIncrementAssign(targetExpr),
                    ExpressionType.PreDecrementAssign => SysExpr.PreDecrementAssign(targetExpr),
                    ExpressionType.PostIncrementAssign => SysExpr.PostIncrementAssign(targetExpr),
                    _ => SysExpr.PostDecrementAssign(targetExpr)
                };

                var body = SysExpr.NewArrayInit(typeof(object),
                    SysExpr.Convert(inc, typeof(object)),
                    SysExpr.Convert(targetExpr, typeof(object)));

                var lambda = SysExpr.Lambda(typeof(Func<,,>).MakeGenericType(holderType, t, typeof(object[])), body, holderParam, param);
#if LIGHT_EXPRESSION
                var fastLambda = lambda.ToLightExpression();
#else
                var fastLambda = lambda;
#endif
                d = fast ? fastLambda.CompileFast(true) : lambda.Compile();
            }
            catch (Exception ex) // e.g. the operator is not defined for the type, so every input gives the same exception
            {
                for (var i = 0; i < results.Length; ++i)
                    results[i] = "ex:" + ex.GetType().Name;
                return results;
            }
            for (var i = 0; i < results.Length; ++i)
            {
                var x = inputs[i];
                try
                {
                    var holder = Activator.CreateInstance(holderType);
                    if (target == "field") field.SetValue(holder, x);
                    else if (target == "array") ((Array)array.GetValue(holder)).SetValue(x, 0);
                    var arr = (object[])d.DynamicInvoke(holder, x);
                    results[i] = arr[0] + ";" + arr[1];
                }
                catch (Exception ex) { results[i] = "ex:" + ex.GetType().Name; }
            }
            return results;
        }

        public void Inc_dec_assign_of_block_variable_compared_and_returned_via_label_are_the_same_as_system(TestContext t)
        {
            var types = new[]
            {
                typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(decimal)
            };
            var nodes = new[]
            {
                ExpressionType.PreIncrementAssign, ExpressionType.PreDecrementAssign,
                ExpressionType.PostIncrementAssign, ExpressionType.PostDecrementAssign
            };

            var failures = new System.Collections.Generic.List<string>();
            var compared = 0;
            foreach (var type in types)
                foreach (var nullable in new[] { false, true })
                    foreach (var node in nodes)
                        foreach (var withLabel in new[] { false, true })
                            foreach (var v in new object[] { 1, 5, null })
                            {
                                if (v == null & !nullable)
                                    continue;

                                var ty = nullable ? typeof(Nullable<>).MakeGenericType(type) : type;
                                var value = v == null ? null : System.Convert.ChangeType(v, type);
                                var isPre = node is ExpressionType.PreIncrementAssign or ExpressionType.PreDecrementAssign;
                                var isInc = node is ExpressionType.PreIncrementAssign or ExpressionType.PostIncrementAssign;
                                var changed = value == null ? null : System.Convert.ChangeType((int)v + (isInc ? 1 : -1), type);
                                var expected = withLabel | isPre ? changed : value; // the block result is the changed value, except for the Post operations

                                var variable = Variable(ty);
                                var inc = node switch
                                {
                                    ExpressionType.PreIncrementAssign => PreIncrementAssign(variable),
                                    ExpressionType.PreDecrementAssign => PreDecrementAssign(variable),
                                    ExpressionType.PostIncrementAssign => PostIncrementAssign(variable),
                                    _ => PostDecrementAssign(variable)
                                };
                                var label = Label(ty);
                                var block = withLabel
                                    ? Block(new[] { variable }, Assign(variable, Constant(value, ty)), inc, Return(label, variable), Label(label, Default(ty)))
                                    : Block(new[] { variable }, Assign(variable, Constant(value, ty)), inc);
                                var lambda = Lambda<Func<bool>>(Equal(Constant(expected, ty), block));

                                bool e, a;
                                try { e = lambda.CompileSys()(); } catch (Exception ex) { failures.Add($"{node} {ty.Name}({v}) sys threw {ex.GetType().Name}"); continue; }
                                try { a = lambda.CompileFast(true)(); } catch (Exception ex) { a = false; failures.Add($"{node} {(withLabel ? "label " : "")}{ty.Name}({v}) fec threw {ex.GetType().Name}: {ex.Message}"); continue; }
                                ++compared;
                                if (e != a)
                                    failures.Add($"{node} {(withLabel ? "label " : "")}{ty.Name}({v}) sys={e} fec={a}");
                            }

            t.AreEqual(0, failures.Count, failures.Count + " failures, e.g.\n" + string.Join("\n", failures.Take(15)));
            t.IsTrue(compared > 200, "compared " + compared);
        }

        public void Arithmetic_on_nullable_by_ref_parameter_is_the_same_as_system(TestContext t)
        {
            var p = Parameter(typeof(int?).MakeByRefType(), "p");
            foreach (var body in new Expression[] { Add(p, p), Add(Constant(1, typeof(int?)), p), Add(p, Constant(1, typeof(int?))) })
            {
                var lambda = Lambda<RefConvFunc<int?, int?>>(body, p);
                var sys = lambda.CompileSys();
                var fec = lambda.CompileFast(true);
                foreach (var value in new int?[] { 5, null })
                {
                    var a = value;
                    var b = value;
                    t.AreEqual(sys(ref a), fec(ref b));
                }
            }
        }
        public void Coalesce_with_the_conversion_and_widening_to_decimal_is_the_same_as_system(TestContext t)
        {
            var i = Parameter(typeof(int?), "i");
            var x = Parameter(typeof(int?), "x");
            var n = Parameter(typeof(int), "n");
            var s = Parameter(typeof(string), "s");
            var y = Parameter(typeof(string), "y");
            var lambdas = new LambdaExpression[]
            {
                Lambda<Func<int?, int?>>(Coalesce(i, Constant(5, typeof(int?)), Lambda<Func<int?, int?>>(Multiply(x, Constant(2, typeof(int?))), x)), i),
                Lambda<Func<int?, int>>(Coalesce(i, Constant(5), Lambda<Func<int, int>>(Multiply(n, Constant(2)), n)), i),
                Lambda<Func<string, string>>(Coalesce(s, Constant("YY"), Lambda<Func<string, string>>(Call(typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) }), y, Constant("def")), y)), s),
#if NET7_0_OR_GREATER // System on net472 fails to compile the coalesce widening to decimal
                Lambda<Func<int?, decimal>>(Coalesce(i, Constant(7m)), i),
                Lambda<Func<int?, decimal?>>(Coalesce(i, Constant(7m, typeof(decimal?))), i),
#endif
            };
            foreach (var lambda in lambdas)
            {
                var sys = lambda.CompileSys();
                var fec = lambda.CompileFast(true);
                foreach (var arg in lambda.Parameters[0].Type == typeof(string) ? new object[] { null, "abc" } : new object[] { null, 5 })
                    t.AreEqual(sys.DynamicInvoke(arg), fec.DynamicInvoke(arg));
            }
        }
        public void Goto_with_the_value_to_the_void_label_is_the_same_as_system(TestContext t)
        {
            foreach (var kind in new[] { GotoExpressionKind.Goto, GotoExpressionKind.Break, GotoExpressionKind.Continue, GotoExpressionKind.Return })
                foreach (var value in new object[] { 42, "Hello", new DateTime(2020, 1, 1) })
                {
                    var target = Label();
                    var lambda = Lambda<Action>(Block(MakeGoto(kind, target, Constant(value), typeof(void)), Label(target)));
                    lambda.CompileSys()();
                    lambda.CompileFast(true)(); // the value is dropped and the IL stays valid
                }
        }
        public void Goto_and_return_with_the_value_to_the_label_in_the_middle_of_block_are_the_same_as_system(TestContext t)
        {
            foreach (var kind in new[] { GotoExpressionKind.Goto, GotoExpressionKind.Return })
            {
                var target = Label(typeof(int));
                var toMiddle = Lambda<Func<int>>(Block(MakeGoto(kind, target, Constant(1), typeof(void)), Label(target, Constant(2)), Constant(3)));
                t.AreEqual(toMiddle.CompileSys()(), toMiddle.CompileFast(true)());

                var target2 = Label(typeof(int));
                var toEnd = Lambda<Func<int>>(Block(MakeGoto(kind, target2, Constant(7), typeof(void)), Label(target2, Constant(0))));
                t.AreEqual(toEnd.CompileSys()(), toEnd.CompileFast(true)());
            }
        }
        public void Label_rules_are_the_same_as_system(TestContext t)
        {
            // the same label in the separate blocks is fine
            var target = Label(typeof(int));
            Expression Part(int v) => Block(Goto(target, Constant(v)), Throw(Constant(new Exception())), Label(target, Constant(0)));
            var multi = Lambda<Func<int>>(Add(Add(Part(6), Part(4)), Part(5)));
            t.AreEqual(multi.CompileSys()(), multi.CompileFast(true)());

            var label = Label();
            var shapes = new Expression[]
            {
                Block(Goto(label), Block(Label(label)), Block(Label(label))), // ambiguous jump
                Block(Block(Label(label)), Block(Block(Goto(label))), Block(Label(label))), // ambiguous jump split
                Block(Goto(label), TypeIs(Label(label), typeof(int))), // jump into the expression
                Block(Label(label), Loop(Empty(), label)), // the loop redefines the label
            };
            foreach (var shape in shapes)
            {
                var lambda = Lambda<Action>(shape);
                var sysThrows = false;
                try { lambda.CompileSys(); } catch (InvalidOperationException) { sysThrows = true; }
                var fecThrows = false;
                try { lambda.CompileFast(true); } catch (InvalidOperationException) { fecThrows = true; }
                t.IsTrue(sysThrows);
                t.IsTrue(fecThrows);
            }
        }
        public void Switch_with_duplicate_test_values_and_void_type_are_the_same_as_system(TestContext t)
        {
            var p = Parameter(typeof(int));
            var dup = Lambda<Func<int, string>>(Switch(p, Constant("d"),
                SwitchCase(Constant("a"), Constant(1)), SwitchCase(Constant("b"), Constant(2)),
                SwitchCase(Constant("c"), Constant(1), Constant(2)), SwitchCase(Constant("e"), Constant(3))), p);
            var sys = dup.CompileSys();
            var fec = dup.CompileFast(true);
            for (var i = 0; i < 5; ++i)
                t.AreEqual(sys(i), fec(i));

            // System on older runtimes rejects the void switch with the non-void bodies, so check FEC alone: a stack leak would make the IL invalid
            var voidSwitch = Lambda<Action>(Switch(typeof(void), Constant(DateTime.MinValue), Constant("Def"), null,
                SwitchCase(Constant("Foo"), Constant(DateTime.MinValue)), SwitchCase(Constant("Bar"), Constant(DateTime.MaxValue))));
            voidSwitch.CompileFast(true)();
        }
        public void Label_and_rethrow_validation_walks_all_node_kinds_the_same_as_system(TestContext t)
        {
            var v = Label(typeof(void));
            var jumpV = Goto(v);
            var hereV = Label(v);
            var i = Label(typeof(int));
            var jumpI = Goto(i, Constant(1));
            var hereI = Label(i, Constant(0));
            var s = Label(typeof(string));
            var jumpS = Goto(s, Constant("a"));
            var hereS = Label(s, Constant("b"));
            var b = Label(typeof(bool));
            var jumpB = Goto(b, Constant(true));
            var hereB = Label(b, Constant(true));
            var other = Label(typeof(int));
            var brk = Label(typeof(int));
            var toStr = typeof(int).GetMethod("ToString", Type.EmptyTypes);
            var plusOne = typeof(ParityTests).GetMethod(nameof(AddPlusOne));
            var ts3 = typeof(TimeSpan).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
            var one = Constant(1);
            var exc = typeof(Exception);
            var rethrow = Rethrow(typeof(void));
            var cond = Equal(Call(plusOne, one, one), Constant(3)); // not folded, so no branch is eliminated together with its labels

            // a jump goes first, then the label (or the rethrow) hidden at the position in the walked node kind
            var shapes = new Expression[]
            {
                Block(typeof(void), jumpV, Block(hereV, Empty())), // the label in the nested block
                Block(typeof(void), jumpV, hereV, Empty()),
                Block(typeof(void), jumpV, Block(Empty(), hereV)),
                Block(typeof(void), jumpV, Block(Empty(), Block(hereV, Empty()), Empty())),
                Block(typeof(void), jumpV, Block(hereV, Block(hereV, Empty()))),
                Block(typeof(void), jumpV, Loop(hereV)),
                Block(typeof(void), jumpV, Loop(Empty(), v)),
                Block(typeof(void), jumpV, Loop(Empty(), Label(), v)),
                Block(typeof(void), Goto(v), Loop(Block(Empty(), Empty()), Label(), Label())),
                Block(typeof(void), jumpV, IfThen(cond, hereV)),
                Block(typeof(void), jumpV, IfThenElse(cond, hereV, hereV)),
                Block(typeof(void), jumpB, IfThen(hereB, Empty())),
                Block(typeof(void), jumpV, Switch(one, hereV, SwitchCase(Empty(), one))),
                Block(typeof(void), jumpV, Switch(one, Empty(), SwitchCase(hereV, one))),
                Block(typeof(void), jumpI, Switch(hereI, Empty(), SwitchCase(Empty(), one))),
                Block(typeof(void), jumpV, TryCatch(hereV, Catch(exc, Empty()))),
                Block(typeof(void), jumpV, TryCatch(Empty(), Catch(exc, hereV))),
#if !NETFRAMEWORK // DynamicMethod on .NET Framework does not support the filters
                Block(typeof(void), jumpB, TryCatch(Empty(), MakeCatchBlock(exc, null, Empty(), hereB))),
#endif
                Block(typeof(void), jumpV, TryFinally(Empty(), hereV)),
#if !NETFRAMEWORK
                Block(typeof(void), jumpV, TryFault(Empty(), hereV)),
#endif
                Block(typeof(void), jumpI, Add(hereI, one)),
                Block(typeof(void), jumpI, Add(one, hereI)),
                Block(typeof(void), jumpI, Negate(hereI)),
                Block(typeof(void), jumpI, TypeIs(hereI, typeof(int))),
                Block(typeof(void), jumpS, Property(hereS, "Length")),
                Block(typeof(void), jumpI, Call(hereI, toStr)),
                Block(typeof(void), jumpI, Call(plusOne, hereI, one)),
                Block(typeof(void), jumpI, Call(plusOne, one, hereI)),
                Block(typeof(void), jumpI, New(ts3, hereI, one, one)),
                Block(typeof(void), jumpI, New(ts3, one, one, hereI)),
                Block(typeof(void), jumpI, Label(other, hereI)),
                Block(typeof(void), jumpI, Goto(other, hereI)),
                Block(typeof(void), jumpI, Label(other, Constant(0)), Block(hereI, hereI)),
                Block(typeof(void), jumpI, Block(hereI, Empty())), // the label with the value can't be entered
                Block(typeof(void), jumpI, hereI, Empty()),

                // the void label in the block, which is the part of the expression, can't be entered
                Block(typeof(void), jumpV, Call(plusOne, Block(typeof(int), hereV, one), one)),
                Block(typeof(void), jumpV, Goto(other, Block(typeof(int), hereV, one))),
                Block(typeof(void), jumpV, Add(Loop(Block(typeof(void), hereV, Break(brk, one)), brk), one)),
                Block(typeof(void), jumpV, Add(Condition(cond, Block(typeof(int), hereV, one), one), one)),
                Block(typeof(void), jumpV, Add(Switch(one, one, SwitchCase(Block(typeof(int), hereV, one), one)), one)),
                Block(typeof(void), jumpV, Add(Switch(Call(plusOne, one, one), Block(typeof(int), hereV, one), SwitchCase(one, one)), one)),

                // the jump from the nested block to the label in the expression of the outer one
                Block(typeof(void), Add(hereI, one), Block(jumpI)),
                Block(typeof(void), Block(jumpI), Add(hereI, one)),

                // the same label twice: the nested one is defined first or last
                Block(typeof(void), Block(hereV), hereV),
                Block(typeof(void), hereV, Block(hereV)),
                Block(typeof(void), Block(hereV), Block(hereV)), // fine, the separate scopes
                Block(typeof(void), Loop(Block(hereV), v)),

                // jumps out of the finally, the fault and the catch
                Block(typeof(void), TryFinally(Empty(), jumpV), hereV),
                Block(typeof(void), TryFinally(Empty(), Block(Empty(), jumpV)), hereV),
#if !NETFRAMEWORK
                Block(typeof(void), TryFault(Empty(), jumpV), hereV),
#endif
                Block(typeof(void), TryCatch(jumpV, Catch(exc, Empty())), hereV),
                Block(typeof(void), TryCatch(Empty(), Catch(exc, jumpV)), hereV),
                Block(typeof(void), TryFinally(Block(jumpV, hereV), Empty())),
                TryFinally(Empty(), Block(jumpV, hereV)), // inside of the finally
                TryFinally(Empty(), Block(Block(jumpV), hereV)),
                TryFinally(Empty(), Block(typeof(void), TryCatch(Empty(), Catch(exc, jumpV)), hereV)), // from the catch within the finally
                Block(typeof(void), TryFinally(Empty(), TryFinally(Empty(), jumpV)), hereV),

                // the rethrow
                rethrow,
                TryCatch(Empty(), Catch(exc, rethrow)),
                TryCatch(Empty(), Catch(exc, Block(typeof(void), Empty(), rethrow))),
                TryCatch(Empty(), Catch(exc, TryCatch(rethrow, Catch(exc, Empty())))),
                TryCatch(Empty(), Catch(exc, TryCatch(Empty(), Catch(exc, rethrow)))),
                TryCatch(rethrow, Catch(exc, Empty())),
#if !NETFRAMEWORK // DynamicMethod on .NET Framework does not support the filters
                TryCatch(Empty(), MakeCatchBlock(exc, null, Empty(), Block(Constant(true)))),
#endif
                TryFinally(rethrow, Empty()),
                TryFinally(Empty(), rethrow),
#if !NETFRAMEWORK
                TryFault(Empty(), rethrow),
#endif
                TryCatch(Empty(), Catch(exc, TryFinally(Empty(), rethrow))),
#if !NETFRAMEWORK
                TryCatch(Empty(), Catch(exc, TryFault(Empty(), rethrow))),
#endif
                TryFinally(Empty(), TryCatch(Empty(), Catch(exc, rethrow))), // the catch within the finally is fine
                TryCatch(Empty(), Catch(exc, Block(TryFinally(Empty(), Empty()), rethrow))),
                Block(typeof(void), Call(plusOne, Block(typeof(int), rethrow, one), one)),
                Block(typeof(void), Convert(Block(typeof(int), rethrow, one), typeof(long))),
            };

            var sysThrowCount = 0;
            var emitLimitCount = 0;
            for (var n = 0; n < shapes.Length; ++n)
            {
                var lambda = Lambda<Action>(shapes[n]);
                var sysThrows = false;
                try { lambda.CompileSys(); } catch (InvalidOperationException) { sysThrows = true; }
                var fecThrows = false;
                try { lambda.CompileFast(true); } catch (InvalidOperationException) { fecThrows = true; }
                catch (ArgumentException ex) { throw new Exception($"Shape #{n} (validated): {ex.Message}: {shapes[n]}"); }
                if (sysThrows != fecThrows)
                {
                    // FEC can't emit some of the valid jumps (e.g. into the switch default) even without the validation, it is not the validation problem
                    var emitThrows = false;
                    try { lambda.CompileFast(true); } catch (InvalidOperationException) { emitThrows = true; } catch (ArgumentException) { emitThrows = true; }
                    if (!(emitThrows & !sysThrows))
                        throw new Exception($"Shape #{n}: System throws={sysThrows} but FEC throws={fecThrows}: {shapes[n]}");
                    ++emitLimitCount;
                    continue;
                }
                if (sysThrows)
                    ++sysThrowCount;
            }
            t.IsTrue(sysThrowCount > 20 & sysThrowCount + emitLimitCount < shapes.Length);

            // FEC can't emit these valid-in-System jumps into the goto value and the switch default, so the validation message is what tells that the labels are walked
            var strict = new Expression[]
            {
                Block(typeof(void), jumpV, Goto(other, Block(typeof(int), hereV, one))),
                Block(typeof(void), jumpV, Add(Switch(Call(plusOne, one, one), Block(typeof(int), hereV, one), SwitchCase(one, one)), one)),
            };
            foreach (var shape in strict)
            {
                var throws = false;
                try { Lambda<Action>(shape).CompileFast(true); } catch (InvalidOperationException ex) { throws = ex.Message.Contains("Control cannot enter"); }
                t.IsTrue(throws);
            }        }
        public void Catch_filter_non_exception_catch_and_rethrow_rules_are_the_same_as_system(TestContext t)
        {
            var p = Parameter(typeof(int));
            var ex = Parameter(typeof(Exception));
            var thrower = Block(typeof(string), Throw(New(typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes))), Constant("x"));

#if !NETFRAMEWORK // DynamicMethod on .NET Framework does not support the filters
            // the false filter falls through to the next handler, the filter assignment to the variable is seen by the handler
            var withFilter = Lambda<Func<int, string>>(TryCatch(thrower,
                MakeCatchBlock(typeof(Exception), ex, Constant("first"), GreaterThan(p, Constant(0))),
                Catch(typeof(Exception), Constant("second"))), p);
            var sys = withFilter.CompileSys();
            var fec = withFilter.CompileFast(true);
            for (var i = -1; i < 2; ++i)
                t.AreEqual(sys(i), fec(i));
#endif

            var str = Parameter(typeof(string));
#if !NETFRAMEWORK
            var nonException = Lambda<Func<string>>(TryCatch(Block(typeof(string), Throw(Constant("Hello")), Constant("x")),
                Catch(str, str)));
            t.AreEqual(nonException.CompileSys()(), nonException.CompileFast(true)());
#endif

            var shapes = new Expression[]
            {
                Rethrow(typeof(void)), // outside of the catch
                TryCatch(Empty(), Catch(typeof(Exception), TryFinally(Empty(), Rethrow(typeof(void))))), // in the finally within the catch
                Block(Label(Label()), TryFinally(Empty(), Goto(Label()))), // a jump out of the finally
            };
            foreach (var shape in shapes)
            {
                var lambda = Lambda<Action>(shape);
                var sysThrows = false;
                try { lambda.CompileSys(); } catch (InvalidOperationException) { sysThrows = true; }
                var fecThrows = false;
                try { lambda.CompileFast(true); } catch (InvalidOperationException) { fecThrows = true; }
                t.IsTrue(sysThrows);
                t.IsTrue(fecThrows);
            }
        }
        public class BindInner { public int X; public List<int> Items = new List<int>(); public HashSet<int> Set = new HashSet<int>(); public static int S; }
        public class BindOuter { public BindInner Inner = new BindInner(); public BindInner Prop { get; } = new BindInner(); }

        public void Member_and_list_bindings_are_the_same_as_system(TestContext t)
        {
            var inner = typeof(BindOuter).GetField(nameof(BindOuter.Inner));
            var prop = typeof(BindOuter).GetProperty(nameof(BindOuter.Prop));
            var x = typeof(BindInner).GetField(nameof(BindInner.X));
            var items = typeof(BindInner).GetField(nameof(BindInner.Items));
            var s = typeof(BindInner).GetField(nameof(BindInner.S));
            var add = typeof(List<int>).GetMethod(nameof(List<int>.Add));
            var set = typeof(BindInner).GetField(nameof(BindInner.Set));
            var setAdd = typeof(HashSet<int>).GetMethod(nameof(HashSet<int>.Add));

            // the nested member and list bindings on the field and on the readonly property, and the static member
            var e = Lambda<Func<BindOuter>>(MemberInit(New(typeof(BindOuter)),
                MemberBind(inner, Bind(x, Constant(5)), ListBind(items, ElementInit(add, Constant(7)), ElementInit(add, Constant(8))), ListBind(set, ElementInit(setAdd, Constant(4)))),
                MemberBind(prop, Bind(x, Constant(3)), Bind(s, Constant(9)))));

            var sys = e.CompileSys()();
            var fast = e.CompileFast(true)();
            t.AreEqual(sys.Inner.X, fast.Inner.X);
            t.AreEqual(5, fast.Inner.X);
            t.AreEqual(sys.Prop.X, fast.Prop.X);
            t.AreEqual(3, fast.Prop.X);
            t.AreEqual(sys.Inner.Items.Count, fast.Inner.Items.Count);
            t.AreEqual(8, fast.Inner.Items[1]);
            t.AreEqual(sys.Inner.Set.Count, fast.Inner.Set.Count);
            t.AreEqual(9, BindInner.S);
        }
        public struct BindStruct { public int X; public List<int> Items; }
        public class BindStructOwner { public BindStruct Field; BindStruct _p; public BindStruct Prop { get => _p; set => _p = value; } }

        public void Member_bindings_on_the_struct_property_and_inside_try_are_the_same_as_system(TestContext t)
        {
            var x = typeof(BindStruct).GetField(nameof(BindStruct.X));
            var field = typeof(BindStructOwner).GetField(nameof(BindStructOwner.Field));
            var prop = typeof(BindStructOwner).GetProperty(nameof(BindStructOwner.Prop));

            // System refuses the struct property, we read the copy, change it and write it back
            var init = MemberInit(New(typeof(BindStructOwner)),
                MemberBind(field, Bind(x, Constant(1))),
                MemberBind(prop, Bind(x, Constant(2))));

            var e = Lambda<Func<BindStructOwner>>(init);
            var fast = e.CompileFast(true)();
            t.AreEqual(1, fast.Field.X);
            t.AreEqual(2, fast.Prop.X);

            // the member init as the call argument inside the try, so the IL stack is not empty at the try
            var pick = typeof(ParityTests).GetMethod(nameof(PickX));
            var ty = Lambda<Func<int>>(Call(pick, TryCatch(Field(Field(MemberInit(New(typeof(BindStructOwner)), MemberBind(field, Bind(x, Constant(1)))), field), x), Catch(typeof(Exception), Constant(-1)))));
            t.AreEqual(ty.CompileSys()(), ty.CompileFast(true)());
        }

        public static int PickX(int a) => a + 1;
        public void Reference_equality_of_the_boxed_values_and_equal_objects_is_the_same_as_system(TestContext t)
        {
            object boxed = 42;
            var s1 = new Uri("http://example.net/");
            var s2 = new Uri("http://example.net/");
            var cases = new[]
            {
                // the same box typed via the interface and the object, used twice so the constant is shared
                Lambda<Func<bool>>(ReferenceEqual(Constant(boxed, typeof(IComparable)), Constant(boxed, typeof(IComparable)))),
                Lambda<Func<bool>>(ReferenceEqual(Constant(boxed, typeof(object)), Constant(boxed, typeof(IComparable)))),
                Lambda<Func<bool>>(ReferenceNotEqual(Constant(boxed, typeof(IComparable)), Constant(boxed, typeof(object)))),
                // the different boxes of the equal values
                Lambda<Func<bool>>(ReferenceEqual(Constant(boxed, typeof(IComparable)), Constant((object)42, typeof(IComparable)))),
                // the equal objects with the operator ==, which the reference comparison should not call
                Lambda<Func<bool>>(ReferenceEqual(Constant(s1), Constant(s2))),
                Lambda<Func<bool>>(ReferenceNotEqual(Constant(s1), Constant(s2))),
                Lambda<Func<bool>>(ReferenceEqual(Constant(s1), Constant(s1))),
            };
            foreach (var c in cases)
            {
                var sys = c.CompileSys()();
                t.AreEqual(sys, c.CompileFast(true)());
            }
        }
        public void Type_is_and_equal_with_the_void(TestContext t)
        {
            var voidBlock = Block(typeof(void), Constant(1));
            var cases = new[]
            {
                Lambda<Func<bool>>(TypeIs(voidBlock, typeof(void))),
                Lambda<Func<bool>>(TypeIs(voidBlock, typeof(object))),
                Lambda<Func<bool>>(TypeIs(Constant(1), typeof(void))),
                Lambda<Func<bool>>(TypeEqual(voidBlock, typeof(void))),
                Lambda<Func<bool>>(TypeEqual(Constant("a"), typeof(void))),
            };
            // the expected values are fixed because the .NET Framework System answers false for the void TypeIs void
            var expected = new[] { true, false, false, true, false };
            for (var i = 0; i < cases.Length; i++)
                t.AreEqual(expected[i], cases[i].CompileFast(true)());
        }
        public static int AddPlusOne(int a, int b) => a + b + 1;
        public class IntBox { public int Value = 25; public int[] Arr = { 25 }; }

        public void Compound_assign_with_the_method_and_conversion_lambda_is_the_same_as_system(TestContext t)
        {
            var x = Parameter(typeof(int), "x");
            var doubleIt = Lambda<Func<int, int>>(Multiply(x, Constant(2)), x);
            var method = GetType().GetMethod(nameof(AddPlusOne));
            var box = Parameter(typeof(IntBox), "box");
            foreach (var target in new Expression[] { Field(box, nameof(IntBox.Value)), ArrayAccess(Field(box, nameof(IntBox.Arr)), Constant(0)) })
            {
                var lambda = Lambda<Func<IntBox, int>>(MakeBinary(ExpressionType.AddAssign, target, Constant(15), false, method, doubleIt), box);
                var sysBox = new IntBox();
                var fecBox = new IntBox();
                t.AreEqual(lambda.CompileSys()(sysBox), lambda.CompileFast(true)(fecBox));
                t.AreEqual(sysBox.Value, fecBox.Value);
                t.AreEqual(sysBox.Arr[0], fecBox.Arr[0]);
            }
        }
        public delegate T RefConvFunc<T, R>(ref R r);
        public class RefConv
        {
            public int Value;
            public static RefConv FromRef(ref int x) => new RefConv { Value = x++ };
        }

        public void Convert_with_the_method_taking_ref_param_is_the_same_as_system(TestContext t)
        {
            var method = typeof(RefConv).GetMethod(nameof(RefConv.FromRef));

            var p = Parameter(typeof(int).MakeByRefType(), "p");
            var e = Lambda<RefConvFunc<RefConv, int>>(Convert(p, typeof(RefConv), method), p);
            int x = 5;
            t.AreEqual(5, e.CompileFast(true)(ref x).Value);
            t.AreEqual(6, x);

            var np = Parameter(typeof(int?).MakeByRefType(), "p");
            var ne = Lambda<RefConvFunc<RefConv, int?>>(Convert(np, typeof(RefConv), method), np);
            int? nx = 5;
            t.AreEqual(5, ne.CompileFast(true)(ref nx).Value);
            t.AreEqual(5, nx); // the refness is lost on lifting, same as in System
        }

        public void Negate_checked_of_short_emits_the_short_IL(TestContext t)
        {
            var s = Parameter(typeof(short), "s"); var ns = Parameter(typeof(short?), "s");
            Lambda<Func<short, short>>(NegateChecked(s), s).CompileFast(true).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Neg, OpCodes.Conv_Ovf_I2, OpCodes.Ret);
            Lambda<Func<short?, short?>>(NegateChecked(ns), ns).CompileFast(true).AssertOpCodes(
                OpCodes.Ldarg_1, OpCodes.Stloc_0, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Brfalse,
                OpCodes.Ldloca_S, OpCodes.Ldfld, OpCodes.Neg, OpCodes.Conv_Ovf_I2, OpCodes.Newobj, OpCodes.Br,
                OpCodes.Ldloca_S, OpCodes.Initobj, OpCodes.Ldloc_1, OpCodes.Ret);
        }
        public void Lifted_comparisons_have_the_same_type_and_result_as_system(TestContext t)
        {
            var ip = Parameter(typeof(int), "a"); var iq = Parameter(typeof(int), "b");
            var np = Parameter(typeof(int?), "a"); var nq = Parameter(typeof(int?), "b");

            var eqInt = Equal(ip, iq, true, null);
            t.AreEqual(typeof(bool), eqInt.Type);
            t.AreEqual(true, Lambda<Func<int, int, bool>>(eqInt, ip, iq).CompileFast(true)(1, 1));
            t.AreEqual(true, Lambda<Func<int, int, bool>>(LessThan(ip, iq, true, null), ip, iq).CompileFast(true)(1, 2));

            // lifted to null: the result is null when any operand is null, so null == null is null and not true
            var eqNull = Equal(np, nq, true, null);
            t.AreEqual(typeof(bool?), eqNull.Type);
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
                t.IsNotNull(ff);
                foreach (var x in values)
                    foreach (var y in values)
                        t.AreEqual(fs(x, y), ff(x, y));

                // the right operand is a complex expression
                var fc = Lambda<Func<int?, int?, bool?>>(liftedOps[i](np, Block(nq), true, null), np, nq).CompileFast(true);
                t.IsNotNull(fc);
                foreach (var x in values)
                    foreach (var y in values)
                        t.AreEqual(fs(x, y), fc(x, y));
            }

            // all comparison factories, with and without lifting, for the nullable and non-nullable operands
            var names = new[] { "Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual" };
            foreach (var name in names)
                foreach (var ty in new[] { typeof(int), typeof(int?), typeof(bool), typeof(bool?), typeof(double?) })
                    foreach (var lift in new[] { false, true })
                    {
                        System.Linq.Expressions.BinaryExpression s = null;
                        try
                        {
                            s = (System.Linq.Expressions.BinaryExpression)typeof(SysExpr).GetMethod(name, new[] { typeof(SysExpr), typeof(SysExpr), typeof(bool), typeof(System.Reflection.MethodInfo) })
                                .Invoke(null, new object[] { SysExpr.Parameter(ty), SysExpr.Parameter(ty), lift, null });
                        }
                        catch (System.Reflection.TargetInvocationException) { }
                        if (s == null)
                            continue;

                        var l = (BinaryExpression)typeof(Expression).GetMethod(name, new[] { typeof(Expression), typeof(Expression), typeof(bool), typeof(System.Reflection.MethodInfo) })
                            .Invoke(null, new object[] { Parameter(ty), Parameter(ty), lift, null });
                        var what = $"{name}({ty.Name}, lift: {lift})";
                        t.AreEqual(s.Type, l.Type, what + " Type");
                        t.AreEqual(s.IsLifted, l.IsLifted, what + " IsLifted");
                        t.AreEqual(s.IsLiftedToNull, l.IsLiftedToNull, what + " IsLiftedToNull");
                    }
        }
        public void IsLifted_and_IsLiftedToNull_are_the_same_as_in_system_expressions(TestContext t)
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
                        t.AreEqual(s.Type, l.Type, what + " Type");
                        t.AreEqual(s.IsLifted, l.IsLifted, what + " IsLifted");
                        t.AreEqual(s.IsLiftedToNull, l.IsLiftedToNull, what + " IsLiftedToNull");
                        ++checkedCount;
                    }
            // the number of the combinations accepted by System differs slightly between the runtimes, so this is just a sanity check
            t.IsTrue(checkedCount > 50);
        }

        public void Can_compile_type_is_and_type_equal_the_same_as_system(TestContext t)
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
                foreach (var ty in typeOperands)
                    foreach (var (operandParam, values) in operands)
                    {
                        var p = Parameter(operandParam.Type, "p"); // not shared with the outer lambda parameter
                        var body = isEqual ? TypeEqual(p, ty) : TypeIs(p, ty);
                        t.AreEqual(isEqual ? ExpressionType.TypeEqual : ExpressionType.TypeIs, body.NodeType);
                        t.AreEqual(typeof(bool), body.Type);

                        var lambda = Lambda<Func<object, bool>>(Invoke(Lambda(body, p), Convert(o, p.Type)), o);
                        var what = $"{(isEqual ? "TypeEqual" : "TypeIs")}({p.Type.Name}, {ty.Name})";
                        var fast = lambda.CompileFast(true);
                        t.IsNotNull(fast, what);
                        // System on .NET Framework fails to compile `TypeEqual` for a nullable parameter, so the expected value is computed by the definition
                        Func<object, bool> sys;
                        try { sys = lambda.CompileSys(); }
                        catch (InvalidOperationException) when (isEqual && p.Type.IsValueType)
                        {
                            sys = v => v != null && v.GetType() == ty.GetNonNullableOrSelf();
                        }
#if LIGHT_EXPRESSION
                        var back = lambda.ToLambdaExpression().ToLightExpression();
                        var backFast = (Func<object, bool>)back.CompileFast(true);
                        t.IsNotNull(backFast, what + " after the round trip");
#endif

                        foreach (var v in values)
                        {
                            t.AreEqual(sys(v), fast(v), what + " with " + (v ?? "null"));
#if LIGHT_EXPRESSION
                            t.AreEqual(sys(v), backFast(v), what + " round trip with " + (v ?? "null"));
#endif
                        }
                    }

            // the result is ignored but the operand is still evaluated
            var calls = 0;
            Func<object> side = () => { ++calls; return "x"; };
            var ignored = Lambda<Action>(TypeEqual(Invoke(Constant(side)), typeof(string))).CompileFast(true);
            ignored();
            t.AreEqual(1, calls);
        }

        public class TypeTestBase { }
        public class TypeTestDerived : TypeTestBase { }

        public void Type_is_and_type_equal_emit_the_short_IL(TestContext t)
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

        public void Type_is_and_type_equal_with_the_known_value_type_still_evaluate_the_operand(TestContext t)
        {
            var calls = 0;
            Func<int> side = () => { ++calls; return 42; };
            Func<int?> sideNullable = () => { ++calls; return null; };

            t.AreEqual(true, Lambda<Func<bool>>(TypeIs(Invoke(Constant(side)), typeof(object))).CompileFast(true)());
            t.AreEqual(false, Lambda<Func<bool>>(TypeEqual(Invoke(Constant(side)), typeof(long))).CompileFast(true)());
            t.AreEqual(false, Lambda<Func<bool>>(TypeIs(Invoke(Constant(sideNullable)), typeof(int))).CompileFast(true)());
            t.AreEqual(false, Lambda<Func<bool>>(TypeEqual(Invoke(Constant(sideNullable)), typeof(int))).CompileFast(true)());
            t.AreEqual(4, calls);
        }
        public void Can_compile_right_shift_with_nullable_shift_count(TestContext t)
        {
            var value = Parameter(typeof(int), "value");
            var count = Parameter(typeof(int?), "count");
            var nullableValue = Parameter(typeof(int?), "nullableValue");
            var plainCount = Parameter(typeof(int), "plainCount");

            // the non-nullable left with the nullable count is the `TypedBinaryExpression` path, the result is nullable
            foreach (var nodeType in new[] { ExpressionType.RightShift, ExpressionType.LeftShift })
            {
                var shift = nodeType == ExpressionType.RightShift ? RightShift(value, count) : LeftShift(value, count);
                t.AreEqual(typeof(int?), shift.Type);
#if LIGHT_EXPRESSION
                t.AreEqual(typeof(int?), shift.ToExpression().Type);
#endif

                var lambda = Lambda<Func<int, int?, int?>>(shift, value, count);
                var fast = lambda.CompileFast(true);
                t.IsNotNull(fast);
                var sys = lambda.CompileSys();
                foreach (var v in new[] { 64, -64, 1 })
                    foreach (int? c in new int?[] { null, 0, 2, 5 })
                        t.AreEqual(sys(v, c), fast(v, c));

                // the nullable left keeps the left typed path
                var nullableLeft = nodeType == ExpressionType.RightShift ? RightShift(nullableValue, count) : LeftShift(nullableValue, count);
                var nullableLambda = Lambda<Func<int?, int?, int?>>(nullableLeft, nullableValue, count);
                var nullableFast = nullableLambda.CompileFast(true);
                t.IsNotNull(nullableFast);
                var nullableSys = nullableLambda.CompileSys();
                foreach (int? v in new int?[] { null, 64, -64 })
                    foreach (int? c in new int?[] { null, 2 })
                        t.AreEqual(nullableSys(v, c), nullableFast(v, c));

                // the plain operands keep the left typed path with the plain result
                var plain = nodeType == ExpressionType.RightShift ? RightShift(value, plainCount) : LeftShift(value, plainCount);
                t.AreEqual(typeof(int), plain.Type);
                var plainLambda = Lambda<Func<int, int, int>>(plain, value, plainCount);
                t.AreEqual(plainLambda.CompileSys()(64, 2), plainLambda.CompileFast(true)(64, 2));
            }

            // the unsigned value is shifted logically
            var uvalue = Parameter(typeof(uint), "uvalue");
            var ushift = RightShift(uvalue, count);
            t.AreEqual(typeof(uint?), ushift.Type);
            var ulambda = Lambda<Func<uint, int?, uint?>>(ushift, uvalue, count);
            var ufast = ulambda.CompileFast(true);
            t.IsNotNull(ufast);
            var usys = ulambda.CompileSys();
            t.AreEqual(usys(0x80000000u, 4), ufast(0x80000000u, 4));
            t.AreEqual(usys(0x80000000u, null), ufast(0x80000000u, null));

            // the shift with a compatible method is built by the method based operator, an incompatible one is rejected
            var shiftMethod = typeof(ParityTests).GetMethod(nameof(AndInts), BindingFlags.Static | BindingFlags.NonPublic);
            var withMethod = RightShift(value, plainCount, shiftMethod);
            t.AreEqual(shiftMethod, withMethod.Method);
            t.AreEqual(typeof(int), withMethod.Type);
            t.Throws<InvalidOperationException>(() => RightShift(Constant("a"), plainCount, shiftMethod));
        }

        public void Can_compile_shifts_with_nullable_shift_count(TestContext t)
        {
            var shift = Parameter(typeof(int?), "shift");

            var leftShift = LeftShift(Constant(16), shift);
            t.AreEqual(typeof(int?), leftShift.Type);
            var lambda = Lambda<Func<int?, int?>>(leftShift, shift);
            var fast = lambda.CompileFast(true);
            t.IsNotNull(fast);
            var sys = lambda.CompileSys();

            t.AreEqual(64, fast(2));
            t.AreEqual(64, sys(2));
            t.AreEqual(null, fast(null));
            t.AreEqual(null, sys(null));
        }
    }
}

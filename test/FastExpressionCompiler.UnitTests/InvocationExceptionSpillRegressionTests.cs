using System;
using System.Collections.Generic;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct InvocationExceptionSpillRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Try_arguments_preserve_delegate_invocation_behavior(t);
        Invocation_target_and_arguments_are_evaluated_in_order(t);
        By_reference_arguments_remain_references_across_try_arguments(t);
        Atomic_arithmetic_operands_follow_try_argument_finalizers(t);
        Left_try_arithmetic_preserves_values_and_overflow_order(t);
        Right_try_arithmetic_preserves_operand_and_exception_order(t);
#if LIGHT_EXPRESSION
        Intrinsic_constants_keep_their_exception_region_spills(t);
#endif
    }

    public void Try_arguments_preserve_delegate_invocation_behavior(TestContext t)
    {
        var function = SysExpr.Parameter(typeof(Func<int, int, int, int>));
        var x = SysExpr.Parameter(typeof(int));
        var y = SysExpr.Parameter(typeof(int));
        var z = SysExpr.Parameter(typeof(int));
        for (var tryIndex = 0; tryIndex < 3; ++tryIndex)
        {
            System.Linq.Expressions.Expression[] arguments = { x, y, z };
            arguments[tryIndex] = SysExpr.TryFinally(arguments[tryIndex], SysExpr.Empty());
            var expression = SysExpr.Lambda<Func<Func<int, int, int, int>, int, int, int, int>>(
                SysExpr.Invoke(function, arguments), function, x, y, z);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Func<int, int, int, int> calculate = (a, b, c) => a + b * c;
            t.AreEqual(7, fast(calculate, 1, 2, 3));
            t.AreEqual(expression.Compile()(calculate, -3, 4, 5), fast(calculate, -3, 4, 5));
        }
    }

    public void Invocation_target_and_arguments_are_evaluated_in_order(TestContext t)
    {
        var target = SysExpr.Parameter(typeof(Func<Func<int, int, int, int>>));
        var first = SysExpr.Parameter(typeof(Func<int>));
        var second = SysExpr.Parameter(typeof(Func<int>));
        var third = SysExpr.Parameter(typeof(Func<int>));
        var finallyAction = SysExpr.Parameter(typeof(Action));
        var expression = SysExpr.Lambda<Func<Func<Func<int, int, int, int>>, Func<int>, Func<int>, Func<int>, Action, int>>(
            SysExpr.Invoke(SysExpr.Invoke(target), SysExpr.TryFinally(SysExpr.Invoke(first), SysExpr.Invoke(finallyAction)),
                SysExpr.Invoke(second), SysExpr.Invoke(third)), target, first, second, third, finallyAction);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var order = new List<int>();
        Func<Func<int, int, int, int>> factory = () => { order.Add(0); return (a, b, c) => { order.Add(5); return a + b * c; }; };
        Func<int> firstValue = () => { order.Add(1); return 1; };
        Func<int> secondValue = () => { order.Add(3); return 2; };
        Func<int> thirdValue = () => { order.Add(4); return 3; };
        Action finalizer = () => order.Add(2);
        t.AreEqual(7, fast(factory, firstValue, secondValue, thirdValue, finalizer));
        t.AreEqual("0,1,2,3,4,5", string.Join(",", order));
        order.Clear();
        t.AreEqual(7, expression.Compile()(factory, firstValue, secondValue, thirdValue, finalizer));
        t.AreEqual("0,1,2,3,4,5", string.Join(",", order));
    }

    public delegate int RefAdder(ref int value, int increment);
    public delegate int RefCaller(ref int value, RefAdder add);

    public void By_reference_arguments_remain_references_across_try_arguments(TestContext t)
    {
        var value = SysExpr.Parameter(typeof(int).MakeByRefType());
        var function = SysExpr.Parameter(typeof(RefAdder));
        var expression = SysExpr.Lambda<RefCaller>(SysExpr.Invoke(function, value,
            SysExpr.TryFinally(SysExpr.Constant(2), SysExpr.Empty())), value, function);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        RefAdder add = (ref int item, int increment) => item += increment;
        var actualValue = 40;
        t.AreEqual(42, fast(ref actualValue, add));
        t.AreEqual(42, actualValue);
#if !NETFRAMEWORK
        // Framework's system compiler rejects EH combined with by-ref arguments; FEC behavior is asserted above.
        var expectedValue = 40;
        t.AreEqual(42, expression.Compile()(ref expectedValue, add));
        t.AreEqual(expectedValue, actualValue);
#endif
    }
    public void Atomic_arithmetic_operands_follow_try_argument_finalizers(TestContext t)
    {
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining })
        for (var rightKind = 0; rightKind < 3; ++rightKind)
        {
            var value = SysExpr.Parameter(typeof(int));
            var argument = SysExpr.Parameter(typeof(int));
            System.Linq.Expressions.Expression right = rightKind == 0 ? SysExpr.Constant(2) :
                rightKind == 1 ? (System.Linq.Expressions.Expression)value : SysExpr.Default(typeof(int));
            var inner = SysExpr.Lambda<Func<int, int>>(SysExpr.Add(argument, right), argument);
            var input = SysExpr.TryFinally(value, SysExpr.Block(SysExpr.Assign(value, SysExpr.Constant(7)), SysExpr.Empty()));
            var expression = SysExpr.Lambda<Func<int, int>>(SysExpr.Invoke(inner, input), value);
            var actual = expression.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(actual)) return;
            t.AreEqual(rightKind == 0 ? 5 : rightKind == 1 ? 10 : 3, actual(3));
            t.AreEqual(expression.Compile()(3), actual(3));
        }
    }

    public void Left_try_arithmetic_preserves_values_and_overflow_order(TestContext t)
    {
        var value = SysExpr.Parameter(typeof(int));
        var finalizer = SysExpr.Parameter(typeof(Action));
        var left = SysExpr.TryFinally(value, SysExpr.Invoke(finalizer));
        var expression = SysExpr.Lambda<Func<int, Action, int>>(SysExpr.TryCatch(
            SysExpr.AddChecked(left, SysExpr.Constant(2)), SysExpr.Catch(typeof(OverflowException), SysExpr.Constant(-1))), value, finalizer);
        var actual = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(actual)) return;
        var calls = 0;
        Action finish = () => ++calls;
        t.AreEqual(5, actual(3, finish));
        t.AreEqual(1, calls);
        t.AreEqual(-1, actual(int.MaxValue, finish));
        t.AreEqual(2, calls);
        calls = 0;
        var expected = expression.Compile();
        t.AreEqual(5, expected(3, finish));
        t.AreEqual(-1, expected(int.MaxValue, finish));
        t.AreEqual(2, calls);
    }

    public void Right_try_arithmetic_preserves_operand_and_exception_order(TestContext t)
    {
        var first = SysExpr.Parameter(typeof(Func<int>));
        var second = SysExpr.Parameter(typeof(Func<int>));
        var finalizer = SysExpr.Parameter(typeof(Action));
        var expression = SysExpr.Lambda<Func<Func<int>, Func<int>, Action, int>>(
            SysExpr.Add(SysExpr.Invoke(first), SysExpr.TryFinally(SysExpr.Invoke(second), SysExpr.Invoke(finalizer))), first, second, finalizer);
        var actual = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(actual)) return;
        var order = new List<int>();
        Func<int> left = () => { order.Add(1); return 3; };
        Func<int> right = () => { order.Add(2); return 5; };
        Action finish = () => order.Add(3);
        t.AreEqual(8, actual(left, right, finish));
        t.AreEqual("1,2,3", string.Join(",", order));
        order.Clear();
        Func<int> throwingRight = () => { order.Add(2); throw new InvalidOperationException(); };
        t.Throws<InvalidOperationException>(() => actual(left, throwingRight, finish));
        t.AreEqual("1,2,3", string.Join(",", order));
        order.Clear();
        t.Throws<InvalidOperationException>(() => expression.Compile()(left, throwingRight, finish));
        t.AreEqual("1,2,3", string.Join(",", order));
    }

#if LIGHT_EXPRESSION
    private sealed class TryConstant : ConstantExpression
    {
        public override Type Type => typeof(int);
        public override object Value => 5;
        public override bool IsIntrinsic => true;
        public override ExpressionCompiler.Result TryCollectInfo(ref ExpressionCompiler.CompilerContext context,
            ExpressionCompiler.NestedLambdaInfo nestedLambda, ref ImTools.SmallList<ExpressionCompiler.NestedLambdaInfo> rootNestedLambdas)
        {
            return ExpressionCompiler.TryCollectInfo(ref context,
                Expression.TryFinally(Expression.Constant(5), Expression.Empty()), nestedLambda, ref rootNestedLambdas);
        }
        public override bool TryEmit(ref ExpressionCompiler.CompilerContext context, System.Reflection.Emit.ILGenerator il,
            ExpressionCompiler.ParentFlags parent, int byRefIndex = -1)
        {
            var value = il.DeclareLocal(typeof(int));
            il.BeginExceptionBlock();
            il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4_5);
            il.Emit(System.Reflection.Emit.OpCodes.Stloc, value);
            il.BeginFinallyBlock();
            il.Emit(System.Reflection.Emit.OpCodes.Nop);
            il.EndExceptionBlock();
            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, value);
            context.LastEmitIsAddress = false;
            return true;
        }
    }

    public void Intrinsic_constants_keep_their_exception_region_spills(TestContext t)
    {
        var expression = Expression.Lambda<Func<int>>(Expression.Add(Expression.Constant(3), new TryConstant()));
        var actual = expression.CompileFast(true);
        if (!t.IsNotNull(actual)) return;
        t.AreEqual(8, actual());
    }
#endif
}

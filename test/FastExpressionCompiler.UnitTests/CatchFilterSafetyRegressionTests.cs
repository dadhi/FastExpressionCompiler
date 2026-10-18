using System;
using System.Collections.Generic;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CatchFilterSafetyRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
#if !NETFRAMEWORK
        Filter_guard_survives_child_context_changes(t);
        Filtered_handler_without_variable_has_empty_entry_stack(t);
        Separately_compiled_filter_lambda_has_its_own_exception_scope(t);
#else
        // DynamicILGenerator filters are unsupported on .NET Framework; rejection is covered by CatchFilterRegressionTests.
#endif
    }

    public sealed class BoolIndexer
    {
        public bool this[int index] { get => true; set { } }
    }
    public static bool Identity(bool value) => value;

    public void Filter_guard_survives_child_context_changes(TestContext t)
    {
        var obj = SysExpr.New(typeof(BoolIndexer));
        var item = typeof(BoolIndexer).GetProperty("Item");
        var identity = typeof(CatchFilterSafetyRegressionTests).GetMethod(nameof(Identity));
        var wrappers = new List<Func<System.Linq.Expressions.Expression, System.Linq.Expressions.Expression>>
        {
            x => x,
            x => SysExpr.ArrayIndex(SysExpr.NewArrayInit(typeof(bool), x), SysExpr.Constant(0)),
            x => SysExpr.MakeIndex(SysExpr.Condition(x, obj, obj), item, new[] { SysExpr.Constant(0) }),
            x => SysExpr.Assign(SysExpr.MakeIndex(SysExpr.Condition(x, obj, obj), item, new[] { SysExpr.Constant(0) }), SysExpr.Constant(true)),
            x => SysExpr.Call(identity, x),
            x => SysExpr.Invoke(SysExpr.Lambda<Func<bool>>(x)),
            x => SysExpr.OrElse(SysExpr.Constant(false), x),
            x => SysExpr.AndAlso(SysExpr.Constant(true), x),
            x => SysExpr.Equal(x, SysExpr.Constant(true)),
            x => SysExpr.Condition(SysExpr.Constant(true), x, SysExpr.Constant(false))
        };
#if !LIGHT_EXPRESSION
        wrappers.Add(x => new ReducedFilter(x));
#endif
        foreach (var wrap in wrappers)
        {
            var outside = SysExpr.Label(typeof(bool));
            var inside = SysExpr.Label(typeof(bool));
            var nested = SysExpr.TryCatch(SysExpr.Constant(true), SysExpr.Catch(typeof(Exception), SysExpr.Constant(false)));
            var jump = SysExpr.Goto(outside, SysExpr.Constant(true), typeof(bool));
            var returnPattern = SysExpr.Block(SysExpr.Return(inside, SysExpr.Constant(true)), SysExpr.Label(inside, SysExpr.Constant(false)));
            foreach (var hidden in new System.Linq.Expressions.Expression[] { nested, jump, returnPattern, SysExpr.Rethrow(typeof(bool)) })
            {
                var guarded = SysExpr.TryCatch(SysExpr.Throw(SysExpr.New(typeof(Exception)), typeof(bool)),
                    SysExpr.Catch(typeof(Exception), SysExpr.Constant(true), wrap(hidden)));
                var expression = SysExpr.Lambda<Func<bool>>(SysExpr.Block(guarded, SysExpr.Label(outside, SysExpr.Constant(false))));
                if (ReferenceEquals(hidden, nested))
                    t.Throws<InvalidOperationException>(() => expression.FromSysExpression().CompileFast(true));
                else
                    t.AreEqual(null, expression.FromSysExpression().CompileFast(true));
            }
        }
    }

    public void Filtered_handler_without_variable_has_empty_entry_stack(TestContext t)
    {
        var handler = SysExpr.TryCatch(SysExpr.Throw(SysExpr.New(typeof(ArgumentException)), typeof(int)),
            SysExpr.Catch(typeof(ArgumentException), SysExpr.Constant(7)));
        foreach (var selected in new[] { false, true })
        {
            var body = SysExpr.TryCatch(SysExpr.Throw(SysExpr.New(typeof(InvalidOperationException)), typeof(int)),
                SysExpr.Catch(typeof(InvalidOperationException), handler, SysExpr.Constant(selected)),
                SysExpr.Catch(typeof(Exception), handler));
            var fast = SysExpr.Lambda<Func<int>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(7, fast());
        }
    }

    public void Separately_compiled_filter_lambda_has_its_own_exception_scope(TestContext t)
    {
        var nested = SysExpr.Lambda<Func<bool>>(SysExpr.TryCatch(
            SysExpr.Throw(SysExpr.New(typeof(ArgumentException)), typeof(bool)),
            SysExpr.Catch(typeof(ArgumentException), SysExpr.Constant(true))));
        var body = SysExpr.TryCatch(SysExpr.Throw(SysExpr.New(typeof(InvalidOperationException)), typeof(int)),
            SysExpr.Catch(typeof(InvalidOperationException), SysExpr.Constant(7), SysExpr.Invoke(nested)));
        var expression = SysExpr.Lambda<Func<int>>(body).FromSysExpression();
        var fast = expression.CompileFast(true, CompilerFlags.NoInvocationLambdaInlining);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(7, fast());
    }

#if !LIGHT_EXPRESSION
    private sealed class ReducedFilter : System.Linq.Expressions.Expression
    {
        private readonly System.Linq.Expressions.Expression _body;
        public ReducedFilter(System.Linq.Expressions.Expression body) => _body = body;
        public override System.Linq.Expressions.ExpressionType NodeType => System.Linq.Expressions.ExpressionType.Extension;
        public override Type Type => typeof(bool);
        public override bool CanReduce => true;
        public override System.Linq.Expressions.Expression Reduce() => _body;
    }
#endif
}

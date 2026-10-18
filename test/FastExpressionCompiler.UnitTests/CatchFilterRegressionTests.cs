using System;
using System.Collections.Generic;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CatchFilterRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Type_guards_and_false_filters_preserve_selection_order(t);
        Filter_variable_mutation_is_scoped_to_its_handler(t);
        Throwing_filter_preserves_original_exception(t);
        Filters_run_before_inner_finally_and_fault(t);
        Nullable_and_user_logical_filters_evaluate_once(t);
        Rethrow_preserves_original_after_filter_variable_mutation(t);
        Unsupported_filter_control_flow_is_rejected(t);
    }

    public void Type_guards_and_false_filters_preserve_selection_order(TestContext t)
    {
        var events = new List<string>();
        Func<bool> wrongType = () => { events.Add("wrong"); return true; };
        Func<bool> reject = () => { events.Add("reject"); return false; };
        Func<bool> accept = () => { events.Add("accept"); return true; };
        Func<int> body = () => { events.Add("body"); return 7; };
        var expression = SysExpr.Lambda<Func<int>>(SysExpr.TryCatch(
            SysExpr.Throw(SysExpr.Constant(new InvalidOperationException()), typeof(int)),
            SysExpr.Catch(typeof(ArgumentException), SysExpr.Constant(1), SysExpr.Invoke(SysExpr.Constant(wrongType))),
            SysExpr.Catch(typeof(InvalidOperationException), SysExpr.Constant(2), SysExpr.Invoke(SysExpr.Constant(reject))),
            SysExpr.Catch(typeof(InvalidOperationException), SysExpr.Invoke(SysExpr.Constant(body)), SysExpr.Invoke(SysExpr.Constant(accept))),
            SysExpr.Catch(typeof(Exception), SysExpr.Constant(9))));
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(7, fast());
        t.AreEqual("reject,accept,body", string.Join(",", events));
    }

    public void Filter_variable_mutation_is_scoped_to_its_handler(TestContext t)
    {
        var original = new InvalidOperationException("original");
        var replacement = new ArgumentException("replacement");
        var error = SysExpr.Parameter(typeof(Exception));
        var assign = SysExpr.Assign(error, SysExpr.Constant(replacement, typeof(Exception)));
        foreach (var selected in new[] { false, true })
        {
            var expression = SysExpr.Lambda<Func<Exception>>(SysExpr.TryCatch(
                SysExpr.Throw(SysExpr.Constant(original), typeof(Exception)),
                SysExpr.Catch(error, error, SysExpr.Block(assign, SysExpr.Constant(selected))),
                SysExpr.Catch(error, error, SysExpr.ReferenceEqual(error, SysExpr.Constant(original)))));
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(selected ? (Exception)replacement : original, fast());
        }
    }

    public void Throwing_filter_preserves_original_exception(TestContext t)
    {
        var original = new InvalidOperationException("original");
        var error = SysExpr.Parameter(typeof(Exception));
        var expression = SysExpr.Lambda<Func<Exception>>(SysExpr.TryCatch(
            SysExpr.Throw(SysExpr.Constant(original), typeof(Exception)),
            SysExpr.Catch(typeof(Exception), SysExpr.Default(typeof(Exception)),
                SysExpr.Throw(SysExpr.New(typeof(ArgumentException)), typeof(bool))),
            SysExpr.Catch(error, error)));
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(original, fast());
    }

    public void Filters_run_before_inner_finally_and_fault(TestContext t)
    {
        foreach (var fault in new[] { false, true })
        {
            var events = new List<string>();
            Action before = () => events.Add("try");
            Action cleanup = () => events.Add("cleanup");
            Func<bool> filter = () => { events.Add("filter"); return true; };
            Func<int> handler = () => { events.Add("handler"); return 7; };
            var body = SysExpr.Block(SysExpr.Invoke(SysExpr.Constant(before)),
                SysExpr.Throw(SysExpr.New(typeof(InvalidOperationException)), typeof(int)));
            var inner = fault ? SysExpr.TryFault(body, SysExpr.Invoke(SysExpr.Constant(cleanup)))
                : SysExpr.TryFinally(body, SysExpr.Invoke(SysExpr.Constant(cleanup)));
            var expression = SysExpr.Lambda<Func<int>>(SysExpr.TryCatch(inner,
                SysExpr.Catch(typeof(Exception), SysExpr.Invoke(SysExpr.Constant(handler)), SysExpr.Invoke(SysExpr.Constant(filter)))));
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(7, fast());
            t.AreEqual("try,filter,cleanup,handler", string.Join(",", events));
        }
    }

    public void Nullable_and_user_logical_filters_evaluate_once(TestContext t)
    {
        foreach (var leftValue in new bool?[] { null, false, true })
        {
            var leftCalls = 0;
            var rightCalls = 0;
            Func<bool?> left = () => { ++leftCalls; return leftValue; };
            Func<bool?> right = () => { ++rightCalls; return true; };
            var filter = SysExpr.Coalesce(SysExpr.AndAlso(SysExpr.Invoke(SysExpr.Constant(left)),
                SysExpr.Invoke(SysExpr.Constant(right))), SysExpr.Constant(false));
            var expression = SysExpr.Lambda<Func<int>>(SysExpr.TryCatch(
                SysExpr.Throw(SysExpr.New(typeof(InvalidOperationException)), typeof(int)),
                SysExpr.Catch(typeof(Exception), SysExpr.Constant(7), filter),
                SysExpr.Catch(typeof(Exception), SysExpr.Constant(9))));
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(leftValue == true ? 7 : 9, fast());
            t.AreEqual(1, leftCalls);
            t.AreEqual(leftValue == false ? 0 : 1, rightCalls);
        }
        foreach (var leftValue in new[] { 0, 5 })
        {
            var leftCalls = 0;
            var rightCalls = 0;
            Func<UserDefinedLogicalRegressionTests.LogicalValue> left = () => { ++leftCalls; return new(leftValue); };
            Func<UserDefinedLogicalRegressionTests.LogicalValue> right = () => { ++rightCalls; return new(3); };
            var filter = SysExpr.Call(typeof(UserDefinedLogicalRegressionTests.LogicalValue).GetMethod("op_True"),
                SysExpr.AndAlso(SysExpr.Invoke(SysExpr.Constant(left)), SysExpr.Invoke(SysExpr.Constant(right))));
            var expression = SysExpr.Lambda<Func<int>>(SysExpr.TryCatch(
                SysExpr.Throw(SysExpr.New(typeof(InvalidOperationException)), typeof(int)),
                SysExpr.Catch(typeof(Exception), SysExpr.Constant(7), filter),
                SysExpr.Catch(typeof(Exception), SysExpr.Constant(9))));
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(leftValue != 0 ? 7 : 9, fast());
            t.AreEqual(1, leftCalls);
            t.AreEqual(leftValue == 0 ? 0 : 1, rightCalls);
        }
    }

    public void Rethrow_preserves_original_after_filter_variable_mutation(TestContext t)
    {
        var original = new InvalidOperationException("original");
        var variable = SysExpr.Parameter(typeof(Exception));
        var caught = SysExpr.Parameter(typeof(Exception));
        var inner = SysExpr.TryCatch(SysExpr.Throw(SysExpr.Constant(original), typeof(Exception)),
            SysExpr.Catch(variable, SysExpr.Rethrow(typeof(Exception)),
                SysExpr.Block(SysExpr.Assign(variable, SysExpr.New(typeof(ArgumentException))), SysExpr.Constant(true))));
        var expression = SysExpr.Lambda<Func<Exception>>(SysExpr.TryCatch(inner, SysExpr.Catch(caught, caught)));
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(original, fast());
    }

    public void Unsupported_filter_control_flow_is_rejected(TestContext t)
    {
        var nested = SysExpr.TryCatch(SysExpr.Constant(true), SysExpr.Catch(typeof(Exception), SysExpr.Constant(false)));
        var target = SysExpr.Label(typeof(bool));
        var jump = SysExpr.Block(SysExpr.Goto(target, SysExpr.Constant(true)), SysExpr.Constant(false));
        foreach (var filter in new System.Linq.Expressions.Expression[] { nested, jump, SysExpr.Rethrow(typeof(bool)) })
        {
            var guarded = SysExpr.TryCatch(SysExpr.Throw(SysExpr.New(typeof(Exception)), typeof(bool)),
                SysExpr.Catch(typeof(Exception), SysExpr.Constant(true), filter));
            var expression = SysExpr.Lambda<Func<bool>>(SysExpr.Block(guarded, SysExpr.Label(target, SysExpr.Constant(false))));
            t.AreEqual(null, expression.FromSysExpression().CompileFast(true));
        }
    }
}

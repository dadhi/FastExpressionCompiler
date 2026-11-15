using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ArithmeticValueOperandTests : ITestX
{
    public struct Number { public int Value; public static Number operator +(Number a, Number b) => new Number { Value = a.Value + b.Value }; }
    public void Run(TestRun t) { Operator_temporaries_keep_value_operands(t); Writable_capture_field_keeps_its_address(t); }
    public void Operator_temporaries_keep_value_operands(TestContext t)
    {
        var value=S.Parameter(typeof(Number));
        var tree=S.Lambda<Func<Number,Func<int>>>(S.Lambda<Func<int>>(S.Assign(
            S.Field(S.Add(value,value),nameof(Number.Value)),S.Constant(17))),value);
        foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo})
        {
            var fast=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))return;
            t.AreEqual(17,fast(new Number{Value=7})());t.AreEqual(17,tree.Compile()(new Number{Value=7})());
        }
    }
    public void Writable_capture_field_keeps_its_address(TestContext t)
    {
        var value=S.Parameter(typeof(Number));var field=S.Field(value,nameof(Number.Value));
        var right=S.Field(S.Add(value,value),nameof(Number.Value));
        var tree=S.Lambda<Func<Number,Func<int>>>(S.Lambda<Func<int>>(S.AddAssign(field,right)),value);
        var control=S.Lambda<Func<Number,Func<int>>>(S.Lambda<Func<int>>(S.Assign(field,S.Add(field,right))),value);
        foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo})
        {
            var fast=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))return;
            var actual=fast(new Number{Value=7});var expected=control.Compile()(new Number{Value=7});
            t.AreEqual(expected(),actual());t.AreEqual(expected(),actual());
        }
    }
}

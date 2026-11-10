using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct LiftedOperandEvaluationRegressionTests : ITestX
{
    public void Run(TestRun t) { Both_operands_run_before_null_propagation(t); Null_left_preserves_right_exceptions(t); }
    public void Both_operands_run_before_null_propagation(TestContext t)
    {
        foreach (var operation in new[] { System.Linq.Expressions.ExpressionType.Add, System.Linq.Expressions.ExpressionType.Subtract,
            System.Linq.Expressions.ExpressionType.Multiply, System.Linq.Expressions.ExpressionType.Divide, System.Linq.Expressions.ExpressionType.Modulo,
            System.Linq.Expressions.ExpressionType.And, System.Linq.Expressions.ExpressionType.Or, System.Linq.Expressions.ExpressionType.ExclusiveOr })
        {
            var first = S.Parameter(typeof(Func<int?>)); var second = S.Parameter(typeof(Func<int?>));
            var tree = S.Lambda<Func<Func<int?>, Func<int?>, int?>>(S.MakeBinary(operation, S.Invoke(first), S.Invoke(second)), first, second);
            var system = tree.Compile();
            foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
            {
                var fast = tree.FromSysExpression().CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
                foreach (var left in new int?[] { null, 5 }) foreach (var right in new int?[] { null, 3 })
                {
                    var trace = new List<int>(); Func<int?> a = () => { trace.Add(0); return left; }; Func<int?> b = () => { trace.Add(1); return right; };
                    var expected = system(a, b); trace.Clear(); t.AreEqual(expected, fast(a, b)); t.AreEqual("0,1", string.Join(",", trace));
                }
            }
        }
    }
    public void Null_left_preserves_right_exceptions(TestContext t)
    {
        var right = S.Parameter(typeof(Func<int?>));
        var tree = S.Lambda<Func<Func<int?>, int?>>(S.Add(S.Default(typeof(int?)), S.Invoke(right)), right);
        Func<int?> fail = () => throw new InvalidOperationException();
        t.Throws<InvalidOperationException>(() => tree.Compile()(fail));
        var fast = tree.FromSysExpression().CompileFast(true); if (t.IsNotNull(fast)) t.Throws<InvalidOperationException>(() => fast(fail));
    }
}

using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct ArithmeticExceptionOperandsTests : ITestX
{
    public void Run(TestRun t) { Nested_exception_operands_keep_the_stack_empty(t); }
    public void Nested_exception_operands_keep_the_stack_empty(TestContext t)
    {
        foreach (var type in new[] { typeof(int), typeof(int?), typeof(decimal), typeof(decimal?) })
        {
            var value = S.Parameter(type);
            var calls = 0; Action cleanup = () => ++calls;
            var body = S.Add(value, S.Add(S.TryFinally(value, S.Invoke(S.Constant(cleanup))), value));
            var tree = S.Lambda(body, value); var fast = tree.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var input = type == typeof(int) || type == typeof(int?) ? (object)7 : 7m;
            t.AreEqual(tree.Compile().DynamicInvoke(input), fast.DynamicInvoke(input)); t.AreEqual(2, calls);
            if (type.IsNullable()) t.AreEqual(null, fast.DynamicInvoke(new object[] { null }));
        }
    }
}

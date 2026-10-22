// The safety guard uses the dynamic-method decoder supported on modern .NET.
// The fatal repro itself is also exercised in a separate child-process harness.
#if NET8_0_OR_GREATER
using System;
using System.Linq;
using System.Linq.Expressions;

#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ILDecoder;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using FastExpressionCompiler.ILDecoder;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue561_NonBooleanEqualityOperator : ITestX
{
    public void Run(TestRun t)
    {
        Equal_returns_the_interface_from_the_selected_operator(t);
        NotEqual_returns_the_interface_from_the_selected_operator(t);
        Reversed_equal_returns_the_interface_from_the_selected_operator(t);
        Reversed_not_equal_returns_the_interface_from_the_selected_operator(t);
    }

    public interface ICustomObject { void Ping(); }

    public sealed class CustomObject : ICustomObject
    {
        public static int OperatorCalls;
        public static int PingCalls;

        public static ICustomObject operator ==(double left, CustomObject right) => Result();
        public static ICustomObject operator !=(double left, CustomObject right) => Result();
        public static ICustomObject operator ==(CustomObject left, double right) => Result();
        public static ICustomObject operator !=(CustomObject left, double right) => Result();
        private static ICustomObject Result() { ++OperatorCalls; return new CustomObject(); }
        public void Ping() { ++PingCalls; }
        public override bool Equals(object obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => base.GetHashCode();
    }

    public void Equal_returns_the_interface_from_the_selected_operator(TestContext t) =>
        Check(t, () => 1 == new CustomObject());

    public void NotEqual_returns_the_interface_from_the_selected_operator(TestContext t) =>
        Check(t, () => 1 != new CustomObject());

    public void Reversed_equal_returns_the_interface_from_the_selected_operator(TestContext t) =>
        Check(t, () => new CustomObject() == 1);

    public void Reversed_not_equal_returns_the_interface_from_the_selected_operator(TestContext t) =>
        Check(t, () => new CustomObject() != 1);

    private static void Check(TestContext t, System.Linq.Expressions.Expression<Func<ICustomObject>> expression)
    {
        var binary = (System.Linq.Expressions.BinaryExpression)expression.Body;
        t.AreEqual(typeof(ICustomObject), binary.Type);
        t.IsNotNull(binary.Method);
        t.IsTrue(!binary.IsLifted);

        CustomObject.OperatorCalls = CustomObject.PingCalls = 0;
        var normal = expression.Compile()();
        t.AreEqual(1, CustomObject.OperatorCalls);
        normal.Ping();
        t.AreEqual(1, CustomObject.PingCalls);

        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
            CompilerFlags.EnableDelegateDebugInfo })
        {
#if LIGHT_EXPRESSION
            var fast = expression.ToLightExpression().CompileFast(true, flags);
#else
            var fast = expression.CompileFast(true, flags);
#endif
            if (!t.IsNotNull(fast)) return;

            // On affected compilers the returned reference is corrupt. Check the
            // selected call before invocation so this regression fails without
            // taking down the whole runner. This does not assert an IL layout.
            var instructions = fast.TryGetDebugInfo()?.ILInstructions ?? fast.Method.ReadAllInstructions();
            if (!instructions.OfType<InlineMethodInstruction>().Any(i => i.Method.Equals(binary.Method)))
            {
                t.Fail("Issue561: selected non-Boolean operator call is missing; corrupt-reference delegate was not invoked.");
                return;
            }

            CustomObject.OperatorCalls = CustomObject.PingCalls = 0;
            var result = fast();
            t.AreEqual(1, CustomObject.OperatorCalls);
            if (!t.IsNotNull(result)) return;
            t.IsTrue(result is CustomObject);
            result.Ping();
            t.AreEqual(1, CustomObject.PingCalls);
        }
    }
}
#endif

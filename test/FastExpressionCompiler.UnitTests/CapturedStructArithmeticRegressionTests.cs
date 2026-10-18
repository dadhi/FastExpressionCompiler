using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CapturedStructArithmeticRegressionTests : ITestX
{
    public struct Counter
    {
        public int Value;
        public int Property { get { ++Reads; return Value; } }
        public int Read() { ++Reads; return Value; }
        public static Counter operator +(Counter left, Counter right)
        { ++Operations; return new Counter { Value = left.Value + right.Value }; }
        public static Counter operator -(Counter value)
        { ++Operations; return new Counter { Value = -value.Value }; }
    }

    private static int Operations;
    private static int Reads;
    private static readonly CompilerFlags[] Flags =
    {
        CompilerFlags.Default,
        CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression
    };

    public void Run(TestRun t)
    {
        Escaped_and_literal_lambdas_read_operator_results_by_value(t);
        Quoted_operator_results_compile_with_the_same_values(t);
        Writable_field_owners_keep_their_addresses(t);
    }

    private static System.Linq.Expressions.Expression Result(System.Linq.Expressions.Expression operand, int member) =>
        member == 0 ? S.Field(operand, nameof(Counter.Value)) : member == 1
            ? S.Property(operand, nameof(Counter.Property))
            : S.Call(operand, typeof(Counter).GetMethod(nameof(Counter.Read)));

    public void Escaped_and_literal_lambdas_read_operator_results_by_value(TestContext t)
    {
        // LightExpression currently rejects method-bearing unary nodes during System conversion.
#if LIGHT_EXPRESSION
foreach (var negate in new[] { false })
#else
foreach (var negate in new[] { false, true })
#endif
        for (var member = 0; member < 3; ++member)
        {
            var value = S.Parameter(typeof(Counter));
            var body = Result(negate ? S.Negate(value) : S.Add(value, value), member);
            var inner = S.Lambda<Func<int>>(body);
            var escaped = S.Lambda<Func<Counter, Func<int>>>(inner, value);
            var invoked = S.Lambda<Func<Counter, int>>(S.Invoke(inner), value);
            var expected = negate ? -7 : 14;
            Check(t, () => escaped.Compile()(new Counter { Value = 7 })(), expected, member);
            Check(t, () => invoked.Compile()(new Counter { Value = 7 }), expected, member);
            foreach (var flags in Flags)
            {
                var fastEscaped = escaped.FromSysExpression().CompileFast(true, flags);
                var fastInvoked = invoked.FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(fastEscaped)) Check(t, () => fastEscaped(new Counter { Value = 7 })(), expected, member);
                if (t.IsNotNull(fastInvoked)) Check(t, () => fastInvoked(new Counter { Value = 7 }), expected, member);
            }
        }
    }

    public void Quoted_operator_results_compile_with_the_same_values(TestContext t)
    {
        // LightExpression currently rejects method-bearing unary nodes during System conversion.
#if LIGHT_EXPRESSION
foreach (var negate in new[] { false })
#else
foreach (var negate in new[] { false, true })
#endif
        for (var member = 0; member < 3; ++member)
        {
            var value = S.Parameter(typeof(Counter));
            var inner = S.Lambda<Func<int>>(Result(negate ? S.Negate(value) : S.Add(value, value), member));
            var source = S.Lambda<Func<Counter, System.Linq.Expressions.Expression<Func<int>>>>(S.Quote(inner), value);
            var system = source.Compile();
            foreach (var flags in Flags)
            {
                var fast = source.FromSysExpression().CompileFast(true, flags);
                if (!t.IsNotNull(fast)) continue;
                foreach (var factory in new[] { system, fast })
                {
                    var quoted = factory(new Counter { Value = 7 });
                    Check(t, quoted.Compile(), negate ? -7 : 14, member);
                    var compiled = quoted.FromSysExpression().CompileFast(true, flags);
                    if (t.IsNotNull(compiled)) Check(t, compiled, negate ? -7 : 14, member);
                }
            }
        }
    }

    public void Writable_field_owners_keep_their_addresses(TestContext t)
    {
        var value = S.Parameter(typeof(Counter));
        var field = S.Field(value, nameof(Counter.Value));
        var right = S.Field(S.Add(value, value), nameof(Counter.Value));
        var compound = S.Lambda<Func<Counter, Func<int>>>(S.Lambda<Func<int>>(S.AddAssign(field, right)), value);
        // System's compound-field reduction copies the struct; ordinary assignment
        // supplies the equivalent writable-storage control for successive calls.
        var control = S.Lambda<Func<Counter, Func<int>>>(S.Lambda<Func<int>>(S.Assign(field, S.Add(field, right))), value);
        var system = control.Compile()(new Counter { Value = 7 });
        Check(t, system, 21, 0); Check(t, system, 63, 0);
        foreach (var flags in Flags)
        {
            var fast = compound.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) continue;
            var next = fast(new Counter { Value = 7 });
            Check(t, next, 21, 0); Check(t, next, 63, 0);
        }

        // The arithmetic operation is itself the field owner. Its operands must
        // remain values even though the resulting temporary is written by address.
        var temporary = S.Lambda<Func<Counter, Func<int>>>(S.Lambda<Func<int>>(S.Assign(
            S.Field(S.Add(value, value), nameof(Counter.Value)), S.Constant(17))), value);
        Check(t, () => temporary.Compile()(new Counter { Value = 7 })(), 17, 0);
        foreach (var flags in Flags)
        {
            var fast = temporary.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fast)) Check(t, () => fast(new Counter { Value = 7 })(), 17, 0);
        }
    }

    private static void Check(TestContext t, Func<int> action, int expected, int member)
    {
        Operations = Reads = 0;
        t.AreEqual(expected, action());
        t.AreEqual(1, Operations);
        t.AreEqual(member == 0 ? 0 : 1, Reads);
    }
}

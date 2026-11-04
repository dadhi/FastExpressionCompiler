using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using L = FastExpressionCompiler.LightExpression.Expression;
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
        public static Counter operator ++(Counter value)
        { ++Operations; return new Counter { Value = value.Value + 1 }; }
        public static Counter operator --(Counter value)
        { ++Operations; return new Counter { Value = value.Value - 1 }; }
        public static explicit operator Converted(Counter value)
        { ++Operations; return new Converted { Value = value.Value + 1 }; }
    }

    public struct Converted
    {
        public int Value;
        public int Property { get { ++Reads; return Value; } }
        public int Read() { ++Reads; return Value; }
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
#if LIGHT_EXPRESSION
        Native_negation_keeps_operator_operands_as_values(t);
#else
        Increment_results_keep_source_operands_as_values(t);
#endif
        Conversion_results_keep_source_operands_as_values(t);
        Scalar_unary_results_are_values_when_used_as_receivers(t);
        Discarded_unary_expressions_preserve_operand_effects(t);
    }

    public static bool ReadBoolean() { ++Operations; return true; }
    public static int? ReadNullable() { ++Operations; return 7; }
    public static int ReadInteger() { ++Operations; return 7; }

    private static void Scalar_unary_results_are_values_when_used_as_receivers(TestContext t)
    {
        var boolean = S.Parameter(typeof(bool));
        var compare = typeof(bool).GetMethod(nameof(bool.CompareTo), new[] { typeof(bool) });
        var not = S.Lambda<Func<bool, Func<int>>>(S.Lambda<Func<int>>(
            S.Call(S.Not(boolean), compare, S.Constant(false))), boolean);
        VerifyScalar(t, not, true, 0); VerifyScalar(t, not, false, 1);

        var nullable = S.Parameter(typeof(int?));
        var complement = S.Lambda<Func<int?, Func<int>>>(S.Lambda<Func<int>>(
            S.Call(S.Not(nullable), typeof(int?).GetMethod(nameof(Nullable<int>.GetValueOrDefault), Type.EmptyTypes))), nullable);
        VerifyScalar(t, complement, (int?)7, -8); VerifyScalar(t, complement, (int?)null, 0);

        var integer = S.Parameter(typeof(int));
        var typeAs = S.TypeAs(integer, typeof(object));
        var getType = S.Call(typeAs, typeof(object).GetMethod(nameof(object.GetType)));
        var cast = S.Lambda<Func<int, Func<int>>>(S.Lambda<Func<int>>(S.Condition(
            S.Equal(getType, S.Constant(typeof(int))), S.Constant(1), S.Constant(0))), integer);
        VerifyScalar(t, cast, 7, 1);
        var typeIs = S.Lambda<Func<int, Func<int>>>(S.Lambda<Func<int>>(
            S.Call(S.TypeIs(integer, typeof(int)), compare, S.Constant(false))), integer);
        VerifyScalar(t, typeIs, 7, 1);
    }

    private static void VerifyScalar<T>(TestContext t, System.Linq.Expressions.Expression<Func<T, Func<int>>> source,
        T argument, int expected)
    {
        Check(t, source.Compile()(argument), expected, 0, operations: 0);
        foreach (var flags in Flags)
        {
            var fast = source.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fast)) Check(t, fast(argument), expected, 0, operations: 0);
        }
    }

    private static void Discarded_unary_expressions_preserve_operand_effects(TestContext t)
    {
        var boolean = S.Call(typeof(CapturedStructArithmeticRegressionTests).GetMethod(nameof(ReadBoolean)));
        var nullable = S.Call(typeof(CapturedStructArithmeticRegressionTests).GetMethod(nameof(ReadNullable)));
        var integer = S.Call(typeof(CapturedStructArithmeticRegressionTests).GetMethod(nameof(ReadInteger)));
        foreach (var body in new System.Linq.Expressions.Expression[] {
            S.Not(boolean), S.Not(nullable), S.TypeAs(integer, typeof(object)), S.TypeIs(integer, typeof(int)) })
        {
            var source = S.Lambda<Action>(S.Block(body, S.Empty()));
            Operations = Reads = 0; source.Compile()(); t.AreEqual(1, Operations); t.AreEqual(0, Reads);
            foreach (var flags in Flags)
            {
                var fast = source.FromSysExpression().CompileFast(true, flags);
                if (!t.IsNotNull(fast)) continue;
                Operations = Reads = 0; fast(); t.AreEqual(1, Operations); t.AreEqual(0, Reads);
            }
        }
    }

#if !LIGHT_EXPRESSION
    private static void Increment_results_keep_source_operands_as_values(TestContext t)
    {
        foreach (var decrement in new[] { false, true })
        for (var member = 0; member < 3; ++member)
        {
            var value = S.Parameter(typeof(Counter));
            var body = Result(decrement ? S.Decrement(value) : S.Increment(value), member);
            var inner = S.Lambda<Func<int>>(body);
            var escaped = S.Lambda<Func<Counter, Func<int>>>(inner, value);
            var invoked = S.Lambda<Func<Counter, int>>(S.Invoke(inner), value);
            var expected = decrement ? 6 : 8;
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
#endif

    private static void Conversion_results_keep_source_operands_as_values(TestContext t)
    {
        for (var member = 0; member < 3; ++member)
        {
            var value = S.Parameter(typeof(Counter));
            var conversion = S.Convert(value, typeof(Converted));
            System.Linq.Expressions.Expression body = member == 0
                ? S.Field(conversion, nameof(Converted.Value)) : member == 1
                    ? S.Property(conversion, nameof(Converted.Property))
                    : S.Call(conversion, typeof(Converted).GetMethod(nameof(Converted.Read)));
            var inner = S.Lambda<Func<int>>(body);
            var escaped = S.Lambda<Func<Counter, Func<int>>>(inner, value);
            var invoked = S.Lambda<Func<Counter, int>>(S.Invoke(inner), value);
            Check(t, () => escaped.Compile()(new Counter { Value = 7 })(), 8, member);
            Check(t, () => invoked.Compile()(new Counter { Value = 7 }), 8, member);
            foreach (var flags in Flags)
            {
                var fastEscaped = escaped.FromSysExpression().CompileFast(true, flags);
                var fastInvoked = invoked.FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(fastEscaped)) Check(t, () => fastEscaped(new Counter { Value = 7 })(), 8, member);
                if (t.IsNotNull(fastInvoked)) Check(t, () => fastInvoked(new Counter { Value = 7 }), 8, member);
            }
#if LIGHT_EXPRESSION
            // Native Light conversion nodes defer operator lookup as well.
            var nativeValue = L.Parameter(typeof(Counter));
            var nativeConversion = L.Convert(nativeValue, typeof(Converted));
            t.AreEqual(true, nativeConversion.Method == null);
            FastExpressionCompiler.LightExpression.Expression nativeBody = member == 0
                ? L.Field(nativeConversion, nameof(Converted.Value)) : member == 1
                    ? L.Property(nativeConversion, nameof(Converted.Property))
                    : L.Call(nativeConversion, typeof(Converted).GetMethod(nameof(Converted.Read)));
            var native = L.Lambda<Func<Counter, Func<int>>>(L.Lambda<Func<int>>(nativeBody), nativeValue);
            foreach (var flags in Flags)
            {
                var fast = native.CompileFast(true, flags);
                if (t.IsNotNull(fast)) Check(t, () => fast(new Counter { Value = 7 })(), 8, member);
            }
#endif
        }

        var integer = S.Parameter(typeof(int));
        var nullableValue = S.Property(S.Convert(integer, typeof(int?)), nameof(Nullable<int>.Value));
        var nullable = S.Lambda<Func<int, Func<int>>>(S.Lambda<Func<int>>(nullableValue), integer);
        Check(t, () => nullable.Compile()(7)(), 7, 0, operations: 0);
        foreach (var flags in Flags)
        {
            var fast = nullable.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fast)) Check(t, () => fast(7)(), 7, 0, operations: 0);
        }
    }

#if LIGHT_EXPRESSION
    private static void Native_negation_keeps_operator_operands_as_values(TestContext t)
    {
        // Native Light nodes defer operator lookup and have Method == null, unlike
        // the System unary nodes above. Exercise the fallback emitter directly.
        foreach (var operation in new[] { 0, 1, 2, 3 })
        for (var member = 0; member < 3; ++member)
        {
            var systemValue = S.Parameter(typeof(Counter));
            var systemOperand = operation == 0 ? S.Negate(systemValue) : operation == 1 ? S.NegateChecked(systemValue)
                : operation == 2 ? S.Increment(systemValue) : S.Decrement(systemValue);
            var oracle = S.Lambda<Func<Counter, int>>(Result(systemOperand, member), systemValue).Compile();
            var expected = operation < 2 ? -7 : operation == 2 ? 8 : 6;
            Check(t, () => oracle(new Counter { Value = 7 }), expected, member);

            var value = L.Parameter(typeof(Counter));
            var operand = operation == 0 ? L.Negate(value) : operation == 1 ? L.NegateChecked(value)
                : operation == 2 ? L.Increment(value) : L.Decrement(value);
            t.AreEqual(true, operand.Method == null);
            FastExpressionCompiler.LightExpression.Expression body = member == 0
                ? L.Field(operand, nameof(Counter.Value)) : member == 1
                    ? L.Property(operand, nameof(Counter.Property))
                    : L.Call(operand, typeof(Counter).GetMethod(nameof(Counter.Read)));
            var inner = L.Lambda<Func<int>>(body);
            var escaped = L.Lambda<Func<Counter, Func<int>>>(inner, value);
            var invoked = L.Lambda<Func<Counter, int>>(L.Invoke(inner), value);
            foreach (var flags in Flags)
            {
                var fastEscaped = escaped.CompileFast(true, flags);
                var fastInvoked = invoked.CompileFast(true, flags);
                if (t.IsNotNull(fastEscaped)) Check(t, () => fastEscaped(new Counter { Value = 7 })(), expected, member);
                if (t.IsNotNull(fastInvoked)) Check(t, () => fastInvoked(new Counter { Value = 7 }), expected, member);
            }
        }
    }
#endif

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

    private static void Check(TestContext t, Func<int> action, int expected, int member, int operations = 1)
    {
        Operations = Reads = 0;
        t.AreEqual(expected, action());
        t.AreEqual(operations, Operations);
        t.AreEqual(member == 0 ? 0 : 1, Reads);
    }
}

using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using F = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using F = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct StringConstantIdentityRegressionTests : ITestX
{
    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };

    public void Run(TestRun t)
    {
        Declared_reference_types_preserve_string_identity(t);
        Distinct_constants_and_nested_lambdas_preserve_identity(t);
        Explicit_closure_compilation_preserves_string_identity(t);
        Constant_lengths_fold_without_observing_string_identity(t);
    }

    public void Constant_lengths_fold_without_observing_string_identity(TestContext t)
    {
        foreach (var flags in Flags)
        foreach (var text in new[] { "", "literal", Fresh() })
        {
            var source = S.Lambda<Func<int>>(S.Property(S.Constant(text), nameof(string.Length)));
            var tree = source.FromSysExpression();
            var fast = tree.CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(text.Length, fast());
            // Reading a known immutable length need not retain the string as a constant.
            var without = tree.TryCompileWithoutClosure<Func<int>>(flags);
            if (t.IsNotNull(without)) t.AreEqual(text.Length, without());
            var ignored = S.Lambda<Action>(source.Body).FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(ignored)) ignored();
        }
        var nullLength = S.Lambda<Func<int>>(S.Property(S.Constant(null, typeof(string)), nameof(string.Length)))
            .FromSysExpression().CompileFast(true);
        if (t.IsNotNull(nullLength)) t.Throws<NullReferenceException>(() => nullLength());
        var parameter = S.Parameter(typeof(string));
        var variableLength = S.Lambda<Func<string, int>>(S.Property(parameter, nameof(string.Length)), parameter)
            .FromSysExpression().CompileFast(true);
        if (t.IsNotNull(variableLength))
        {
            t.AreEqual(3, variableLength("abc"));
            t.Throws<NullReferenceException>(() => variableLength(null));
        }
    }

    private static string Fresh() => new string(string.Intern("fec-string-constant-identity").ToCharArray());
    public const string Literal = "fec-string-constant-identity";

    private static System.Linq.Expressions.Expression Bound(string value, Type type) =>
        S.Convert(S.Constant(value, typeof(object)), type);

    public void Declared_reference_types_preserve_string_identity(TestContext t)
    {
        CheckExplicitBlocks(t);
        foreach (var type in new[] { typeof(string), typeof(object), typeof(IComparable), typeof(ICloneable) })
        {
            var value = Fresh();
            var constant = S.Constant(value, type);
            var direct = S.Lambda<Func<object>>(S.Convert(constant, typeof(object)));
            var repeated = S.Lambda<Func<object[]>>(S.NewArrayInit(typeof(object),
                S.Convert(constant, typeof(object)), S.Convert(constant, typeof(object))));
            var withHandler = S.Lambda<Func<object>>(S.TryFinally(direct.Body, S.Empty()));
            var cachedWithHandler = S.Lambda<Func<object[]>>(S.TryFinally(repeated.Body, S.Empty()));
            // System IL uses ldstr for string-typed constants and can lose identity.
            // Its interpreter and a compiled object-bound conversion retain the
            // ConstantExpression.Value object, which is the semantic control here.
            var systemCompiled = type == typeof(string) ? string.Intern(value) : value;
            t.IsTrue(ReferenceEquals(systemCompiled, direct.Compile()()));
            t.IsTrue(ReferenceEquals(systemCompiled, withHandler.Compile()()));
            var expected = value;
#if !NETFRAMEWORK
            // Framework accepts preferInterpretation but still uses its IL compiler.
            t.IsTrue(ReferenceEquals(expected, direct.Compile(preferInterpretation: true)()));
            t.IsTrue(ReferenceEquals(expected, withHandler.Compile(preferInterpretation: true)()));
#endif
            t.IsTrue(ReferenceEquals(expected, S.Lambda<Func<object>>(S.Convert(Bound(value, type), typeof(object))).Compile()()));
            foreach (var flags in Flags)
            {
                var fast = direct.FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(fast)) t.IsTrue(ReferenceEquals(expected, fast()));
                var anonymous = withHandler.FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(anonymous)) t.IsTrue(ReferenceEquals(expected, anonymous()));
                var cached = repeated.FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(cached))
                {
                    var result = cached();
                    t.IsTrue(ReferenceEquals(expected, result[0]));
                    t.IsTrue(ReferenceEquals(expected, result[1]));
                }
                var cachedAnonymous = cachedWithHandler.FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(cachedAnonymous))
                {
                    var result = cachedAnonymous();
                    t.IsTrue(ReferenceEquals(expected, result[0]));
                    t.IsTrue(ReferenceEquals(expected, result[1]));
                }
            }
        }
    }

    private static void CheckExplicitBlocks(TestContext t)
    {
        foreach (var constantType in new[] { typeof(string), typeof(object), typeof(IComparable), typeof(ICloneable) })
        foreach (var blockType in new[] { typeof(string), typeof(object), typeof(IComparable), typeof(ICloneable) })
        {
            if (!blockType.IsAssignableFrom(constantType)) continue;
            var value = Fresh();
            var expected = value;
            for (var size = 2; size <= 8; ++size)
            {
                var expressions = new System.Linq.Expressions.Expression[size];
                for (var i = 0; i < size - 1; ++i) expressions[i] = S.Empty();
                expressions[size - 1] = S.Constant(value, constantType);
                var block = S.Block(blockType, expressions);
                var nested = S.Block(typeof(object), S.Empty(), S.Block(blockType, S.Empty(), block));
                var source = S.Lambda<Func<object>>(S.Convert(block, typeof(object)));
                var nestedSource = S.Lambda<Func<object>>(nested);
#if !NETFRAMEWORK
                t.IsTrue(ReferenceEquals(expected, source.Compile(preferInterpretation: true)()));
                t.IsTrue(ReferenceEquals(expected, nestedSource.Compile(preferInterpretation: true)()));
#endif
                var boundBlock = S.Block(blockType, S.Empty(), Bound(value, constantType));
                t.IsTrue(ReferenceEquals(expected, S.Lambda<Func<object>>(S.Convert(boundBlock, typeof(object))).Compile()()));
                foreach (var flags in Flags)
                {
                    var fast = source.FromSysExpression().CompileFast(true, flags);
                    if (t.IsNotNull(fast)) t.IsTrue(ReferenceEquals(expected, fast()));
                    var fastNested = nestedSource.FromSysExpression().CompileFast(true, flags);
                    if (t.IsNotNull(fastNested)) t.IsTrue(ReferenceEquals(expected, fastNested()));
                }
                var without = nestedSource.FromSysExpression().TryCompileWithoutClosure<Func<object>>();
                t.IsTrue(without == null);
            }
        }
    }

    public void Distinct_constants_and_nested_lambdas_preserve_identity(TestContext t)
    {
        CheckMixedComparisons(t);
        var first = Fresh(); var second = Fresh();
        var left = S.Constant(first, typeof(object)); var right = S.Constant(second, typeof(object));
        var comparison = S.Lambda<Func<bool>>(S.ReferenceEqual(left, right));
        var inner = S.Lambda<Func<object>>(left);
        var nested = S.Lambda<Func<Func<object>>>(S.TryFinally(inner, S.Empty()));
        var mixed = S.Lambda<Func<object[]>>(S.NewArrayInit(typeof(object),
            S.Block(typeof(object), S.Empty(), S.Constant(first, typeof(string))), left, left));
        t.IsTrue(!comparison.Compile()());
        t.IsTrue(ReferenceEquals(first, nested.Compile()()()));
        var mixedControl = S.Lambda<Func<object[]>>(S.NewArrayInit(typeof(object),
            S.Convert(Bound(first, typeof(string)), typeof(object)), left, left)).Compile()();
        t.IsTrue(ReferenceEquals(first, mixedControl[0]));
        t.IsTrue(ReferenceEquals(first, mixedControl[1]));
        t.IsTrue(ReferenceEquals(first, mixedControl[2]));
        foreach (var flags in Flags)
        {
            var fastComparison = comparison.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fastComparison)) t.IsTrue(!fastComparison());
            var fastNested = nested.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fastNested)) t.IsTrue(ReferenceEquals(first, fastNested()()));
            var fastMixed = mixed.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fastMixed))
            {
                var result = fastMixed();
                t.IsTrue(ReferenceEquals(first, result[0]));
                t.IsTrue(ReferenceEquals(first, result[1]));
                t.IsTrue(ReferenceEquals(first, result[2]));
            }
        }
    }

    private static void CheckMixedComparisons(TestContext t)
    {
        var first = Fresh(); var second = Fresh();
        foreach (var leftType in new[] { typeof(string), typeof(object), typeof(IComparable) })
        foreach (var rightType in new[] { typeof(string), typeof(object), typeof(IComparable) })
        foreach (var same in new[] { false, true })
        foreach (var negate in new[] { false, true })
        {
            var right = same ? first : second;
            var leftExpression = S.Constant(first, leftType); var rightExpression = S.Constant(right, rightType);
            var body = negate ? S.ReferenceNotEqual(leftExpression, rightExpression) : S.ReferenceEqual(leftExpression, rightExpression);
            var source = S.Lambda<Func<bool>>(body);
            var expected = same != negate;
#if !NETFRAMEWORK
            t.AreEqual(expected, source.Compile(preferInterpretation: true)());
#endif
            var bound = negate ? S.ReferenceNotEqual(Bound(first, leftType), Bound(right, rightType)) :
                S.ReferenceEqual(Bound(first, leftType), Bound(right, rightType));
            t.AreEqual(expected, S.Lambda<Func<bool>>(bound).Compile()());
            foreach (var flags in Flags)
            {
                var fast = source.FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(fast)) t.AreEqual(expected, fast());
            }
        }
    }

    public void Explicit_closure_compilation_preserves_string_identity(TestContext t)
    {
        foreach (var type in new[] { typeof(string), typeof(object), typeof(IComparable), typeof(ICloneable) })
        {
            var value = Fresh();
            var source = S.Lambda<Func<object>>(S.Convert(S.Constant(value, type), typeof(object))).FromSysExpression();
            // Without a supplied constant reference, ldstr cannot reproduce this particular object.
            t.IsTrue(source.TryCompileWithoutClosure<Func<object>>() == null);
            var supplied = source.TryCompileWithPreCreatedClosure<Func<object>>(F.Constant(value, type));
            if (t.IsNotNull(supplied)) t.IsTrue(ReferenceEquals(value, supplied()));
            var interned = string.Intern(value);
            var literalReference = S.Lambda<Func<object>>(S.Convert(S.Constant(interned, type), typeof(object)))
                .FromSysExpression().TryCompileWithoutClosure<Func<object>>();
            if (t.IsNotNull(literalReference)) t.IsTrue(ReferenceEquals(interned, literalReference()));
        }
        var literal = S.Lambda<Func<string>>(S.Constant(Literal)).FromSysExpression().TryCompileWithoutClosure<Func<string>>();
        if (t.IsNotNull(literal)) t.AreEqual("fec-string-constant-identity", literal());
        var field = S.Lambda<Func<string>>(S.Field(null, typeof(StringConstantIdentityRegressionTests).GetField(nameof(Literal))));
        t.AreEqual(Literal, field.Compile()());
        foreach (var flags in Flags)
        {
            var compiledField = field.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(compiledField)) t.AreEqual(Literal, compiledField());
        }
    }
}

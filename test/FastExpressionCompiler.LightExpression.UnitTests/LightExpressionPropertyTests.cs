using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using FastExpressionCompiler.FlatExpression;
using static FastExpressionCompiler.LightExpression.Expression;
#if NET8_0_OR_GREATER
using CsCheck;
#endif

namespace FastExpressionCompiler.LightExpression.UnitTests;

public partial class LightExpressionTests
{
#if NET8_0_OR_GREATER
    public void Can_property_test_generated_flat_expression_roundtrip_structurally()
    {
        Gen.Int[0, int.MaxValue / 2]
            .Select(seed => new GeneratedCase(seed, GeneratedIntSpecFactory.Create(seed, maxDepth: 3, maxBreadth: 3)))
            .Sample(testCase =>
                GeneratedExpressionComparer.AreEqual(
                    CreateGeneratedLightExpression(testCase.Spec),
                    CreateGeneratedFlatExpression(testCase.Spec).ToLightExpression()),
                iter: 100,
                threads: 1,
                seed: "0N0XIzNsQ0O2",
                print: testCase => $"{testCase.Seed}: {testCase.Spec}");
    }

    private static FastExpressionCompiler.LightExpression.Expression<Func<int, int>> CreateGeneratedLightExpression(IntSpec spec)
    {
        var parameter = ParameterOf<int>("p");
        return Lambda<Func<int, int>>(BuildLightInt(spec, [parameter]), parameter);
    }

    private static ExprTree CreateGeneratedFlatExpression(IntSpec spec)
    {
        var fe = default(ExprTree);
        var parameter = fe.ParameterOf<int>("p");
        fe.RootIndex = fe.Lambda<Func<int, int>>(BuildFlatInt(ref fe, spec, [parameter]), parameter);
        return fe;
    }

    private static FastExpressionCompiler.LightExpression.Expression BuildLightInt(IntSpec spec, FastExpressionCompiler.LightExpression.ParameterExpression[] ints) =>
        spec switch
        {
            IntSpec.ParameterRef parameter => ints[parameter.Index],
            IntSpec.Constant constant => Constant(constant.Value),
            IntSpec.Add add => Add(BuildLightInt(add.Left, ints), BuildLightInt(add.Right, ints)),
            IntSpec.Subtract subtract => Subtract(BuildLightInt(subtract.Left, ints), BuildLightInt(subtract.Right, ints)),
            IntSpec.Multiply multiply => Multiply(BuildLightInt(multiply.Left, ints), BuildLightInt(multiply.Right, ints)),
            IntSpec.Conditional conditional => Condition(
                BuildLightBool(conditional.Test, ints),
                BuildLightInt(conditional.IfTrue, ints),
                BuildLightInt(conditional.IfFalse, ints)),
            IntSpec.LetMany letMany => BuildLightBlock(letMany, ints),
            _ => throw new NotSupportedException(spec.GetType().Name)
        };

    private static int BuildFlatInt(ref ExprTree fe, IntSpec spec, int[] ints) =>
        spec switch
        {
            IntSpec.ParameterRef parameter => ints[parameter.Index],
            IntSpec.Constant constant => fe.ConstantInt(constant.Value),
            IntSpec.Add add => fe.Add(BuildFlatInt(ref fe, add.Left, ints), BuildFlatInt(ref fe, add.Right, ints)),
            IntSpec.Subtract subtract => fe.MakeBinary(ExpressionType.Subtract,
                BuildFlatInt(ref fe, subtract.Left, ints), BuildFlatInt(ref fe, subtract.Right, ints)),
            IntSpec.Multiply multiply => fe.MakeBinary(ExpressionType.Multiply,
                BuildFlatInt(ref fe, multiply.Left, ints), BuildFlatInt(ref fe, multiply.Right, ints)),
            IntSpec.Conditional conditional => fe.Condition(
                BuildFlatBool(ref fe, conditional.Test, ints),
                BuildFlatInt(ref fe, conditional.IfTrue, ints),
                BuildFlatInt(ref fe, conditional.IfFalse, ints)),
            IntSpec.LetMany letMany => BuildFlatBlock(ref fe, letMany, ints),
            _ => throw new NotSupportedException(spec.GetType().Name)
        };

    private static FastExpressionCompiler.LightExpression.Expression BuildLightBool(BoolSpec spec, FastExpressionCompiler.LightExpression.ParameterExpression[] ints) =>
        spec switch
        {
            BoolSpec.Constant constant => Constant(constant.Value),
            BoolSpec.Not not => Not(BuildLightBool(not.Operand, ints)),
            BoolSpec.Equal equal => Equal(BuildLightInt(equal.Left, ints), BuildLightInt(equal.Right, ints)),
            BoolSpec.GreaterThan greaterThan => GreaterThan(BuildLightInt(greaterThan.Left, ints), BuildLightInt(greaterThan.Right, ints)),
            BoolSpec.AndAlso andAlso => AndAlso(BuildLightBool(andAlso.Left, ints), BuildLightBool(andAlso.Right, ints)),
            BoolSpec.OrElse orElse => OrElse(BuildLightBool(orElse.Left, ints), BuildLightBool(orElse.Right, ints)),
            _ => throw new NotSupportedException(spec.GetType().Name)
        };

    private static int BuildFlatBool(ref ExprTree fe, BoolSpec spec, int[] ints) =>
        spec switch
        {
            BoolSpec.Constant constant => fe.ConstantOf(constant.Value),
            BoolSpec.Not not => fe.Not(BuildFlatBool(ref fe, not.Operand, ints)),
            BoolSpec.Equal equal => fe.Equal(BuildFlatInt(ref fe, equal.Left, ints), BuildFlatInt(ref fe, equal.Right, ints)),
            BoolSpec.GreaterThan greaterThan => fe.MakeBinary(ExpressionType.GreaterThan,
                BuildFlatInt(ref fe, greaterThan.Left, ints), BuildFlatInt(ref fe, greaterThan.Right, ints)),
            BoolSpec.AndAlso andAlso => fe.MakeBinary(ExpressionType.AndAlso,
                BuildFlatBool(ref fe, andAlso.Left, ints), BuildFlatBool(ref fe, andAlso.Right, ints)),
            BoolSpec.OrElse orElse => fe.MakeBinary(ExpressionType.OrElse,
                BuildFlatBool(ref fe, orElse.Left, ints), BuildFlatBool(ref fe, orElse.Right, ints)),
            _ => throw new NotSupportedException(spec.GetType().Name)
        };

    private static FastExpressionCompiler.LightExpression.Expression BuildLightBlock(IntSpec.LetMany letMany, FastExpressionCompiler.LightExpression.ParameterExpression[] ints)
    {
        var locals = new FastExpressionCompiler.LightExpression.ParameterExpression[letMany.Values.Length];
        var expressions = new FastExpressionCompiler.LightExpression.Expression[letMany.Values.Length + 1];
        for (var i = 0; i < locals.Length; ++i)
        {
            locals[i] = Variable(typeof(int), $"v{i}");
            expressions[i] = Assign(locals[i], BuildLightInt(letMany.Values[i], ints));
        }

        expressions[locals.Length] = BuildLightInt(letMany.Body, Append(ints, locals));
        return Block(locals, expressions);
    }

    private static int BuildFlatBlock(ref ExprTree fe, IntSpec.LetMany letMany, int[] ints)
    {
        var locals = new int[letMany.Values.Length];
        var expressions = new int[letMany.Values.Length + 1];
        for (var i = 0; i < locals.Length; ++i)
        {
            locals[i] = fe.Variable(typeof(int), $"v{i}");
            expressions[i] = fe.Assign(locals[i], BuildFlatInt(ref fe, letMany.Values[i], ints));
        }

        expressions[locals.Length] = BuildFlatInt(ref fe, letMany.Body, Append(ints, locals));
        return fe.Block(typeof(int), locals, expressions);
    }

    private static T[] Append<T>(T[] source, T[] append)
    {
        var result = new T[source.Length + append.Length];
        Array.Copy(source, result, source.Length);
        Array.Copy(append, 0, result, source.Length, append.Length);
        return result;
    }

    private readonly record struct GeneratedCase(int Seed, IntSpec Spec);

    private abstract record IntSpec
    {
        public sealed record ParameterRef(int Index) : IntSpec;
        public sealed record Constant(int Value) : IntSpec;
        public sealed record Add(IntSpec Left, IntSpec Right) : IntSpec;
        public sealed record Subtract(IntSpec Left, IntSpec Right) : IntSpec;
        public sealed record Multiply(IntSpec Left, IntSpec Right) : IntSpec;
        public sealed record Conditional(BoolSpec Test, IntSpec IfTrue, IntSpec IfFalse) : IntSpec;
        public sealed record LetMany(IntSpec[] Values, IntSpec Body) : IntSpec;
    }

    private abstract record BoolSpec
    {
        public sealed record Constant(bool Value) : BoolSpec;
        public sealed record Not(BoolSpec Operand) : BoolSpec;
        public sealed record Equal(IntSpec Left, IntSpec Right) : BoolSpec;
        public sealed record GreaterThan(IntSpec Left, IntSpec Right) : BoolSpec;
        public sealed record AndAlso(BoolSpec Left, BoolSpec Right) : BoolSpec;
        public sealed record OrElse(BoolSpec Left, BoolSpec Right) : BoolSpec;
    }

    private static class GeneratedIntSpecFactory
    {
        public static IntSpec Create(int seed, int maxDepth, int maxBreadth) =>
            NextInt(new Random(seed), maxDepth, envIntCount: 1, maxBreadth);

        private static IntSpec NextInt(Random random, int depth, int envIntCount, int maxBreadth)
        {
            if (depth <= 0)
                return NextIntLeaf(random, envIntCount);

            switch (random.Next(7))
            {
                case 0: return NextIntLeaf(random, envIntCount);
                case 1: return new IntSpec.Add(NextInt(random, depth - 1, envIntCount, maxBreadth), NextInt(random, depth - 1, envIntCount, maxBreadth));
                case 2: return new IntSpec.Subtract(NextInt(random, depth - 1, envIntCount, maxBreadth), NextInt(random, depth - 1, envIntCount, maxBreadth));
                case 3: return new IntSpec.Multiply(NextInt(random, depth - 1, envIntCount, maxBreadth), NextInt(random, depth - 1, envIntCount, maxBreadth));
                case 4: return new IntSpec.Conditional(
                    NextBool(random, depth - 1, envIntCount, maxBreadth),
                    NextInt(random, depth - 1, envIntCount, maxBreadth),
                    NextInt(random, depth - 1, envIntCount, maxBreadth));
                case 5: return NextLetMany(random, depth - 1, envIntCount, maxBreadth);
                default: return new IntSpec.Constant(random.Next(-8, 9));
            }
        }

        private static IntSpec NextIntLeaf(Random random, int envIntCount) =>
            random.Next(3) == 0
                ? new IntSpec.Constant(random.Next(-8, 9))
                : new IntSpec.ParameterRef(random.Next(envIntCount));

        private static IntSpec NextLetMany(Random random, int depth, int envIntCount, int maxBreadth)
        {
            var count = random.Next(1, maxBreadth + 1);
            var values = new IntSpec[count];
            for (var i = 0; i < count; ++i)
                values[i] = NextInt(random, depth, envIntCount, maxBreadth);
            return new IntSpec.LetMany(values, NextInt(random, depth, envIntCount + count, maxBreadth));
        }

        private static BoolSpec NextBool(Random random, int depth, int envIntCount, int maxBreadth)
        {
            if (depth <= 0)
                return NextBoolLeaf(random, envIntCount);

            switch (random.Next(6))
            {
                case 0: return NextBoolLeaf(random, envIntCount);
                case 1: return new BoolSpec.Not(NextBool(random, depth - 1, envIntCount, maxBreadth));
                case 2: return new BoolSpec.Equal(NextInt(random, depth - 1, envIntCount, maxBreadth), NextInt(random, depth - 1, envIntCount, maxBreadth));
                case 3: return new BoolSpec.GreaterThan(NextInt(random, depth - 1, envIntCount, maxBreadth), NextInt(random, depth - 1, envIntCount, maxBreadth));
                case 4: return new BoolSpec.AndAlso(NextBool(random, depth - 1, envIntCount, maxBreadth), NextBool(random, depth - 1, envIntCount, maxBreadth));
                default: return new BoolSpec.OrElse(NextBool(random, depth - 1, envIntCount, maxBreadth), NextBool(random, depth - 1, envIntCount, maxBreadth));
            }
        }

        private static BoolSpec NextBoolLeaf(Random random, int envIntCount) =>
            random.Next(2) == 0
                ? new BoolSpec.Constant(random.Next(2) == 0)
                : new BoolSpec.Equal(NextIntLeaf(random, envIntCount), NextIntLeaf(random, envIntCount));
    }

    private sealed class GeneratedExpressionComparer
    {
        private readonly List<FastExpressionCompiler.LightExpression.ParameterExpression> _xs = new();
        private readonly List<FastExpressionCompiler.LightExpression.ParameterExpression> _ys = new();

        public static bool AreEqual(FastExpressionCompiler.LightExpression.Expression x, FastExpressionCompiler.LightExpression.Expression y) => new GeneratedExpressionComparer().Eq(x, y);

        private bool Eq(FastExpressionCompiler.LightExpression.Expression x, FastExpressionCompiler.LightExpression.Expression y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x == null || y == null || x.NodeType != y.NodeType || x.Type != y.Type)
                return false;

            return x.NodeType switch
            {
                ExpressionType.Lambda => EqLambda((FastExpressionCompiler.LightExpression.LambdaExpression)x, (FastExpressionCompiler.LightExpression.LambdaExpression)y),
                ExpressionType.Parameter => EqParameter((FastExpressionCompiler.LightExpression.ParameterExpression)x, (FastExpressionCompiler.LightExpression.ParameterExpression)y),
                ExpressionType.Constant => Equals(((FastExpressionCompiler.LightExpression.ConstantExpression)x).Value, ((FastExpressionCompiler.LightExpression.ConstantExpression)y).Value),
                ExpressionType.Not => Eq(((FastExpressionCompiler.LightExpression.UnaryExpression)x).Operand, ((FastExpressionCompiler.LightExpression.UnaryExpression)y).Operand),
                ExpressionType.Add or ExpressionType.Subtract or ExpressionType.Multiply or ExpressionType.Assign
                    or ExpressionType.Equal or ExpressionType.GreaterThan or ExpressionType.AndAlso or ExpressionType.OrElse
                    => EqBinary((FastExpressionCompiler.LightExpression.BinaryExpression)x, (FastExpressionCompiler.LightExpression.BinaryExpression)y),
                ExpressionType.Conditional => EqConditional((FastExpressionCompiler.LightExpression.ConditionalExpression)x, (FastExpressionCompiler.LightExpression.ConditionalExpression)y),
                ExpressionType.Block => EqBlock((FastExpressionCompiler.LightExpression.BlockExpression)x, (FastExpressionCompiler.LightExpression.BlockExpression)y),
                _ => throw new NotSupportedException(x.NodeType.ToString())
            };
        }

        private bool EqLambda(FastExpressionCompiler.LightExpression.LambdaExpression x, FastExpressionCompiler.LightExpression.LambdaExpression y)
        {
            if (x.Parameters.Count != y.Parameters.Count)
                return false;

            var start = _xs.Count;
            for (var i = 0; i < x.Parameters.Count; ++i)
            {
                _xs.Add(x.Parameters[i]);
                _ys.Add(y.Parameters[i]);
            }

            var equal = Eq(x.Body, y.Body);
            _xs.RemoveRange(start, _xs.Count - start);
            _ys.RemoveRange(start, _ys.Count - start);
            return equal;
        }

        private bool EqParameter(FastExpressionCompiler.LightExpression.ParameterExpression x, FastExpressionCompiler.LightExpression.ParameterExpression y)
        {
            for (var i = _xs.Count - 1; i >= 0; --i)
            {
                var xMatches = ReferenceEquals(_xs[i], x);
                var yMatches = ReferenceEquals(_ys[i], y);
                if (xMatches || yMatches)
                    return xMatches && yMatches;
            }

            return x.Name == y.Name;
        }

        private bool EqBinary(FastExpressionCompiler.LightExpression.BinaryExpression x, FastExpressionCompiler.LightExpression.BinaryExpression y) =>
            x.Method == y.Method && Eq(x.Left, y.Left) && Eq(x.Right, y.Right);

        private bool EqConditional(FastExpressionCompiler.LightExpression.ConditionalExpression x, FastExpressionCompiler.LightExpression.ConditionalExpression y) =>
            Eq(x.Test, y.Test) && Eq(x.IfTrue, y.IfTrue) && Eq(x.IfFalse, y.IfFalse);

        private bool EqBlock(FastExpressionCompiler.LightExpression.BlockExpression x, FastExpressionCompiler.LightExpression.BlockExpression y)
        {
            if (x.Variables.Count != y.Variables.Count || x.Expressions.Count != y.Expressions.Count)
                return false;

            var start = _xs.Count;
            for (var i = 0; i < x.Variables.Count; ++i)
            {
                _xs.Add(x.Variables[i]);
                _ys.Add(y.Variables[i]);
            }

            var equal = true;
            for (var i = 0; equal && i < x.Expressions.Count; ++i)
                equal = Eq(x.Expressions[i], y.Expressions[i]);

            _xs.RemoveRange(start, _xs.Count - start);
            _ys.RemoveRange(start, _ys.Count - start);
            return equal;
        }
    }
#else
    public void Can_property_test_generated_flat_expression_roundtrip_structurally() { }
#endif
}

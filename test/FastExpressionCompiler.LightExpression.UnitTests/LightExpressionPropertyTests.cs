using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using FastExpressionCompiler.FlatExpression;
using SysExpr = System.Linq.Expressions.Expression;
using SysSwitchCase = System.Linq.Expressions.SwitchCase;
using SysCatchBlock = System.Linq.Expressions.CatchBlock;
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

    /// <summary>The flat-native collect+emit must agree with System.Linq on the generated int/bool trees and must not fall back.</summary>
    public void Can_compile_generated_flat_expression_natively()
    {
        Gen.Int[0, int.MaxValue / 2].Sample(seed =>
            {
                var spec = GeneratedIntSpecFactory.Create(seed, maxDepth: 3, maxBreadth: 3);
                var fe = CreateGeneratedFlatExpression(spec);
                Ensure(fe.TryCollectFlatInfo() == ExpressionCompiler.Result.OK, "flat collect");
                var flat = fe.TryCompileFlat<Func<int, int>>();
                Ensure(flat != null, "flat native compile");
                var expected = CreateGeneratedLightExpression(spec).CompileSys();
                for (var arg = -3; arg <= 3; ++arg)
                    Ensure(flat(arg) == expected(arg), $"result for {arg}");
                return true;
            },
            iter: 300, threads: 1, seed: "0N0XIzNsQ0O2", print: seed => $"{seed}: {GeneratedIntSpecFactory.Create(seed, 3, 3)}");
    }

    /// <summary>Prints how long the phases of the compilation take on the flat and on the light trees; asserts nothing but the agreement.</summary>
    public void Experiment_compare_flat_and_light_compile_phases()
    {
        const int n = 200, reps = 20;
        var flats = new ExprTree[n];
        var lights = new FastExpressionCompiler.LightExpression.Expression<Func<int, int>>[n];
        var nodes = 0;
        for (var i = 0; i < n; ++i)
        {
            var spec = GeneratedIntSpecFactory.Create(i + 1, maxDepth: 4, maxBreadth: 3);
            flats[i] = CreateGeneratedFlatExpression(spec);
            lights[i] = CreateGeneratedLightExpression(spec);
            nodes += flats[i].Nodes.Count;
        }

        void Time(string name, int repeat, Action<int> body)
        {
            for (var i = 0; i < n; ++i) body(i); // warm up
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (var r = 0; r < repeat; ++r)
                for (var i = 0; i < n; ++i) body(i);
            sw.Stop();
            Console.WriteLine($"//flat-vs-light {name,-34} {sw.Elapsed.TotalMilliseconds * 1000 / (n * repeat),9:F2} us/expr");
        }

        Console.WriteLine($"//flat-vs-light {n} exprs, {nodes / n} flat nodes avg");
        Time("collect: flat linear scan", reps * 10, i => flats[i].TryCollectFlatInfo());
        Time("collect: light TryCollectInfo", reps * 10, i =>
        {
            var ctx = new ExpressionCompiler.CompilerContext(ExpressionCompiler.ClosureStatus.ToBeCollected, (FastExpressionCompiler.LightExpression.IParameterProvider)lights[i], CompilerFlags.Default);
            ExpressionCompiler.TryCollectInfo(ref ctx, lights[i].Body, null, ref ctx.NestedLambdas);
        });
        Time("compile: flat native (collect+emit)", reps, i => flats[i].TryCompileFlat<Func<int, int>>());
        Time("compile: light FEC (collect+emit)", reps, i => lights[i].CompileFast(ifFastFailedReturnNull: true));
        Time("compile: flat->sys->light->FEC", reps, i => ((FastExpressionCompiler.LightExpression.LambdaExpression)flats[i].ToLightExpression()).CompileFast<Func<int, int>>(true));
        Time("convert only: flat->sys->light", reps, i => flats[i].ToLightExpression());
        Time("compile: System.Compile", 1, i => lights[i].CompileSys());
    }

    /// <summary>Fuzzes presence/absence of every optional child (switch default/cases, try finally/catch, catch variable/filter,
    /// loop break/continue, goto value, label default): the bits decide what is present; the flat tree must report exactly that.</summary>
    public void Can_fuzz_optional_children_presence_in_flat_expression()
    {
        Gen.Int[0, (1 << 10) - 1].Sample(
            bits => CheckOptionalChildren(bits),
            iter: 300, threads: 1, seed: "0N0XIzNsQ0O2", print: bits => System.Convert.ToString(bits, 2).PadLeft(10, '0'));
    }

    private static bool CheckOptionalChildren(int bits)
    {
        var swDefault = (bits & 1) != 0;
        var swCases = (bits >> 1 & 3) % 3;
        if (!swDefault && swCases == 0) swCases = 1; // switch needs a default or at least one case
        var hasFinally = (bits & 8) != 0;
        var hasCatch = (bits & 16) != 0 || !hasFinally; // try needs a catch or a finally
        var catchVar = hasCatch && (bits & 32) != 0;
        var catchFilter = hasCatch && (bits & 64) != 0;
        var loopBreak = (bits & 128) != 0;
        var loopContinue = (bits & 256) != 0;
        var gotoValue = (bits & 512) != 0;
        var labelDefault = gotoValue; // a non-void label needs its default value

        var p = SysExpr.Parameter(typeof(int), "p");
        var r = SysExpr.Variable(typeof(int), "r");
        var t = SysExpr.Variable(typeof(int), "t");
        var i = SysExpr.Variable(typeof(int), "i");
        var g = SysExpr.Variable(typeof(int), "g");
        var fin = SysExpr.Variable(typeof(int), "fin");
        SysExpr Int(int x) => SysExpr.Constant(x);

        var cases = new SysSwitchCase[swCases];
        for (var k = 0; k < cases.Length; ++k)
            cases[k] = SysExpr.SwitchCase(SysExpr.Assign(r, Int(10 + k)), Int(k));
        var @switch = SysExpr.Switch(typeof(void), p, swDefault ? SysExpr.Assign(r, Int(7)) : null, null, cases);

        var tryBody = SysExpr.Condition(SysExpr.LessThan(p, Int(0)), SysExpr.Throw(SysExpr.New(typeof(Exception)), typeof(int)), p);
        var filter = catchFilter ? SysExpr.Equal(Int(1), Int(1)) : null;
        var handlers = !hasCatch ? Array.Empty<SysCatchBlock>() : new[]
        {
            catchVar ? SysExpr.Catch(SysExpr.Variable(typeof(Exception), "ex"), Int(-1), filter) : SysExpr.Catch(typeof(Exception), Int(-1), filter)
        };
        var @finally = SysExpr.Assign(fin, Int(1));
        var @try = !hasFinally ? SysExpr.TryCatch(tryBody, handlers)
            : hasCatch ? SysExpr.TryCatchFinally(tryBody, @finally, handlers)
            : SysExpr.TryFinally(tryBody, @finally);

        var brk = loopBreak ? SysExpr.Label("brk") : null;
        var cont = loopContinue ? SysExpr.Label("cont") : null;
        var exit = SysExpr.Label("exit");
        var loop = SysExpr.Block(
            SysExpr.Loop(SysExpr.Block(
                SysExpr.Assign(i, SysExpr.Add(i, Int(1))),
                cont != null ? SysExpr.IfThen(SysExpr.LessThan(i, Int(2)), SysExpr.Continue(cont)) : SysExpr.Empty(),
                SysExpr.IfThen(SysExpr.GreaterThanOrEqual(i, Int(3)), loopBreak ? SysExpr.Break(brk) : SysExpr.Goto(exit))), brk, cont),
            SysExpr.Label(exit));

        var l = SysExpr.Label(gotoValue ? typeof(int) : typeof(void), "l");
        var gotoLabel = gotoValue
            ? SysExpr.Block(SysExpr.Goto(l, Int(5)), SysExpr.Label(l, labelDefault ? Int(9) : null))
            : SysExpr.Block(typeof(int), SysExpr.Goto(l), SysExpr.Label(l), Int(3));

        var lambda = SysExpr.Lambda<Func<int, int>>(
            SysExpr.Block(typeof(int), new[] { r, t, i, g, fin },
                @switch, SysExpr.Assign(t, @try), loop, SysExpr.Assign(g, gotoLabel),
                SysExpr.Add(SysExpr.Add(r, t), SysExpr.Add(SysExpr.Add(i, g), fin))),
            p);

        var fe = lambda.ToFlatExpression();
        var back2 = ((System.Linq.Expressions.Expression<Func<int, int>>)fe.ToExpression()).ToFlatExpression();
        Ensure(fe.Equals(back2), "structural roundtrip: Equals");
        Ensure(fe.GetHashCode() == back2.GetHashCode(), "structural roundtrip: GetHashCode");

        int Count(Func<ExprNode, bool> match)
        {
            var n = 0;
            for (var k = 1; k < fe.Nodes.Count; ++k)
                if (match(fe.Nodes[k])) ++n;
            return n;
        }
        bool Is(ExprNode n, ExpressionType type, int childCount) =>
            n.Kind == ExprNodeKind.Expression && n.NodeType == type && n.ChildCount == childCount;

        Ensure(Count(n => Is(n, ExpressionType.Switch, 1 + (swDefault ? 1 : 0) + (swCases > 0 ? 1 : 0))) == 1, "switch children");
        Ensure(Count(n => Is(n, ExpressionType.Loop, 1 + (loopBreak ? 1 : 0) + (loopContinue ? 1 : 0))) == 1, "loop children");
        Ensure(Count(n => Is(n, ExpressionType.Try, 1 + (hasFinally ? 1 : 0) + (hasCatch ? 1 : 0))) == 1, "try children");
        Ensure(Count(n => n.Kind == ExprNodeKind.CatchBlock && n.ChildCount == 1 + (catchVar ? 1 : 0) + (catchFilter ? 1 : 0)) == (hasCatch ? 1 : 0), "catch children");
        Ensure(Count(n => Is(n, ExpressionType.Goto, 2)) == (gotoValue ? 1 : 0), "goto value");
        Ensure(Count(n => Is(n, ExpressionType.Label, 2)) == (labelDefault ? 1 : 0), "label default");

        if (catchFilter) return true; // FEC does not support catch filters, the structure is checked above

        var fast = fe.ToLightExpression() is FastExpressionCompiler.LightExpression.LambdaExpression fl
            ? fl.CompileFast<Func<int, int>>(ifFastFailedReturnNull: true) : null;
        Ensure(fast != null, "CompileFast failed");
        var expected = lambda.Compile();
        // an uncaught throw (no catch) must happen in both
        static int Run(Func<int, int> f, int arg) { try { return f(arg); } catch (Exception) { return int.MinValue; } }
        foreach (var arg in new[] { -1, 0, 1, 2, 5 })
            Ensure(Run(fast, arg) == Run(expected, arg), $"result for {arg}");
        return true;
    }

    private static void Ensure(bool ok, string what)
    {
        if (!ok) throw new InvalidOperationException("Property failed: " + what);
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
        fe.RootIdx = fe.Lambda<Func<int, int>>(BuildFlatInt(ref fe, spec, [parameter]), parameter);
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

    private static ushort BuildFlatInt(ref ExprTree fe, IntSpec spec, ushort[] ints) =>
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

    private static ushort BuildFlatBool(ref ExprTree fe, BoolSpec spec, ushort[] ints) =>
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

    private static ushort BuildFlatBlock(ref ExprTree fe, IntSpec.LetMany letMany, ushort[] ints)
    {
        var locals = new ushort[letMany.Values.Length];
        var expressions = new ushort[letMany.Values.Length + 1];
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
    public void Can_fuzz_optional_children_presence_in_flat_expression() { }
    public void Can_compile_generated_flat_expression_natively() { }
    public void Experiment_compare_flat_and_light_compile_phases() { }
#endif
}

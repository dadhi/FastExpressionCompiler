using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using FastExpressionCompiler.FlatExpression;
using FlatCompiler = FastExpressionCompiler.FlatExpression.ExpressionCompiler;
using SysExpr = System.Linq.Expressions.Expression;
using SysSwitchCase = System.Linq.Expressions.SwitchCase;
using SysCatchBlock = System.Linq.Expressions.CatchBlock;
using static FastExpressionCompiler.LightExpression.Expression;
using CsCheck;

namespace FastExpressionCompiler.LightExpression.UnitTests;

public partial class LightExpressionTests
{
    public void Can_property_test_generated_flat_expression_roundtrip_structurally()
    {
        Gen.Int[0, int.MaxValue / 2]
            .Select(seed => new GeneratedCase(seed, GeneratedIntSpecFactory.Create(seed, maxDepth: 3, maxBreadth: 3)))
            .Sample(testCase =>
            {
                var built = default(ExprTree);
                FillFlat(ref built, testCase.Spec);
                GeneratedExpressionComparer.AreEqual(
                    CreateGeneratedLightExpression(testCase.Spec),
                    built.ToExpression().ToLightExpression());
            },
                iter: 100,
                threads: 1,
                seed: "0N0XIzNsQ0O2",
                print: testCase => $"{testCase.Seed}: {testCase.Spec}");
    }

    /// <summary>Generated int trees and nested wrappers: Flat TryCollect matches Light on result, constants, nested count, HasClosure.</summary>
    public void Can_property_test_flat_try_collect_matches_light()
    {
        Gen.Int[0, int.MaxValue / 2]
            .Select(seed => new CollectCase(seed, GeneratedIntSpecFactory.Create(seed, maxDepth: 3, maxBreadth: 3), Nested: (seed & 1) != 0))
            .Sample(testCase =>
            {
                if (testCase.Nested)
                {
                    var light = CreateGeneratedLightNested(testCase.Spec);
                    var fe = default(ExprTree);
                    FillFlatNested(ref fe, testCase.Spec);
                    AssertCollectMatch(light, ref fe, testCase.Seed + " nested");
                    return;
                }

                var lightExpr = CreateGeneratedLightExpression(testCase.Spec);
                var native = default(ExprTree);
                FillFlat(ref native, testCase.Spec);
                AssertCollectMatch(lightExpr, ref native, testCase.Seed.ToString());
                var roundtrip = default(ExprTree);
                lightExpr.ToExpression().ToFlatExpression(ref roundtrip);
                AssertCollectMatch(lightExpr, ref roundtrip, testCase.Seed + " roundtrip");
            },
            iter: 100,
            threads: 1,
            seed: "0N0XIzNsQ0O2",
            print: testCase => $"{testCase.Seed} nested={testCase.Nested}: {testCase.Spec}");
    }

    /// <summary>Arithmetic, compare, and closure constants emitted from the flat tree match Light compile.</summary>
    public void Can_property_test_flat_try_emit_matches_light()
    {
        var pFe = default(ExprTree);
        var p = pFe.ParameterOf<int>("p");
        pFe.RootIdx = pFe.Lambda<Func<int, bool>>(
            pFe.MakeBinary(ExpressionType.AndAlso,
                pFe.MakeBinary(ExpressionType.GreaterThan, p, pFe.ConstantInt(0)),
                pFe.Not(pFe.Equal(p, pFe.ConstantInt(2)))),
            p);
        var pred = FlatCompiler.CompileFast<Func<int, bool>>(ref pFe, ifFastFailedReturnNull: true);
        var expectedPred = ((LambdaExpression)pFe.ToExpression().ToLightExpression()).CompileFast<Func<int, bool>>(true);
        Ensure(pred != null && expectedPred != null, "bool emit");
        foreach (var arg in new[] { -2, 0, 1, 2, 5 })
            Ensure(pred(arg) == expectedPred(arg), $"bool {arg}");

        var box = new string('q', 3);
        var constFe = default(ExprTree);
        constFe.RootIdx = constFe.Lambda<Func<string>>(constFe.Constant(box));
        var got = FlatCompiler.CompileFast<Func<string>>(ref constFe, true);
        Ensure(got != null && ReferenceEquals(got(), box), "closure constant");

        var callFe = default(ExprTree);
        var cp = callFe.ParameterOf<int>("p");
        var abs = typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) });
        callFe.RootIdx = callFe.Lambda<Func<int, int>>(callFe.Call(abs, cp), cp);
        var callFast = FlatCompiler.CompileFast<Func<int, int>>(ref callFe, true);
        var callSlow = ((LambdaExpression)callFe.ToExpression().ToLightExpression()).CompileFast<Func<int, int>>(true);
        Ensure(callFast != null && callFast(-4) == callSlow(-4), "call");

        var arrFe = default(ExprTree);
        var ap = arrFe.ParameterOf<int>("p");
        arrFe.RootIdx = arrFe.Lambda<Func<int, int>>(
            arrFe.ArrayIndex(arrFe.NewArrayInit(typeof(int), ap, arrFe.ConstantInt(3)), arrFe.ConstantInt(1)), ap);
        var arrFast = FlatCompiler.CompileFast<Func<int, int>>(ref arrFe, true);
        Ensure(arrFast != null && arrFast(9) == 3, "array");

        var lenFe = default(ExprTree);
        var sp = lenFe.ParameterOf<string>("s");
        lenFe.RootIdx = lenFe.Lambda<Func<string, int>>(lenFe.Property(sp, typeof(string).GetProperty(nameof(string.Length))), sp);
        var lenFast = FlatCompiler.CompileFast<Func<string, int>>(ref lenFe, true);
        Ensure(lenFast != null && lenFast("abcd") == 4, "member");

        var blockFe = default(ExprTree);
        var bp = blockFe.ParameterOf<int>("p");
        var bv = blockFe.Variable(typeof(int), "v");
        blockFe.RootIdx = blockFe.Lambda<Func<int, int>>(
            blockFe.Block(typeof(int), new[] { bv }, blockFe.Assign(bv, blockFe.Add(bp, blockFe.ConstantInt(1))), bv), bp);
        var blockFast = FlatCompiler.CompileFast<Func<int, int>>(ref blockFe, true);
        var blockSlow = ((LambdaExpression)blockFe.ToExpression().ToLightExpression()).CompileFast<Func<int, int>>(true);
        Ensure(blockFast != null && blockFast(10) == blockSlow(10), "block");

        var capFe = default(ExprTree);
        var capP = capFe.ParameterOf<int>("p");
        capFe.RootIdx = capFe.Lambda<Func<int, Func<int>>>(capFe.Lambda<Func<int>>(capFe.Add(capP, capFe.ConstantInt(1))), capP);
        var capFast = FlatCompiler.CompileFast<Func<int, Func<int>>>(ref capFe, true);
        Ensure(capFast != null && capFast(3)() == 4, "capture");

        var plainFe = default(ExprTree);
        var plainP = plainFe.ParameterOf<int>("p");
        plainFe.RootIdx = plainFe.Lambda<Func<int, Func<int>>>(plainFe.Lambda<Func<int>>(plainFe.ConstantInt(7)), plainP);
        var plainFast = FlatCompiler.CompileFast<Func<int, Func<int>>>(ref plainFe, true);
        Ensure(plainFast != null && plainFast(0)() == 7, "nested constant");

        var invFe = default(ExprTree);
        var invP = invFe.ParameterOf<int>("p");
        var invQ = invFe.ParameterOf<int>("q");
        invFe.RootIdx = invFe.Lambda<Func<int, int>>(
            invFe.Invoke(invFe.Lambda<Func<int, int>>(invFe.Add(invQ, invFe.ConstantInt(1)), invQ), invP), invP);
        var invFast = FlatCompiler.CompileFast<Func<int, int>>(ref invFe, true);
        Ensure(invFast != null && invFast(10) == 11, "inline invoke");

        var niFe = default(ExprTree);
        var niP = niFe.ParameterOf<int>("p");
        var niQ = niFe.ParameterOf<int>("q");
        niFe.RootIdx = niFe.Lambda<Func<int, int>>(
            niFe.Invoke(niFe.Lambda<Func<int, int>>(niFe.Add(niQ, niP), niQ), niFe.ConstantInt(10)), niP);
        var niFast = FlatCompiler.CompileFast<Func<int, int>>(ref niFe, true,
            FastExpressionCompiler.FlatExpression.CompilerFlags.NoInvocationLambdaInlining);
        Ensure(niFast != null && niFast(3) == 13, "invoke delegate");

        var text = new string('z', 4);
        var bothFe = default(ExprTree);
        var bothP = bothFe.ParameterOf<int>("p");
        var concat = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) });
        var toString = typeof(int).GetMethod(nameof(int.ToString), Type.EmptyTypes);
        bothFe.RootIdx = bothFe.Lambda<Func<int, Func<string>>>(
            bothFe.Lambda<Func<string>>(bothFe.Call(concat, bothFe.Constant(text), bothFe.Call(bothP, toString))), bothP);
        var bothFast = FlatCompiler.CompileFast<Func<int, Func<string>>>(ref bothFe, true);
        Ensure(bothFast != null && bothFast(7)() == text + "7", "capture and closure constant");

        Gen.Int[0, int.MaxValue / 2]
            .Select(seed => new GeneratedCase(seed, GeneratedIntSpecFactory.Create(seed, maxDepth: 3, maxBreadth: 3)))
            .Sample(testCase =>
            {
                var light = CreateGeneratedLightNested(testCase.Spec);
                var fe = default(ExprTree);
                FillFlatNested(ref fe, testCase.Spec);
                var fast = FlatCompiler.CompileFast<Func<int, Func<int>>>(ref fe, true);
                var slow = light.CompileFast<Func<int, Func<int>>>(true);
                Ensure(fast != null && slow != null, testCase.Seed + " nested compile");
                foreach (var arg in new[] { -2, 0, 1, 5 })
                    Ensure(fast(arg)() == slow(arg)(), $"{testCase.Seed} nested {arg}");
            },
            iter: 20,
            threads: 1,
            seed: "0N0XIzNsQ0O2",
            print: testCase => $"{testCase.Seed} nested: {testCase.Spec}");

        Gen.Int[0, int.MaxValue / 2]
            .Select(seed => new GeneratedCase(seed, GeneratedIntSpecFactory.Create(seed, maxDepth: 3, maxBreadth: 3, arithmeticOnly: true)))
            .Sample(testCase =>
            {
                var lightExpr = CreateGeneratedLightExpression(testCase.Spec);
                var fe = default(ExprTree);
                FillFlat(ref fe, testCase.Spec);
                var fast = FlatCompiler.CompileFast<Func<int, int>>(ref fe, true);
                var slow = lightExpr.CompileFast<Func<int, int>>(true);
                Ensure(fast != null && slow != null, testCase.Seed + " compile");
                foreach (var arg in new[] { -2, 0, 1, 5 })
                    Ensure(fast(arg) == slow(arg), $"{testCase.Seed} arg {arg}");
            },
            iter: 40,
            threads: 1,
            seed: "0N0XIzNsQ0O2",
            print: testCase => $"{testCase.Seed}: {testCase.Spec}");
    }

    /// <summary>Checks every presence/absence combination of the optional children (switch default/cases, try finally/catch, catch variable/filter,
    /// loop break/continue, goto value, label default): the bits decide what is present; the flat tree must report exactly that.</summary>
    public void Can_fuzz_optional_children_presence_in_flat_expression()
    {
        // The input space is finite: exercise every optional-child combination.
        for (var bits = 0; bits < 1 << 10; ++bits)
            Ensure(CheckOptionalChildren(bits), $"optional-child mask {bits}");
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
            cases[k] = SysExpr.SwitchCase(SysExpr.Block(SysExpr.Assign(r, Int(10 + k)), SysExpr.Empty()), Int(k));
        var @switch = SysExpr.Switch(typeof(void), p, swDefault ? SysExpr.Block(SysExpr.Assign(r, Int(7)), SysExpr.Empty()) : null, null, cases);

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
        var light = fe.ToExpression().ToLightExpression();
        Ensure(fe.Equals(light.ToExpression().ToFlatExpression()), "flat->sys->flat roundtrip");

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

        if (light is FastExpressionCompiler.LightExpression.LambdaExpression lightLambda)
            AssertCollectMatch(lightLambda, ref fe, "optional-child");

        if (catchFilter) return true; // FEC does not support catch filters, the structure is checked above

        var fast = fe.ToExpression().ToLightExpression() is FastExpressionCompiler.LightExpression.LambdaExpression fl
            ? fl.CompileFast<Func<int, int>>(ifFastFailedReturnNull: true) : null;
        Ensure(fast != null, "CompileFast failed");
        var expected = lambda.Compile();
        // an uncaught throw (no catch) must happen in both
        static int Run(Func<int, int> f, int arg) { try { return f(arg); } catch (Exception) { return int.MinValue; } }
        foreach (var arg in new[] { -1, 0, 1, 2, 5 })
            Ensure(Run(fast, arg) == Run(expected, arg), $"result for {arg}");
        var flatFast = FlatCompiler.CompileFast<Func<int, int>>(ref fe, true);
        Ensure(flatFast != null, "flat emit failed");
        foreach (var arg in new[] { -1, 0, 1, 2, 5 })
            Ensure(Run(flatFast, arg) == Run(expected, arg), $"flat result for {arg}");
        return true;
    }

    public void Can_compile_complex_flat_expression_like_light()
    {
        var fe = default(ExprTree);
        CreateComplexFlatExpression(ref fe, "state");
        var fast = FlatCompiler.CompileFast<Func<object[], object>>(ref fe, true);
        Asserts.IsNotNull(fast);
        var light = CreateComplexLightExpression("state").CompileFast(true);
        var input = new object[12];
        input[11] = "direct";
        var flatA = (A)fast(input);
        var lightA = (A)light(input);
        Asserts.AreEqual(lightA.Sop, flatA.Sop);
        var flatCount = 0;
        var lightCount = 0;
        foreach (var _ in flatA.Dop) ++flatCount;
        foreach (var _ in lightA.Dop) ++lightCount;
        Asserts.AreEqual(lightCount, flatCount);
        Asserts.IsInstanceOf<P>(flatA.Prop);
        Asserts.IsNotNull(flatA.Bop);
    }

    /// <summary>Flat compile of a generated int tree and of the README complex expression stays ahead of Light.</summary>
    public void Can_check_flat_compile_is_faster_than_light()
    {
        var spec = GeneratedIntSpecFactory.Create(1, maxDepth: 3, maxBreadth: 3);
        var lightExpr = CreateGeneratedLightExpression(spec);
        var flatExpr = default(ExprTree);
        FillFlat(ref flatExpr, spec);
        var expected = lightExpr.CompileFast(true)(3);
        var flatOnce = FlatCompiler.CompileFast<Func<int, int>>(ref flatExpr, true);
        Asserts.IsNotNull(flatOnce);
        Asserts.AreEqual(expected, flatOnce(3));

        for (var i = 0; i < 16; ++i)
        {
            FlatCompiler.CompileFast<Func<int, int>>(ref flatExpr, true);
            lightExpr.CompileFast(true);
        }
        Check.Faster(
            () => { FlatCompiler.CompileFast<Func<int, int>>(ref flatExpr, true); },
            () => { lightExpr.CompileFast(true); },
            threads: 1, repeat: 8, timeout: 15);

        var complexFlat = default(ExprTree);
        CreateComplexFlatExpression(ref complexFlat);
        var complexLight = CreateComplexLightExpression();
        for (var i = 0; i < 16; ++i)
        {
            FlatCompiler.CompileFast<Func<object[], object>>(ref complexFlat, true);
            complexLight.CompileFast(true);
        }
        Check.Faster(
            () => { FlatCompiler.CompileFast<Func<object[], object>>(ref complexFlat, true); },
            () => { complexLight.CompileFast(true); },
            threads: 1, repeat: 4, timeout: 15);
    }

    public void Flat_compile_allocates_no_more_than_light()
    {
        var flat = default(ExprTree);
        CreateComplexFlatExpression(ref flat);
        var light = CreateComplexLightExpression();
        for (var i = 0; i < 64; ++i)
        {
            FlatCompiler.CompileFast<Func<object[], object>>(ref flat, true);
            light.CompileFast(true);
        }

        long Measure(Action compile)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; ++i)
                compile();
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        var flatBytes = Measure(() => FlatCompiler.CompileFast<Func<object[], object>>(ref flat, true));
        var lightBytes = Measure(() => light.CompileFast(true));
        // A leaked Type[2] is 40 bytes a compile. A few bytes of pool noise is not.
        Ensure(flatBytes <= lightBytes + 64 * 16, $"flat {flatBytes} light {lightBytes}");
    }

    private static void Ensure(bool ok, string what)
    {
        if (!ok) throw new InvalidOperationException("Property failed: " + what);
    }

    private static void AssertCollectMatch(FastExpressionCompiler.LightExpression.LambdaExpression light, ref ExprTree fe, string tag)
    {
        var lightCtx = new ExpressionCompiler.CompilerContext(
            ExpressionCompiler.ClosureStatus.ToBeCollected, light, FastExpressionCompiler.LightExpression.CompilerFlags.Default);
        var lightR = ExpressionCompiler.TryCollectInfo(
            ref lightCtx, light.Body, null, ref lightCtx.NestedLambdas, 0, true);

        var flatCtx = new FlatCompiler.CompilerContext(
            FlatCompiler.ClosureStatus.ToBeCollected, null, FastExpressionCompiler.FlatExpression.CompilerFlags.Default);
        var flatR = FlatCompiler.TryCollectInfo(ref flatCtx, ref fe);

        Ensure((int)flatR == (int)lightR, $"{tag}: result flat={flatR} light={lightR}");
        if (lightR != ExpressionCompiler.Result.OK)
            return;

        Ensure(flatCtx.NestedLambdas.Count == lightCtx.NestedLambdas.Count,
            $"{tag}: nested {flatCtx.NestedLambdas.Count} vs {lightCtx.NestedLambdas.Count}");
        Ensure(flatCtx.Constants.Count == lightCtx.Constants.Count,
            $"{tag}: constants {flatCtx.Constants.Count} vs {lightCtx.Constants.Count}");
        for (var i = 0; i < lightCtx.Constants.Count; ++i)
            Ensure(ReferenceEquals(flatCtx.Constants[i], lightCtx.Constants[i]), $"{tag}: constant {i}");

        Ensure(((int)flatCtx.Status & (int)FlatCompiler.ClosureStatus.HasClosure) ==
            ((int)lightCtx.Status & (int)ExpressionCompiler.ClosureStatus.HasClosure), $"{tag}: HasClosure");
    }

    private static FastExpressionCompiler.LightExpression.LambdaExpression CreateGeneratedLightNested(IntSpec spec)
    {
        var parameter = ParameterOf<int>("p");
        return Lambda<Func<int, Func<int>>>(Lambda<Func<int>>(BuildLightInt(spec, [parameter])), parameter);
    }

    private static void FillFlatNested(ref ExprTree fe, IntSpec spec)
    {
        fe = default;
        var parameter = fe.ParameterOf<int>("p");
        fe.RootIdx = fe.Lambda<Func<int, Func<int>>>(
            fe.Lambda<Func<int>>(BuildFlatInt(ref fe, spec, [parameter])), parameter);
    }

    private static FastExpressionCompiler.LightExpression.Expression<Func<int, int>> CreateGeneratedLightExpression(IntSpec spec)
    {
        var parameter = ParameterOf<int>("p");
        return Lambda<Func<int, int>>(BuildLightInt(spec, [parameter]), parameter);
    }

    private static void FillFlat(ref ExprTree fe, IntSpec spec)
    {
        fe = default;
        var parameter = fe.ParameterOf<int>("p");
        fe.RootIdx = fe.Lambda<Func<int, int>>(BuildFlatInt(ref fe, spec, [parameter]), parameter);
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
    private readonly record struct CollectCase(int Seed, IntSpec Spec, bool Nested);

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
        public static IntSpec Create(int seed, int maxDepth, int maxBreadth, bool arithmeticOnly = false) =>
            NextInt(new Random(seed), maxDepth, envIntCount: 1, maxBreadth, arithmeticOnly);

        private static IntSpec NextInt(Random random, int depth, int envIntCount, int maxBreadth, bool arithmeticOnly = false)
        {
            if (depth <= 0)
                return NextIntLeaf(random, envIntCount);

            switch (random.Next(arithmeticOnly ? 5 : 7))
            {
                case 0: return NextIntLeaf(random, envIntCount);
                case 1: return new IntSpec.Add(NextInt(random, depth - 1, envIntCount, maxBreadth, arithmeticOnly), NextInt(random, depth - 1, envIntCount, maxBreadth, arithmeticOnly));
                case 2: return new IntSpec.Subtract(NextInt(random, depth - 1, envIntCount, maxBreadth, arithmeticOnly), NextInt(random, depth - 1, envIntCount, maxBreadth, arithmeticOnly));
                case 3: return new IntSpec.Multiply(NextInt(random, depth - 1, envIntCount, maxBreadth, arithmeticOnly), NextInt(random, depth - 1, envIntCount, maxBreadth, arithmeticOnly));
                case 4 when !arithmeticOnly: return new IntSpec.Conditional(
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
}

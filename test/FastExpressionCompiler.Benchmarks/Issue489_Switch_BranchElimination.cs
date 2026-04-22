using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace FastExpressionCompiler.Benchmarks;

/// <summary>
/// Benchmarks for compile-time switch branch elimination introduced in #489.
///
/// Two variants are compared:
///   - Baseline:    Switch value is a runtime parameter - FEC cannot eliminate any branches.
///   - Eliminated:  Switch value is a compile-time constant - FEC selects the single matching
///                  branch and emits only that, skipping closure collection and IL for all others.
///
/// Each variant has two nested classes:
///   - Compile: measures how fast `Compile()` / `CompileFast()` process the expression.
///   - Invoke:  measures the execution speed of the resulting delegate.
///
/// The Invoke benchmark is the most telling: the eliminated switch emits just
/// `ldstr "B" / ret` (2 IL instructions) vs the full switch table.
/// </summary>
public class Issue489_Switch_BranchElimination
{
    // -----------------------------------------------------------------
    //  Shared expression factories
    // -----------------------------------------------------------------

    /// <summary>
    /// Baseline: the switch value is a runtime parameter - no elimination possible.
    ///   switch (x) { case 1: "A"; case 2: "B"; case 5: "C"; default: "Z" }
    /// </summary>
    private static Expression<Func<int, string>> CreateExpr_Baseline()
    {
        var p = Expression.Parameter(typeof(int), "x");
        return Expression.Lambda<Func<int, string>>(
            Expression.Switch(p,
                Expression.Constant("Z"),
                Expression.SwitchCase(Expression.Constant("A"), Expression.Constant(1)),
                Expression.SwitchCase(Expression.Constant("B"), Expression.Constant(2)),
                Expression.SwitchCase(Expression.Constant("C"), Expression.Constant(5))),
            p);
    }

    /// <summary>
    /// Branch-eliminated: the switch value is the compile-time constant <c>2</c>.
    ///   switch (2) { case 1: "A"; case 2: "B"; case 5: "C"; default: "Z" }
    /// FEC reduces this to a single <c>ldstr "B" / ret</c>.
    /// </summary>
    private static Expression<Func<string>> CreateExpr_Eliminated()
    {
        return Expression.Lambda<Func<string>>(
            Expression.Switch(Expression.Constant(2),
                Expression.Constant("Z"),
                Expression.SwitchCase(Expression.Constant("A"), Expression.Constant(1)),
                Expression.SwitchCase(Expression.Constant("B"), Expression.Constant(2)),
                Expression.SwitchCase(Expression.Constant("C"), Expression.Constant(5))));
    }

    // -----------------------------------------------------------------
    //  Compilation benchmarks
    // -----------------------------------------------------------------

    /// <summary>
    /// Measures how fast Compile / CompileFast process each expression variant.
    /// Baseline: runtime parameter switch (no FEC branch elimination).
    /// Eliminated: constant switch (FEC skips dead branch closure-collection and IL emission).
    /// </summary>
    [MemoryDiagnoser, RankColumn, Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class Compile
    {
        /*
        ## Results placeholder - run with: dotnet run -c Release --project test/FastExpressionCompiler.Benchmarks -- --filter *Issue489*Compile*

        | Method                       | Mean      | Error    | StdDev   | Ratio | Rank | Allocated |
        |----------------------------- |----------:|---------:|---------:|------:|-----:|----------:|
        | Baseline_CompileFast         |       N/A |      N/A |      N/A |       |      |       N/A |
        | Baseline_Compile             |       N/A |      N/A |      N/A |       |      |       N/A |
        | Eliminated_CompileFast       |       N/A |      N/A |      N/A |       |      |       N/A |
        | Eliminated_Compile           |       N/A |      N/A |      N/A |       |      |       N/A |
        */

        private static readonly Expression<Func<int, string>> _baseline = CreateExpr_Baseline();
        private static readonly Expression<Func<string>> _eliminated = CreateExpr_Eliminated();

        [Benchmark(Baseline = true)]
        public object Baseline_Compile() => _baseline.Compile();

        [Benchmark]
        public object Baseline_CompileFast() => _baseline.CompileFast();

        [Benchmark]
        public object Eliminated_Compile() => _eliminated.Compile();

        [Benchmark]
        public object Eliminated_CompileFast() => _eliminated.CompileFast();
    }

    // -----------------------------------------------------------------
    //  Invocation benchmarks
    // -----------------------------------------------------------------

    /// <summary>
    /// Measures invocation speed of the compiled delegates.
    /// The eliminated FEC delegate emits only 2 IL instructions (ldstr + ret),
    /// while the system-compiled one runs a full switch dispatch at runtime.
    /// </summary>
    [MemoryDiagnoser, RankColumn, Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class Invoke
    {
        /*
        ## Results placeholder - run with: dotnet run -c Release --project test/FastExpressionCompiler.Benchmarks -- --filter *Issue489*Invoke*

        | Method                          | Mean | Error | StdDev | Ratio | Rank | Allocated |
        |-------------------------------- |-----:|------:|-------:|------:|-----:|----------:|
        | Baseline_Compiled               |  N/A |   N/A |    N/A |       |      |       N/A |
        | Baseline_CompiledFast           |  N/A |   N/A |    N/A |       |      |       N/A |
        | Eliminated_Compiled             |  N/A |   N/A |    N/A |       |      |       N/A |
        | Eliminated_CompiledFast         |  N/A |   N/A |    N/A |       |      |       N/A |
        */

        private Func<int, string> _baselineCompiled;
        private Func<int, string> _baselineCompiledFast;
        private Func<string> _eliminatedCompiled;
        private Func<string> _eliminatedCompiledFast;

        [GlobalSetup]
        public void Setup()
        {
            _baselineCompiled = CreateExpr_Baseline().Compile();
            _baselineCompiledFast = CreateExpr_Baseline().CompileFast();
            _eliminatedCompiled = CreateExpr_Eliminated().Compile();
            _eliminatedCompiledFast = CreateExpr_Eliminated().CompileFast();
        }

        [Benchmark(Baseline = true)]
        public string Baseline_Compiled() => _baselineCompiled(2);

        [Benchmark]
        public string Baseline_CompiledFast() => _baselineCompiledFast(2);

        [Benchmark]
        public string Eliminated_Compiled() => _eliminatedCompiled();

        [Benchmark]
        public string Eliminated_CompiledFast() => _eliminatedCompiledFast();
    }
}

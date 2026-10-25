using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer.Horology;
using S = System.Linq.Expressions.Expression;
using L = FastExpressionCompiler.LightExpression.Expression;

// Run a copied Release output directory for each revision. In-process execution deliberately
// avoids BenchmarkDotNet rebuilding project references from a different working-tree revision.
var config = DefaultConfig.Instance
    .AddJob(Job.Default.WithId("FrozenBinaries").WithToolchain(InProcessEmitToolchain.Instance)
        .WithWarmupCount(3).WithIterationCount(8).WithIterationTime(TimeInterval.FromMilliseconds(150)))
    .AddExporter(JsonExporter.Full)
    .WithArtifactsPath(Path.GetFullPath(Environment.GetEnvironmentVariable("FEC_PERF_OUTPUT") ?? "performance-results"));
Console.WriteLine($"Host: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}; " +
    $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription}; " +
    $"{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}; CPU count {Environment.ProcessorCount}");
BenchmarkSwitcher.FromAssembly(typeof(Compilation).Assembly).Run(args, config);

public enum Workload { Arithmetic, Array, Switch, CapturedConstant, Conditional }

public sealed class Input
{
    public int Value;
    public int[] Values = Enumerable.Range(0, 64).ToArray();
}

public static class Trees
{
    public static readonly Input Captured = new() { Value = 17 };

    public static System.Linq.Expressions.Expression<Func<Input, int>> System(Workload workload)
    {
        var p = S.Parameter(typeof(Input), "input");
        var value = S.Field(p, nameof(Input.Value));
        S body = workload switch
        {
            Workload.Arithmetic => S.Add(S.Multiply(value, S.Constant(7)), S.Constant(3)),
            Workload.Conditional => S.Condition(S.Equal(S.And(value, S.Constant(1)), S.Constant(1)), S.Constant(7), S.Constant(19)),
            Workload.Array => S.Add(S.ArrayAccess(S.Field(p, nameof(Input.Values)), S.And(value, S.Constant(63))),
                S.ArrayAccess(S.Field(p, nameof(Input.Values)), S.And(S.Add(value, S.Constant(1)), S.Constant(63)))),
            Workload.Switch => S.Switch(S.And(value, S.Constant(3)), S.Constant(-1),
                S.SwitchCase(S.Constant(17), S.Constant(0)), S.SwitchCase(S.Constant(29), S.Constant(1)),
                S.SwitchCase(S.Constant(43), S.Constant(2))),
            _ => S.Add(value, S.Field(S.Constant(Captured), nameof(Input.Value)))
        };
        return S.Lambda<Func<Input, int>>(body, p);
    }

    public static FastExpressionCompiler.LightExpression.Expression<Func<Input, int>> Light(Workload workload)
    {
        var p = L.Parameter(typeof(Input), "input");
        var value = L.Field(p, nameof(Input.Value));
        L body = workload switch
        {
            Workload.Arithmetic => L.Add(L.Multiply(value, L.Constant(7)), L.Constant(3)),
            Workload.Conditional => L.Condition(L.Equal(L.And(value, L.Constant(1)), L.Constant(1)), L.Constant(7), L.Constant(19)),
            Workload.Array => L.Add(L.ArrayAccess(L.Field(p, nameof(Input.Values)), L.And(value, L.Constant(63))),
                L.ArrayAccess(L.Field(p, nameof(Input.Values)), L.And(L.Add(value, L.Constant(1)), L.Constant(63)))),
            Workload.Switch => L.Switch(L.And(value, L.Constant(3)), L.Constant(-1),
                L.SwitchCase(L.Constant(17), L.Constant(0)), L.SwitchCase(L.Constant(29), L.Constant(1)),
                L.SwitchCase(L.Constant(43), L.Constant(2))),
            _ => L.Add(value, L.Field(L.Constant(Captured), nameof(Input.Value)))
        };
        return L.Lambda<Func<Input, int>>(body, p);
    }
}

[MemoryDiagnoser]
public class Construction
{
    [ParamsAllValues] public Workload Workload;
    [Benchmark(Baseline = true)] public object SystemTree() => Trees.System(Workload);
    [Benchmark] public object LightTree() => Trees.Light(Workload);
}

[MemoryDiagnoser]
public class Compilation
{
    [ParamsAllValues] public Workload Workload;
    private System.Linq.Expressions.Expression<Func<Input, int>> _system;
    private FastExpressionCompiler.LightExpression.Expression<Func<Input, int>> _light;
    [GlobalSetup] public void Setup() { _system = Trees.System(Workload); _light = Trees.Light(Workload); }
    [Benchmark(Baseline = true)] public Delegate SystemCompile() => _system.Compile();
    [Benchmark] public Delegate FastCompile() => FastExpressionCompiler.ExpressionCompiler.CompileFast(_system, true);
    [Benchmark] public Delegate LightCompile() => FastExpressionCompiler.LightExpression.ExpressionCompiler.CompileFast(_light, true);
}

[MemoryDiagnoser]
public class Invocation
{
    [ParamsAllValues] public Workload Workload;
    private Func<Input, int> _system, _fast, _light;
    private readonly Input _input = new();
    [GlobalSetup]
    public void Setup()
    {
        var tree = Trees.System(Workload);
        _system = tree.Compile();
        _fast = FastExpressionCompiler.ExpressionCompiler.CompileFast(tree, true);
        _light = FastExpressionCompiler.LightExpression.ExpressionCompiler.CompileFast(Trees.Light(Workload), true);
        for (var i = -128; i < 128; ++i)
        {
            _input.Value = i;
            if (_system(_input) != _fast(_input) || _system(_input) != _light(_input))
                throw new InvalidOperationException($"Behavior mismatch: {Workload}, {i}");
        }
    }
    // Vary inputs and consume outputs; batch all implementations identically to amortize harness overhead.
    private int Run(Func<Input, int> function)
    {
        var sum = 0;
        for (var i = 0; i < 256; ++i) { _input.Value = i; sum += function(_input); }
        return sum;
    }
    [Benchmark(Baseline = true, OperationsPerInvoke = 256)] public int SystemInvoke() => Run(_system);
    [Benchmark(OperationsPerInvoke = 256)] public int FastInvoke() => Run(_fast);
    [Benchmark(OperationsPerInvoke = 256)] public int LightInvoke() => Run(_light);
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;

namespace Regression;

[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class PerformanceBenchmarks
{
    [Params("Standard", "Light")] public string Backend { get; set; } = "Standard";
    [Params("Scalar", "Arithmetic", "CallConditional", "Closure")] public string Shape { get; set; } = "Scalar";
    private Func<Delegate> compile = null!;
    private Func<object> construct = null!;
    private Func<Delegate, int, int> invoke = null!;
    private Delegate compiled = null!;

    [GlobalSetup]
    public void Setup()
    {
        if (Backend == "Standard")
        {
            compile = Standard.Workloads.Prepare(Shape);
            construct = () => Standard.Workloads.Construct(Shape);
            invoke = Standard.Workloads.Invoke;
            Standard.Workloads.Verify(Shape, compile);
        }
        else
        {
            compile = Light.Workloads.Prepare(Shape);
            construct = () => Light.Workloads.Construct(Shape);
            invoke = Light.Workloads.Invoke;
            Light.Workloads.Verify(Shape, compile);
        }
        compiled = compile();
        _ = invoke(compiled, 3); // Warm delegate execution independently of compilation.
    }

    [Benchmark] public object Construct() => construct();
    [Benchmark] public Delegate Compile() => compile();
    [Benchmark] public int CompileAndFirstInvoke() => invoke(compile(), 3);

    // Amortize harness overhead; return a consumed checksum to keep every call observable.
    [Benchmark(OperationsPerInvoke = 256)]
    public int Invoke()
    {
        var sum = 0;
        for (var i = 0; i < 256; ++i) sum += invoke(compiled, i & 7);
        return sum;
    }
}

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Contains("--verify"))
        {
            foreach (var backend in new[] { "Standard", "Light" })
            foreach (var shape in new[] { "Scalar", "Arithmetic", "CallConditional", "Closure" })
                new PerformanceBenchmarks { Backend = backend, Shape = shape }.Setup();
            Console.WriteLine("All 8 workload/backend behavior checks passed.");
            return 0;
        }
        var smoke = args.Contains("--smoke");
        var job = smoke ? Job.Dry : Job.Default.WithLaunchCount(1).WithWarmupCount(2).WithIterationCount(5).WithIterationTime(TimeInterval.FromMilliseconds(100));
        var config = ManualConfig.Create(DefaultConfig.Instance).AddJob(job.WithId(smoke ? "Smoke" : "Regression"));
        var summaries = BenchmarkSwitcher.FromTypes(new[] { typeof(PerformanceBenchmarks) }).Run(args.Where(a => a != "--smoke").ToArray(), config).ToArray();
        return summaries.Length == 0 || summaries.Any(s => s.HasCriticalValidationErrors || s.Reports.Any(r => !r.Success)) ? 1 : 0;
    }
}

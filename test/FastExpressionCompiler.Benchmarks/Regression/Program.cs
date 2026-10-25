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
    private Func<int, int>? scalar;
    private Func<int, Func<int>>? closure;
    private Delegate compiled = null!;

    [GlobalSetup]
    public void Setup()
    {
        if (Backend == "Standard")
        {
            compile = Standard.Workloads.Prepare(Shape);
            construct = () => Standard.Workloads.Construct(Shape);
            Standard.Workloads.Verify(Shape, compile);
        }
        else
        {
            compile = Light.Workloads.Prepare(Shape);
            construct = () => Light.Workloads.Construct(Shape);
            Light.Workloads.Verify(Shape, compile);
        }
        compiled = compile();
        if (Shape == "Closure") { closure = (Func<int, Func<int>>)compiled; _ = closure(3)(); }
        else { scalar = (Func<int, int>)compiled; _ = scalar(3); }
    }

    [Benchmark] public object Construct() => construct();
    [Benchmark] public Delegate Compile() => compile();
    [Benchmark]
    public int CompileAndFirstInvoke()
    {
        var fresh = compile();
        return Shape == "Closure" ? ((Func<int, Func<int>>)fresh)(3)() : ((Func<int, int>)fresh)(3);
    }

    // Amortize harness overhead; return a consumed checksum to keep every call observable.
    [Benchmark(OperationsPerInvoke = 256)]
    public int Invoke()
    {
        var sum = 0;
        if (Shape == "Closure")
        {
            var f = closure!;
            for (var i = 0; i < 256; ++i) sum += f((i & 15) - 8)();
        }
        else
        {
            var f = scalar!;
            for (var i = 0; i < 256; ++i) sum += f((i & 15) - 8);
        }
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
            {
                var benchmark = new PerformanceBenchmarks { Backend = backend, Shape = shape };
                benchmark.Setup();
                var first = shape switch { "Scalar" or "Closure" => 4, "Arithmetic" => 96, _ => 7 };
                var batched = shape switch { "Scalar" or "Closure" => 128, "Arithmetic" => -4096, _ => 1216 };
                if (benchmark.CompileAndFirstInvoke() != first || benchmark.Invoke() != batched || benchmark.Construct() == null)
                    throw new InvalidOperationException("Incorrect benchmark driver: " + backend + "/" + shape);
            }
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

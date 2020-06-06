using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
/*
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18363
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.202
  [Host]     : .NET Core 3.1.4 (CoreCLR 4.700.20.20201, CoreFX 4.700.20.22101), X64 RyuJIT
  DefaultJob : .NET Core 3.1.4 (CoreCLR 4.700.20.20201, CoreFX 4.700.20.22101), X64 RyuJIT


|              Method |      Mean |    Error |   StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |----------:|---------:|---------:|------:|-------:|------:|------:|----------:|
|    ReflectionInvoke | 159.67 ns | 0.684 ns | 0.640 ns |  1.00 | 0.0236 |     - |     - |     112 B |
| CallObjectParamFunc |  16.48 ns | 0.129 ns | 0.115 ns |  0.10 | 0.0153 |     - |     - |      72 B |
*/

    [MemoryDiagnoser]
    public class ReflectionInvoke_vs_CallWithObjectArgsAndNestedLambda
    {
        private static ReflectionInvoke_vs_CallWithObjectArgsAndNestedLambda _instance = new ReflectionInvoke_vs_CallWithObjectArgsAndNestedLambda();

        // private C F(A a, B b) => new C(a, b);

        private static Func<A, B, C> _f = (a, b) => new C(a, b);
        private static MethodInfo _fm = _f.Method;
        private static object _target = _f.Target;
        private static object _a = new A();
        private static object _b = new B();

        [Benchmark(Baseline = true)]
        public object ReflectionInvoke()
        {
            return _fm.Invoke(_target, new object[] { _a, _b });
        }

        private static Func<object[], object> _fo = p => _f((A)p[0], (B)p[1]);

        [Benchmark]
        public object CallObjectParamFunc()
        {
            return _fo(new object[] { _a, _b });
        }

        public class A { }
        public class B { }
        public class C
        {
            public A A;
            public B B;
            public C(A a, B b) { A = a; B = b; }
        }
    }
}
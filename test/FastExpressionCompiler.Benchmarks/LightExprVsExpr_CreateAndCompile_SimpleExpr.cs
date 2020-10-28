using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;

using LEC = FastExpressionCompiler.LightExpression.ExpressionCompiler;
using LE = FastExpressionCompiler.LightExpression.Expression;

//BenchmarkDotNet=v0.11.2, OS=Windows 10.0.17134.345 (1803/April2018Update/Redstone4)
//Intel Core i7-8750H CPU 2.20GHz(Coffee Lake), 1 CPU, 12 logical and 6 physical cores
//Frequency=2156252 Hz, Resolution=463.7677 ns, Timer=TSC
//.NET Core SDK=2.1.403
//  [Host] : .NET Core 2.1.5 (CoreCLR 4.6.26919.02, CoreFX 4.6.26919.02), 64bit RyuJIT
//  Core   : .NET Core 2.1.5 (CoreCLR 4.6.26919.02, CoreFX 4.6.26919.02), 64bit RyuJIT

//Job=Core Runtime = Core

//
//                                          Method |        Mean |        Error |       StdDev |  Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
//------------------------------------------------ |------------:|-------------:|-------------:|-------:|--------:|------------:|------------:|------------:|--------------------:|
//             CreateExpression_and_Compile_Invoke | 96,754.5 ns | 1,135.402 ns | 1,062.056 ns | 155.62 |    1.64 |      0.8545 |      0.3662 |           - |              4392 B |
//     CreateExpression_and_FastCompile_and_Invoke | 80,889.9 ns | 1,234.337 ns | 1,154.599 ns | 129.98 |    1.63 |      0.2441 |      0.1221 |           - |              1648 B |
// CreateLightExpression_and_FastCompile_and_Invoke | 79,597.1 ns | 1,241.430 ns | 1,100.495 ns | 128.13 |    1.86 |      0.2441 |      0.1221 |           - |              1576 B |
//                         ActivatorCreateInstance |    621.2 ns |     1.566 ns |     1.388 ns |   1.00 |    0.00 |      0.1059 |           - |           - |               504 B |


namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    public class LightExprVsExpr_CreateAndCompile_SimpleExpr
    {
        public class A { }
        public class B { }
        public class X
        {
            public A A { get; }
            public B B { get; }
            public X(A a, B b)
            {
                A = a;
                B = b;
            }
        }

        private static readonly ConstructorInfo _xCtor = typeof(X).GetTypeInfo().GetConstructors()[0];
        private static readonly ConstructorInfo _aCtor = typeof(A).GetTypeInfo().GetConstructors()[0];
        private static readonly ConstructorInfo _bCtor = typeof(B).GetTypeInfo().GetConstructors()[0];

        private static readonly Expression<Func<X>> _expr = CreateExpr();


        private static Expression<Func<X>> CreateExpr() => 
            Expression.Lambda<Func<X>>(
            Expression.New(_xCtor, Expression.New(_aCtor), Expression.New(_bCtor)),
            Array.Empty<ParameterExpression>());

        /*
      Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
------------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
     Compile | 99.247 us | 1.9556 us | 2.0082 us | 26.79 |    1.06 |      0.8545 |      0.3662 |           - |             3.96 KB |
 FastCompile |  3.706 us | 0.0719 us | 0.1008 us |  1.00 |    0.00 |      0.2823 |      0.1411 |      0.0191 |             1.28 KB |
         */
        //[Benchmark]
        public Func<X> Compile() => _expr.Compile();

        //[Benchmark(Baseline = true)]
        public Func<X> FastCompile() => _expr.CompileFast(true);

/*
             Method |     Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
------------------- |---------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
     InvokeCompiled | 15.68 ns | 0.0835 ns | 0.0741 ns |  1.23 |    0.02 |      0.0169 |           - |           - |                80 B |
 InvokeFastCompiled | 12.75 ns | 0.1291 ns | 0.1208 ns |  1.00 |    0.00 |      0.0169 |           - |           - |                80 B |


#V3-preview-03

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.403
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT


|             Method |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------- |---------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|     InvokeCompiled | 18.64 ns | 0.266 ns | 0.236 ns |  1.30 |    0.03 | 0.0191 |     - |     - |      80 B |
| InvokeFastCompiled | 14.30 ns | 0.317 ns | 0.281 ns |  1.00 |    0.00 | 0.0191 |     - |     - |      80 B |

*/

        private static readonly Func<X> _compiledFunc = _expr.Compile();
        private static Func<X> _fastCompiledFunc = _expr.CompileFast(true);

        [Benchmark]
        public X InvokeCompiled() => _compiledFunc();

        [Benchmark(Baseline = true)]
        public X InvokeFastCompiled() => _fastCompiledFunc();

        //[Benchmark]
        public X CreateExpression_and_Compile_Invoke()
        {
            var expr = Expression.Lambda<Func<X>>(
                Expression.New(_xCtor, Expression.New(_aCtor), Expression.New(_bCtor)),
                Array.Empty<ParameterExpression>());
            return expr.Compile().Invoke();
        }

        //[Benchmark(Baseline = true)]
        public X CreateExpression_and_FastCompile_and_Invoke()
        {
            var expr = Expression.Lambda<Func<X>>(
                Expression.New(_xCtor, Expression.New(_aCtor), Expression.New(_bCtor)),
                Array.Empty<ParameterExpression>());
            return expr.CompileFast(true).Invoke();
        }

        //[Benchmark]
        public X CreateLightExpression_and_FastCompile_and_Invoke()
        {
            var expr = LE.Lambda<Func<X>>(LE.New(_xCtor, LE.New(_aCtor), LE.New(_bCtor)));
            return LEC.CompileFast(expr, true).Invoke();
        }

        //[Benchmark]
        public X ManualEmit_and_Invoke()
        {
            var method = new DynamicMethod(string.Empty, typeof(object), Type.EmptyTypes,
                typeof(LightExprVsExpr_CreateAndCompile_SimpleExpr), skipVisibility: true);

            var il = method.GetILGenerator();

            il.Emit(OpCodes.Newobj, _aCtor);
            il.Emit(OpCodes.Newobj, _bCtor);
            il.Emit(OpCodes.Newobj, _xCtor);
            il.Emit(OpCodes.Ret);

            return ((Func<X>)method.CreateDelegate(typeof(Func<X>), null)).Invoke();
        }

        //[Benchmark(Baseline = true)]
        public X ActivatorCreateInstance()
        {
            return (X)Activator.CreateInstance(typeof(X),
                Activator.CreateInstance(typeof(A)),
                Activator.CreateInstance(typeof(B)));
        }
    }
}

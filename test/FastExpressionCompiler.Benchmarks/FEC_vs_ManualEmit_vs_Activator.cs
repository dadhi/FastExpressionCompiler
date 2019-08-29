using System;
using System.Linq.Expressions;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    /// <summary>
    /// FEC v1.6 or something:
    /// 
    /// Results:
    ///                  Method |       Mean |    StdDev | Scaled | Scaled-StdDev |
    /// ----------------------- |----------- |---------- |------- |-------------- |
    ///   Fec_with_CreatingExpr | 10.0897 us | 0.2031 us |   3.98 |          0.08 |
    ///                     FEC |  6.7786 us | 0.1235 us |   2.67 |          0.05 |
    ///              ManualEmit |  6.4999 us | 0.1452 us |   2.56 |          0.06 |
    /// ActivatorCreateInstance |  2.5376 us | 0.0200 us |   1.00 |          0.00 |
    ///
    /// FEC v2.0-preview:

    ///                            Method |  Job | Runtime |     Mean |     Error |    StdDev | Scaled | ScaledSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
    /// --------------------------------- |----- |-------- |---------:|----------:|----------:|-------:|---------:|-------:|-------:|-------:|----------:|
    ///  CreateExpression_and_CompileFast |  Clr |     Clr | 8.295 us | 0.0841 us | 0.0786 us |   4.83 |     0.06 | 0.4120 | 0.1984 | 0.0305 |   1.96 KB |
    ///                       CompileFast |  Clr |     Clr | 5.581 us | 0.0667 us | 0.0592 us |   3.25 |     0.04 | 0.3052 | 0.1526 | 0.0229 |   1.41 KB |
    ///                        ManualEmit |  Clr |     Clr | 5.344 us | 0.1225 us | 0.1086 us |   3.11 |     0.07 | 0.3052 | 0.1526 | 0.0229 |   1.41 KB |
    ///           ActivatorCreateInstance |  Clr |     Clr | 1.717 us | 0.0144 us | 0.0135 us |   1.00 |     0.00 | 0.2346 |      - |      - |   1.09 KB |
    ///                                   |      |         |          |           |           |        |          |        |        |        |           |
    ///  CreateExpression_and_CompileFast | Core |    Core | 7.122 us | 0.0537 us | 0.0449 us |   4.74 |     0.05 | 0.4272 | 0.2136 | 0.0305 |   1.97 KB |
    ///                       CompileFast | Core |    Core | 3.992 us | 0.0784 us | 0.0839 us |   2.66 |     0.06 | 0.2899 | 0.1450 | 0.0229 |   1.34 KB |
    ///                        ManualEmit | Core |    Core | 3.649 us | 0.0360 us | 0.0337 us |   2.43 |     0.03 | 0.2937 | 0.1450 | 0.0267 |   1.34 KB |
    ///           ActivatorCreateInstance | Core |    Core | 1.503 us | 0.0162 us | 0.0152 us |   1.00 |     0.00 | 0.2346 |      - |      - |   1.09 KB |
    /// 
    /// </summary>
    [MemoryDiagnoser]
    public class FEC_vs_ManualEmit_vs_Activator
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

        private static readonly Expression<Func<object>> _expr = () => new X(new A(), new B());

        private static readonly object[] _emptyArray = new object[0];

        [Benchmark]
        public object CreateExpression_and_CompileFast()
        {
            Expression<Func<object>> expr = () => new X(new A(), new B());
            return expr.CompileFast();
        }

        [Benchmark]
        public object CompileFast()
        {
            return _expr.CompileFast();
        }

        [Benchmark]
        public object ManualEmit()
        {
            var method = new DynamicMethod(string.Empty, typeof(object), Type.EmptyTypes,
                typeof(FEC_vs_ManualEmit_vs_Activator), skipVisibility: true);
            var il = method.GetILGenerator();

            var newX = (NewExpression)_expr.Body;
            var newXArgs = newX.Arguments;
            for (var i = 0; i < newXArgs.Count; i++)
            {
                var arg = newXArgs[i];
                var e = (NewExpression)arg;
                il.Emit(OpCodes.Newobj, e.Constructor);
            }

            il.Emit(OpCodes.Newobj, newX.Constructor);
            il.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(Func<object>), null);
        }

        [Benchmark(Baseline = true)]
        public object ActivatorCreateInstance()
        {
            return Activator.CreateInstance(typeof(X),
                Activator.CreateInstance(typeof(A), _emptyArray),
                Activator.CreateInstance(typeof(B), _emptyArray));
        }
    }
}

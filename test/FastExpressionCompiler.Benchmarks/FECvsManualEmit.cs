using System;
using System.Linq.Expressions;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    /// <summary>
    /// Results:
    ///                  Method |       Mean |    StdDev | Scaled | Scaled-StdDev |
    /// ----------------------- |----------- |---------- |------- |-------------- |
    ///   Fec_with_CreatingExpr | 10.0897 us | 0.2031 us |   3.98 |          0.08 |
    ///                     FEC |  6.7786 us | 0.1235 us |   2.67 |          0.05 |
    ///              ManualEmit |  6.4999 us | 0.1452 us |   2.56 |          0.06 |
    /// ActivatorCreateInstance |  2.5376 us | 0.0200 us |   1.00 |          0.00 |
    /// </summary>
    public class FECvsManualEmit
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

        [Benchmark]
        public object Fec_with_CreatingExpr()
        {
            Expression<Func<object>> expr = () => new X(new A(), new B());
            return ExpressionCompiler.Compile<Func<object>>(expr);
        }

        [Benchmark]
        public object FEC()
        {
            return ExpressionCompiler.Compile<Func<object>>(_expr);
        }

        [Benchmark]
        public object ManualEmit()
        {
            var method = new DynamicMethod(string.Empty, typeof(object), Type.EmptyTypes,
                typeof(FECvsManualEmit), skipVisibility: true);
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
                Activator.CreateInstance(typeof(A)),
                Activator.CreateInstance(typeof(B)));
        }
    }
}

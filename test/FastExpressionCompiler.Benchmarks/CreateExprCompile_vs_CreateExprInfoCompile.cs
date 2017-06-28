using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;

namespace FastExpressionCompiler.Benchmarks
{
    /// <summary>
    /// Results:
    ///                              Method |       Mean |    StdErr |    StdDev | Scaled | Scaled-StdDev |  Gen 0 |  Gen 1 | Allocated |
    ///------------------------------------ |----------- |---------- |---------- |------- |-------------- |------- |------- |---------- |
    ///    CreateExpression_and_FastCompile | 14.0174 us | 0.0765 us | 0.2758 us |  11.28 |          0.25 | 0.6714 |      - |    2.2 kB |
    ///CreateExpressionInfo_and_FastCompile |  6.5936 us | 0.0658 us | 0.2632 us |   5.31 |          0.21 | 0.7135 | 0.2223 |   1.49 kB |
    ///                          ManualEmit |  5.7978 us | 0.0747 us | 0.2893 us |   4.67 |          0.23 | 0.7100 | 0.2625 |   1.38 kB |
    ///             ActivatorCreateInstance |  1.2429 us | 0.0041 us | 0.0149 us |   1.00 |          0.00 | 0.2698 |      - |     504 B |
    /// </summary>
    [MemoryDiagnoser, MarkdownExporter]
    public class CreateExprCompile_vs_CreateExprInfoCompile
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

        [Benchmark]
        public object CreateExpression_and_Compile()
        {
            var expr = Expression.Lambda(Expression.New(_xCtor, Expression.New(_aCtor), Expression.New(_bCtor)));
            return expr.Compile();
        }


        [Benchmark]
        public object CreateExpression_and_FastCompile()
        {
            var expr = Expression.Lambda(Expression.New(_xCtor, Expression.New(_aCtor), Expression.New(_bCtor)));
            return ExpressionCompiler.TryCompile<Func<object>>(expr);
        }

        [Benchmark(Baseline = true)]
        public object CreateExpressionInfo_and_FastCompile()
        {
            var expr = ExpressionInfo.Lambda(ExpressionInfo.New(_xCtor, ExpressionInfo.New(_aCtor), ExpressionInfo.New(_bCtor)));
            return ExpressionCompiler.TryCompile<Func<object>>(expr);
        }

        [Benchmark]
        public object ManualEmit()
        {
            var method = new DynamicMethod(string.Empty, typeof(object), Type.EmptyTypes,
                typeof(CreateExprCompile_vs_CreateExprInfoCompile), skipVisibility: true);
            var il = method.GetILGenerator();

            il.Emit(OpCodes.Newobj, _aCtor);
            il.Emit(OpCodes.Newobj, _bCtor);
            il.Emit(OpCodes.Newobj, _xCtor);

            il.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(Func<object>), null);
        }
        
        [Benchmark]
        public object ActivatorCreateInstance()
        {
            return Activator.CreateInstance(typeof(X),
                Activator.CreateInstance(typeof(A)),
                Activator.CreateInstance(typeof(B)));
        }
    }
}

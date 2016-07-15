using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using DryIoc;

namespace Playground
{
    public class CompileClosureExpression_FastExprCompiler_vs_ExprCompile
    {
        private Expression<Func<object>> _hoistedExpr = GetHoistedExpr();

        private static Expression<Func<object>> GetHoistedExpr()
        {
            var a = new A();
            var b = new B();
            Expression<Func<object>> e = () => new X(a, b);
            return e;
        }

        private Expression<Func<object>> _manualExpr = GetManualExpr();

        private static Expression<Func<object>> GetManualExpr()
        {
            var a = new A();
            var b = new B();
            var e = Expression.Lambda<Func<object>>(
                Expression.New(typeof(X).GetConstructors()[0],
                Expression.Constant(a, typeof(A)),
                Expression.Constant(b, typeof(B))));
            return e;
        }

        [Benchmark]
        public Func<object> ExpressionCompile()
        {
            return _hoistedExpr.Compile();
        }

        [Benchmark]
        public Func<object> FastExpressionCompiler()
        {
            return DryIoc.FastExpressionCompiler.TryCompile<Func<object>>(_manualExpr.Body, 
                ArrayTools.Empty<ParameterExpression>(), ArrayTools.Empty<Type>(), typeof(object));
        }

        public class A { }

        public class B { }

        public class X
        {
            public A A { get; private set; }
            public B B { get; private set; }

            public X(A a, B b)
            {
                A = a;
                B = b;
            }
        }
    }
}

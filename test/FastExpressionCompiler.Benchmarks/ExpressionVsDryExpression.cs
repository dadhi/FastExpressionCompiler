using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    public class ExpressionVsDryExpression
    {
        private static readonly ConstructorInfo _xCtor = typeof(X).GetTypeInfo().DeclaredConstructors.First();
        private static readonly Y _y = new Y();

        [Benchmark]
        public object NewExpression()
        {
            return Expression.New(_xCtor, Expression.Constant(_y));
        }

        [Benchmark]
        public object NewDryExpression()
        {
            return DryExpression.New(_xCtor, DryExpression.Constant(_y));
        }

        public class Y { }
        public class X { public X(Y y) {} }
    }
}

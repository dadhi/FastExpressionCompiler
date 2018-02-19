using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    public class ExprInfoVsExpr_SimpleExpr_NewInstance
    {
        private static readonly ConstructorInfo _xCtor = typeof(X).GetTypeInfo().DeclaredConstructors.First();
        private static readonly Y _y = new Y();

        [Benchmark]
        public object NewExpression()
        {
            return Expression.New(_xCtor, Expression.Constant(_y));
        }

        [Benchmark]
        public object NewExpressionInfo()
        {
            return ExpressionInfo.New(_xCtor, ExpressionInfo.Constant(_y));
        }

        public class Y { }
        public class X
        {
            public Y Y { get; }
            public X(Y y)
            {
                Y = y;
            }
        }
    }
}

using System;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{

    public class HoistedLambdaExprTests : ITest
    {
        public int Run()
        {
            Should_compile_nested_lambda();
            return 1;
        }


        public void Should_compile_nested_lambda()
        {
            var a = new A();
            System.Linq.Expressions.Expression<Func<X>> se = () => X.Get(it => new X(it), new Lazy<A>(() => a));
            var getXExpr = se.FromSysExpression();
            getXExpr.PrintCSharp();

            var getX = getXExpr.CompileFast(true);

            var x = getX();
            Asserts.AreSame(a, x.A);
        }

        public class A { }

        public class X
        {
            public static X Get(Func<A, X> factory, Lazy<A> a)
            {
                return factory(a.Value);
            }

            public A A { get; }

            public X(A a)
            {
                A = a;
            }
        }
    }
}

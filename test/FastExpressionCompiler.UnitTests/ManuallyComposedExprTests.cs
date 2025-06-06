﻿using System;
using System.Linq;
using System.Reflection;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{

    public class ManuallyComposedExprTests : ITest
    {
        public int Run()
        {
            Should_compile_manually_composed_expr();
            Should_compile_manually_composed_expr_with_parameters();
            return 2;
        }


        public void Should_compile_manually_composed_expr()
        {
            var manualExpr = ComposeManualExpr();

            var lambda = manualExpr.CompileFast(true);

            Asserts.IsInstanceOf<X>(lambda());
        }

        private static Expression<Func<object>> ComposeManualExpr()
        {
            var a = new A();
            var b = new B();
            var e = Lambda<Func<object>>(
                New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
                    Constant(a, typeof(A)),
                    Constant(b, typeof(B))));
            return e;
        }


        public void Should_compile_manually_composed_expr_with_parameters()
        {
            var manualExpr = ComposeManualExprWithParams();

            var lambda = manualExpr.CompileFast<Func<B, X>>();

            Asserts.IsInstanceOf<X>(lambda(new B()));
        }

        private static LambdaExpression ComposeManualExprWithParams()
        {
            var a = new A();
            var bParamExpr = Parameter(typeof(B), "b");

            var e = Lambda(
                New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
                    Constant(a, typeof(A)),
                    bParamExpr),
                bParamExpr);
            return e;
        }

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
    }
}

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using static FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.LightExpression.UnitTests
{
    [TestFixture]
    public class NestedLambdasSharedToExpressionCodeStringTest : ITest
    {
        public int Run()
        {
            Should_output_a_valid_expression_code();
            Test_the_output_expression_code();
            return 2;
        }

        [Test]
        public void Should_output_a_valid_expression_code()
        {
            var e = CreateExpression();
            var s = e.ToExpressionString();
            e.PrintCSharp();
            StringAssert.Contains("new Expression[17];", s);
        }

        private LightExpression.Expression<Func<A>> CreateExpression()
        {
            var test = Constant(new NestedLambdasSharedToExpressionCodeStringTest());
            var getOrAddMethod = test.Type.GetMethod(nameof(GetOrAdd));
            var d = Convert(
                Call(test, getOrAddMethod,
                    Constant(2),
                    Lambda<Func<object>>(New(_dCtor), typeof(object))),
                typeof(D));

            var c = Convert(
                Call(test, getOrAddMethod,
                    Constant(1),
                    Lambda<Func<object>>(New(_cCtor, d), typeof(object))),
                typeof(C));

            var b = Convert(
                Call(test, getOrAddMethod,
                    Constant(0),
                    Lambda<Func<object>>(New(_bCtor, c, d), typeof(object))),
                typeof(B));

            return Lambda<Func<A>>(New(_aCtor, b, c), typeof(A));
        }

        [Test]
        public void Test_the_output_expression_code()
        {
            var e = new Expression[17]; // unique expressions
            var expr = Lambda(/*$*/
              typeof(System.Func<FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest.A>),
              e[0] = New(/*2 args*/
                typeof(FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest.A).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                e[1] = Convert(
                  e[2] = Call(
                    e[3] = Constant(new FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest()),
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest).GetTypeInfo().GetDeclaredMethods("GetOrAdd").ToArray()[0],
                    e[4] = Constant(0),
                    e[5] = Lambda(/*$*/
                      typeof(System.Func<System.Object>),
                      e[6] = New(/*2 args*/
                        typeof(FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest.B).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                        e[7] = Convert(
                          e[8] = Call(
                            e[3],
                            typeof(FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest).GetTypeInfo().GetDeclaredMethods("GetOrAdd").ToArray()[0],
                            e[9] = Constant(1),
                            e[10] = Lambda(/*$*/
                              typeof(System.Func<System.Object>),
                              e[11] = New(/*1 args*/
                                typeof(FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest.C).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                                e[12] = Convert(
                                  e[13] = Call(
                                    e[3],
                                    typeof(FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest).GetTypeInfo().GetDeclaredMethods("GetOrAdd").ToArray()[0],
                                    e[14] = Constant(2),
                                    e[15] = Lambda(/*$*/
                                      typeof(System.Func<System.Object>),
                                      e[16] = New(/*0 args*/
                                        typeof(FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest.D).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]), new ParameterExpression[0])),
                                  typeof(FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest.D))), new ParameterExpression[0])),
                          typeof(FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest.C)),
                        e[12]), new ParameterExpression[0])),
                  typeof(FastExpressionCompiler.LightExpression.UnitTests.NestedLambdasSharedToExpressionCodeStringTest.B)),
                e[7]), new ParameterExpression[0]);
            var f = expr.CompileFast<Func<A>>(true);
            Assert.IsNotNull(f);
            var a = f();
            Assert.IsInstanceOf<A>(a);
        }

        public readonly object[] _objects = new object[3];
        private static readonly ConstructorInfo _aCtor = typeof(A).GetTypeInfo().DeclaredConstructors.First();
        private static readonly ConstructorInfo _bCtor = typeof(B).GetTypeInfo().DeclaredConstructors.First();
        private static readonly ConstructorInfo _cCtor = typeof(C).GetTypeInfo().DeclaredConstructors.First();
        private static readonly ConstructorInfo _dCtor = typeof(D).GetTypeInfo().DeclaredConstructors.First();

        public object GetOrAdd(int i, Func<object> getValue) =>
            _objects[i] ?? (_objects[i] = getValue());

        public class A
        {
            public B B { get; }
            public C C { get; }

            public A(B b, C c)
            {
                B = b;
                C = c;
            }
        }

        public class B
        {
            public C C { get; }
            public D D { get; }

            public B(C c, D d)
            {
                C = c;
                D = d;
            }
        }

        public class C
        {
            public D D { get; }

            public C(D d)
            {
                D = d;
            }
        }

        public class D
        {
        }
    }
}
using System;
using System.Linq.Expressions;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Nested_lambdas_assigned_to_vars : ITest
    {
        public int Run()
        {
            Test_shared_sub_expressions(); // todo: @wip
            Test_shared_sub_expressions_assigned_to_vars();
            return 2;
        }

        [Test]
        public void Test_shared_sub_expressions()
        {
            var e = CreateExpression();
            e.PrintCSharp();
            var @cs = (Func<A>)(() =>
                new A(
                    ((B)default(Nested_lambdas_assigned_to_vars)/*Please provide the non-default value for the constant!*/.GetOrAdd(
                        0,
                        (Func<B>)(() =>
                                new B(
                                    ((C)default(Nested_lambdas_assigned_to_vars)/*Please provide the non-default value for the constant!*/.GetOrAdd(
                                        1,
                                        (Func<C>)(() =>
                                                new C(((D)default(Nested_lambdas_assigned_to_vars)/*Please provide the non-default value for the constant!*/.GetOrAdd(
                                                    2,
                                                    (Func<D>)(() =>
                                                            new D()))))))),
                                    ((D)default(Nested_lambdas_assigned_to_vars)/*Please provide the non-default value for the constant!*/.GetOrAdd(
                                        2,
                                        (Func<D>)(() =>
                                                new D()))))))),
                    ((C)default(Nested_lambdas_assigned_to_vars)/*Please provide the non-default value for the constant!*/.GetOrAdd(
                        1,
                        (Func<C>)(() =>
                                new C(((D)default(Nested_lambdas_assigned_to_vars)/*Please provide the non-default value for the constant!*/.GetOrAdd(
                                    2,
                                    (Func<D>)(() =>
                                            new D())))))))));

            var f = e.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(f);
            Assert.IsNotNull(f());

            var d = f.TryGetDebugInfo();

            // 1, 1 - compiling B in A
            // 2, 2 - compiling C in B in A
            // 3, 3 - compiling D in C in B in A
            // 4    - trying to compile D in B - but already compiled
            // 5    - trying to compile C in A - but already compiled
            Assert.AreEqual(5, d.NestedLambdaCount);
            Assert.AreEqual(3, d.NestedLambdaCompiledTimesCount);
        }

        [Test]
        public void Test_shared_sub_expressions_assigned_to_vars()
        {
            var expr = CreateExpressionWithVars();

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.IsNotNull(f());
        }

        public readonly object[] _objects = new object[3];
        public object GetOrAdd(int i, Func<object> getValue) =>
            _objects[i] ?? (_objects[i] = getValue());

        private Expression<Func<A>> CreateExpression()
        {
            var test = Constant(new Nested_lambdas_assigned_to_vars());

            var d = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                    Constant(2),
                    Lambda(
                        New(typeof(D).GetConstructors()[0], new Expression[0]))),
                typeof(D));

            var c = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                    Constant(1),
                    Lambda(
                        New(typeof(C).GetConstructors()[0], d))),
                typeof(C));

            var b = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                    Constant(0),
                    Lambda(
                        New(typeof(B).GetConstructors()[0], c, d))),
                typeof(B));

            var fe = Lambda<Func<A>>(
                New(typeof(A).GetConstructors()[0], b, c));

            return fe;
        }

        private Expression<Func<A>> CreateExpressionWithVars()
        {
            var test = Constant(new Nested_lambdas_assigned_to_vars());

            var dVar = Parameter(typeof(D), "d");
            var cVar = Parameter(typeof(C), "c");
            var bVar = Parameter(typeof(B), "b");

            var fe = Lambda<Func<A>>(
                Block(typeof(A),
                    new[] { bVar, cVar, dVar },
                    Assign(dVar, Convert(
                        Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                            Constant(2),
                            Lambda(
                                New(typeof(D).GetConstructors()[0], new Expression[0]))),
                        typeof(D))),
                    Assign(cVar, Convert(
                        Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                            Constant(1),
                            Lambda(
                                New(typeof(C).GetConstructors()[0], dVar))),
                        typeof(C))),
                    Assign(bVar, Convert(
                        Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                            Constant(0),
                            Lambda(
                                New(typeof(B).GetConstructors()[0], cVar, dVar))),
                        typeof(B))),
                    New(typeof(A).GetConstructors()[0], bVar, cVar))
            );

            return fe;
        }

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

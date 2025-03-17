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
            Test_shared_sub_expressions();
            Test_shared_sub_expressions_assigned_to_vars();
            Test_shared_sub_expressions_with_3_dublicate_D();
            Test_shared_sub_expressions_with_non_passed_params_in_closure();
            Test_2_shared_lambdas_on_the_same_level();
            return 5;
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

            // var d = f.TryGetDebugInfo();

            // 1, 1 - compiling B in A
            // 2, 2 - compiling C in B in A
            // 3, 3 - compiling D in C in B in A
            // 4    - trying to compile D in B - but already compiled
            // 5    - trying to compile C in A - but already compiled
            // Asserts.AreEqual(5, d.NestedLambdaCount);
            // Asserts.AreEqual(3, d.NestedLambdaCompiledTimesCount);
        }

        [Test]
        public void Test_shared_sub_expressions_with_3_dublicate_D()
        {
            var e = CreateExpressionWith3DinA();
            e.PrintCSharp();

            var f = e.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(f);
            Assert.IsNotNull(f());

            // var d = f.TryGetDebugInfo();

            // 1, 1 - compiling D in A
            // 2, 2 - compiling B in A
            // 3, 3 - compiling C in B in A
            // 4    - trying to compile D in C in B in A - but already compiled
            // 5    - trying to compile D in B - but already compiled
            // 6    - trying to compile C in A - but already compiled
            // Asserts.AreEqual(6, d.NestedLambdaCount);
            // Asserts.AreEqual(3, d.NestedLambdaCompiledTimesCount);
        }

        [Test]
        public void Test_shared_sub_expressions_with_non_passed_params_in_closure()
        {
            var e = CreateExpressionWithNonPassedParamsInClosure();
            e.PrintCSharp();

            var f = e.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(f);

            var a = f(new Name("d1"), new Name("c1"), new Name("b1"));
            Asserts.AreEqual("d1", a.D1.Name.Value);
            Asserts.AreEqual("c1", a.C1.Name.Value);
            Asserts.AreEqual("b1", a.B1.Name.Value);

            // var d = f.TryGetDebugInfo();
            // Asserts.AreEqual(6, d.NestedLambdaCount);
            // Asserts.AreEqual(3, d.NestedLambdaCompiledTimesCount);
        }

        [Test]
        public void Test_2_shared_lambdas_on_the_same_level()
        {
            var e = CreateExpressionWith2SharedLambdasOnTheSameLevels();
            e.PrintCSharp();

            var f = e.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(f);

            var dd = f();
            Assert.IsNotNull(dd);
            Assert.AreSame(dd.D1, dd.D2);

            // var d = f.TryGetDebugInfo();
            // Asserts.AreEqual(2, d.NestedLambdaCount);
            // Asserts.AreEqual(1, d.NestedLambdaCompiledTimesCount);
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

        public object GetOrPut(int i, Func<object> getValue) =>
            _objects[i] = getValue();

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

        private Expression<Func<A1>> CreateExpressionWith3DinA()
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

            var fe = Lambda<Func<A1>>(
                New(typeof(A1).GetConstructors()[0], d, b, c));

            return fe;
        }

        private Expression<Func<Name, Name, Name, A2>> CreateExpressionWithNonPassedParamsInClosure()
        {
            var test = Constant(new Nested_lambdas_assigned_to_vars());

            var b1Name = Parameter(typeof(Name), "b1Name");
            var c1Name = Parameter(typeof(Name), "c1Name");
            var d1Name = Parameter(typeof(Name), "d1Name");

            var d1 = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrPut)),
                    Constant(2),
                    Lambda(
                        New(typeof(D1).GetConstructors()[0], d1Name))),
                typeof(D1));

            var c1 = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrPut)),
                    Constant(1),
                    Lambda(
                        New(typeof(C1).GetConstructors()[0], d1, c1Name))),
                typeof(C1));

            var b1 = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrPut)),
                    Constant(0),
                    Lambda(
                        New(typeof(B1).GetConstructors()[0], c1, b1Name, d1))),
                typeof(B1));

            var fe = Lambda<Func<Name, Name, Name, A2>>(
                New(typeof(A2).GetConstructors()[0], d1, c1, b1),
                d1Name, c1Name, b1Name);

            return fe;
        }

        private Expression<Func<DD>> CreateExpressionWith2SharedLambdasOnTheSameLevels()
        {
            var test = Constant(new Nested_lambdas_assigned_to_vars());

            var d = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                    Constant(2),
                    Lambda(
                        New(typeof(D).GetConstructors()[0]))),
                typeof(D));

            var fe = Lambda<Func<DD>>(
                New(typeof(DD).GetConstructors()[0], d, d));

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

        public class A1
        {
            public B B { get; }
            public C C { get; }
            public D D { get; }

            public A1(D d, B b, C c)
            {
                B = b;
                C = c;
                D = d;
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

        public class DD
        {
            public D D1, D2;
            public DD(D d1, D d2)
            {
                D1 = d1;
                D2 = d2;
            }
        }

        public class Name
        {
            public string Value { get; }
            public Name(string value) => Value = value;
        }

        public class D1
        {
            public Name Name { get; }
            public D1(Name name) => Name = name;
        }

        public class C1
        {
            public Name Name { get; }
            public D1 D1 { get; }
            public C1(D1 d1, Name name)
            {
                D1 = d1;
                Name = name;
            }
        }

        public class B1
        {
            public Name Name { get; }
            public C1 C1 { get; }
            public D1 D1 { get; }
            public B1(C1 c1, Name name, D1 d1)
            {
                C1 = c1;
                D1 = d1;
                Name = name;
            }
        }

        public class A2
        {
            public B1 B1 { get; }
            public C1 C1 { get; }
            public D1 D1 { get; }
            public A2(D1 d1, C1 c1, B1 b1)
            {
                B1 = b1;
                C1 = c1;
                D1 = d1;
            }
        }
    }
}

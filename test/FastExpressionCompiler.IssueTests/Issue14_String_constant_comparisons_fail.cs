using System;
using System.Text;
using NUnit.Framework;
#pragma warning disable 659

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue14_String_constant_comparisons_fail : ITest
    {
        public int Run()
        {
            Guid_equality_should_work();
            Guid_not_equality_should_work();
            Enum_equality_should_work();
            Class_Equals_equality_should_work();

            String_equality_should_work();
            String_not_equality_should_work();

            return 6;
        }

        [Test]
        public void String_equality_should_work()
        {
            System.Linq.Expressions.Expression<Func<string, bool>> se = str => str == "Hello";
            var isHelloExpr = se.FromSysExpression();

            var isHello = isHelloExpr.CompileFast<Func<string, bool>>(true);

            Asserts.IsTrue(isHello("Hello"));
            Asserts.IsFalse(isHello("Blah"));

            // this is needed because for the string literal above it does reference comparison, and here it does op_Equality
            Asserts.IsTrue(isHello(new StringBuilder("Hello").ToString()));
        }

        [Test]
        public void String_not_equality_should_work()
        {
            System.Linq.Expressions.Expression<Func<string, bool>> se = str => str != "Hello";
            var isHelloExpr = se.FromSysExpression();

            var isHello = isHelloExpr.CompileFast<Func<string, bool>>(true);

            Asserts.IsFalse(isHello("Hello"));
            Asserts.IsTrue(isHello("Blah"));

            // this is needed because for the string literal above it does reference comparison, and here it does op_Equality
            Asserts.IsFalse(isHello(new StringBuilder("Hello").ToString()));
        }

        [Test]
        public void Guid_equality_should_work()
        {
            var expectedId = Guid.NewGuid();
            var expectedIdExpr = Constant(expectedId);

            var idParamExpr = Parameter(typeof(Guid), "id");
            var isExpectedIdExpr = Lambda(Equal(idParamExpr, expectedIdExpr), idParamExpr);

            var isExpectedId = isExpectedIdExpr.CompileFast<Func<Guid, bool>>(true);

            Asserts.IsTrue(isExpectedId(expectedId));
        }

        [Test]
        public void Guid_not_equality_should_work()
        {
            var expectedId = Guid.NewGuid();
            var expectedIdExpr = Constant(expectedId);

            var idParamExpr = Parameter(typeof(Guid), "id");
            var isExpectedIdExpr = Lambda(NotEqual(idParamExpr, expectedIdExpr), idParamExpr);

            var isExpectedId = isExpectedIdExpr.CompileFast<Func<Guid, bool>>(true);

            Asserts.IsFalse(isExpectedId(expectedId));
        }

        [Test]
        public void Enum_equality_should_work()
        {
            var expectedExpr = Constant(Blah.Bar, typeof(Blah));

            var paramExpr = Parameter(typeof(Blah), "x");
            var isExpectedExpr = Lambda(Equal(paramExpr, expectedExpr), paramExpr);

            var isExpected = isExpectedExpr.CompileFast<Func<Blah, bool>>(true);

            Asserts.IsTrue(isExpected(Blah.Bar));
            Asserts.IsFalse(isExpected(Blah.Foo));
        }

        enum Blah { Foo, Bar }

        [Test]
        public void Class_Equals_equality_should_work()
        {
            var expected = new Pooh(42);
            var expectedExpr = Constant(expected, typeof(Pooh));

            var paramExpr = Parameter(typeof(Pooh), "x");
            var isExpectedExpr = Lambda(Equal(paramExpr, expectedExpr), paramExpr);

            var isExpected = isExpectedExpr.CompileFast<Func<Pooh, bool>>(true);

            Asserts.IsTrue(isExpected(expected));
            Asserts.IsFalse(isExpected(new Pooh(53)));
        }

        class Pooh
        {
            internal int N;
            public Pooh(int n) => N = n;
            public override bool Equals(object obj) => (obj as Pooh)?.N == N;
        }
    }
}

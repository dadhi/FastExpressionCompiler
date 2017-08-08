using System;
using System.Linq.Expressions;
using System.Text;
using NUnit.Framework;
#pragma warning disable 659

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue14_String_constant_comparisons_fail
    {
        [Test]
        public void String_equality_should_work()
        {
            Expression<Func<string, bool>> isHelloExpr = str => str == "Hello";

            var isHello = isHelloExpr.CompileFast<Func<string, bool>>(true);

            Assert.IsTrue(isHello("Hello"));
            Assert.IsFalse(isHello("Blah"));

            // this is needed because for the string literal above it does reference comparison, and here it does op_Equality
            Assert.IsTrue(isHello(new StringBuilder("Hello").ToString()));
        }

        [Test]
        public void String_not_equality_should_work()
        {
            Expression<Func<string, bool>> isHelloExpr = str => str != "Hello";

            var isHello = isHelloExpr.CompileFast<Func<string, bool>>(true);

            Assert.IsFalse(isHello("Hello"));
            Assert.IsTrue(isHello("Blah"));

            // this is needed because for the string literal above it does reference comparison, and here it does op_Equality
            Assert.IsFalse(isHello(new StringBuilder("Hello").ToString())); 
        }

        [Test]
        public void Guid_equality_should_work()
        {
            var expectedId = Guid.NewGuid();
            var expectedIdExpr = Expression.Constant(expectedId);

            var idParamExpr = Expression.Parameter(typeof(Guid), "id");
            var isExpectedIdExpr = Expression.Lambda(Expression.Equal(idParamExpr, expectedIdExpr), idParamExpr);

            var isExpectedId = isExpectedIdExpr.CompileFast<Func<Guid, bool>>(true);

            Assert.IsTrue(isExpectedId(expectedId));
        }

        [Test]
        public void Guid_not_equality_should_work()
        {
            var expectedId = Guid.NewGuid();
            var expectedIdExpr = Expression.Constant(expectedId);

            var idParamExpr = Expression.Parameter(typeof(Guid), "id");
            var isExpectedIdExpr = Expression.Lambda(Expression.NotEqual(idParamExpr, expectedIdExpr), idParamExpr);

            var isExpectedId = isExpectedIdExpr.CompileFast<Func<Guid, bool>>(true);

            Assert.IsFalse(isExpectedId(expectedId));
        }

        [Test]
        public void Enum_equality_should_work()
        {
            var expectedExpr = Expression.Constant(Blah.Bar, typeof(Blah));

            var paramExpr = Expression.Parameter(typeof(Blah), "x");
            var isExpectedExpr = Expression.Lambda(Expression.Equal(paramExpr, expectedExpr), paramExpr);

            var isExpected = isExpectedExpr.CompileFast<Func<Blah, bool>>(true);

            Assert.IsTrue(isExpected(Blah.Bar));
            Assert.IsFalse(isExpected(Blah.Foo));
        }

        enum Blah { Foo, Bar }

        [Test]
        public void Class_Equals_equality_should_work()
        {
            var expected = new Pooh(42);
            var expectedExpr = Expression.Constant(expected, typeof(Pooh));

            var paramExpr = Expression.Parameter(typeof(Pooh), "x");
            var isExpectedExpr = Expression.Lambda(Expression.Equal(paramExpr, expectedExpr), paramExpr);

            var isExpected = isExpectedExpr.CompileFast<Func<Pooh, bool>>(true);

            Assert.IsTrue(isExpected(expected));
            Assert.IsFalse(isExpected(new Pooh(53)));
        }

        class Pooh
        {
            internal int N;
            public Pooh(int n) => N = n;
            public override bool Equals(object obj) => (obj as Pooh)?.N == N;
        }
    }
}

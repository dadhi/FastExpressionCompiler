using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class TypeBinaryExpressionTests : ITest
    {
        public int Run()
        {
            TypeEqual_should_work();
            TypeIs_should_work();
            TypeIs_more_advanced();

            return 3;
        }

        [Test]
        public void TypeEqual_should_work()
        {
            var sExpr = Parameter(typeof(object), "o");
            var expr = Lambda<Func<object, bool>>(
                TypeEqual(sExpr, typeof(A)),
                sExpr);

            var f = expr.CompileSys(); // TODO: CompileFast, but it does not work ATM
            Asserts.IsNotNull(f);
            bool result = f(new A());
            Asserts.IsTrue(result, expr.ToString());
            bool result2 = f(new B());
            Asserts.IsFalse(result2, expr.GetType().FullName);
        }

        class A { }
        class B : A { }

        [Test]
        public void TypeIs_should_work()
        {
            var sExpr = Parameter(typeof(object), "o");
            var expr = Lambda<Func<object, bool>>(
                TypeIs(sExpr, typeof(string)),
                sExpr);

            var f = expr.CompileFast(true);
            bool result = f("123");

            Asserts.IsTrue(result);
        }

        [Test]
        public void TypeIs_more_advanced()
        {
            var fromParam = Parameter(typeof(object));
            var exprInt = Lambda<Func<object, bool>>(TypeIs(fromParam, typeof(int)), fromParam).CompileFast(true);
            var exprX = Lambda<Func<object, bool>>(TypeIs(fromParam, typeof(S)), fromParam).CompileFast(true);
            var exprXEnum = Lambda<Func<object, bool>>(TypeIs(fromParam, typeof(E)), fromParam).CompileFast(true);
            var exprString = Lambda<Func<object, bool>>(TypeIs(fromParam, typeof(string)), fromParam).CompileFast(true);
            var exprIgnoredResult = Lambda<Action<object>>(TypeIs(fromParam, typeof(string)), fromParam).CompileFast(true);

            Asserts.AreEqual(true, exprInt(1));
            Asserts.AreEqual(false, exprInt("A"));
            Asserts.AreEqual(false, exprInt(1L));
            Asserts.AreEqual(true, exprX(new S()));
            Asserts.AreEqual(false, exprX("A"));
            Asserts.AreEqual(false, exprX(null));
            Asserts.AreEqual(true, exprXEnum(E.A));
            Asserts.AreEqual(false, exprXEnum("A"));
            Asserts.AreEqual(false, exprXEnum(null));
            Asserts.AreEqual(true, exprString("A"));
            Asserts.AreEqual(false, exprString(E.A));
            Asserts.AreEqual(false, exprXEnum(null));
            exprIgnoredResult(1);
            exprIgnoredResult("A");
        }

        struct S { }
        enum E { A }
    }
}

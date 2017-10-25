using NUnit.Framework;
using System;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue44_Conversion_To_Nullable_Throws_Exception
    {
        [Test]
        public void Conversion_to_nullable_should_work()
        {
            Expression<Func<int?>> expression = () => 42;
            int? answer = expression.CompileFast().Invoke();

            Assert.IsTrue(answer.HasValue);
            Assert.AreEqual(42, answer.Value);
        }

        [Test]
        public void Conversion_to_nullable_should_work_with_null()
        {
            Expression<Func<int?>> expression = () => null;
            int? answer = expression.CompileFast().Invoke();

            Assert.IsFalse(answer.HasValue);
        }

        [Test]
        public void Conversion_to_nullable_should_work_with_null_constructed_with_expressions()
        {
            var expr = Lambda<Func<int?>>(Convert(Constant(null), typeof(int?)));
            int? answer = expr.CompileFast().Invoke();

            Assert.IsFalse(answer.HasValue);
        }
    }
}

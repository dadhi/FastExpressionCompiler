using NUnit.Framework;
using System;
using System.Linq.Expressions;

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
    }
}

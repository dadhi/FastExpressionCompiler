using System;
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
    public class Issue153_MinValueMethodNotSupported : ITest
    {
        public int Run() 
        {
            Int_MinValue_Should_Work();
            return 1;
        }

        [Test]
        public void Int_MinValue_Should_Work()
        {
            var minValueField = typeof(int).GetField("MinValue");
            var minValue = Field(null, minValueField);
            var minValueLambda = Lambda<Func<int>>(minValue);
            var res = minValueLambda.CompileFast(true);
            Assert.AreEqual(int.MinValue, res());
        }
    }
}
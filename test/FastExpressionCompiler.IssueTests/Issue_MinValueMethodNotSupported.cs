using System;
using NUnit.Framework;
#pragma warning disable 659

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class Issue_MinValueMethodNotSupported
    {
        [Test, Ignore("Fails")]
        public void Int_MinValue_Should_Work()
        {
            var minValueField = typeof(int).GetField("MinValue");
            var minValue = Field(null, minValueField);

            var minValueLambda = Lambda<Func<int>>(minValue);

            // Throws 'Method not supported'
            minValueLambda.CompileFast();
        }
    }
}

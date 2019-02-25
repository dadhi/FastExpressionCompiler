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
    public class Issue181_TryEmitIncDecAssign_InvalidCastException
    {
        // originally seen in a Rezolver example, which I've tried to replicate as close as possible

        public int Counter { get; set; } = 1;


        [Test]
        public void TryEmitIncDecAssign_DoesntThrow_InvalidCastException()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Func<Issue181_TryEmitIncDecAssign_InvalidCastException, int>>(
                PreIncrementAssign(
                    Property(p, nameof(Counter))
                ),
                p);
 
            var del = lambda.CompileFast();
            Assert.AreEqual(Counter + 1, del.Invoke(this));
        }
    }
}
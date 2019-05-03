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

        public int CounterProperty { get; set; } = 1;

        public int CounterField = 2;

        public Counter CounterObjField = new Counter();

        public class Counter
        {
            public int CounterProperty { get; set; } = 3;

            public int CounterField = 4;
        }

        [Test]
        public void TryEmitIncDecAssign_Supports_PreIncrement_Property_Action()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PreIncrementAssign(
                    Property(p, nameof(CounterProperty))
                ),
                p);

            var del = lambda.CompileFast();

            var expectedValue = CounterProperty + 1;
            del.Invoke(this);

            Assert.AreEqual(expectedValue, CounterProperty);
        }

        [Test]
        public void TryEmitIncDecAssign_Supports_PreIncrement_Nested_Property_Action()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PreIncrementAssign(
                    Property(Field(p, nameof(CounterObjField)), nameof(CounterProperty))
                ),
                p);

            var del = lambda.CompileFast();

            var expectedValue = CounterObjField.CounterProperty + 1;
            del.Invoke(this);

            Assert.AreEqual(expectedValue, CounterObjField.CounterProperty);
        }

        [Test]
        public void TryEmitIncDecAssign_Supports_PreIncrement_Field_Action()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PreIncrementAssign(
                    Field(p, nameof(CounterField))
                ),
                p);

            var del = lambda.CompileFast();

            var expectedValue = CounterField + 1;
            del.Invoke(this);

            Assert.AreEqual(expectedValue, CounterField);
        }

        [Test]
        public void TryEmitIncDecAssign_Supports_PreIncrement_Nested_Field_Action()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PreIncrementAssign(
                    Field(Field(p, nameof(CounterObjField)), nameof(CounterField))
                ),
                p);

            var del = lambda.CompileFast();

            var expectedValue = CounterObjField.CounterField + 1;
            del.Invoke(this);

            Assert.AreEqual(expectedValue, CounterObjField.CounterField);
        }
    }
}

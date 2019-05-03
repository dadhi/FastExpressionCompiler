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
    using System.Reflection.Emit;

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
            //var method = new DynamicMethod(
            //    string.Empty,
            //    typeof(void),
            //    new[] { typeof(Issue181_TryEmitIncDecAssign_InvalidCastException) },
            //    typeof(ExpressionCompiler),
            //    skipVisibility: true);

            //var il = method.GetILGenerator();

            //var field = typeof(Issue181_TryEmitIncDecAssign_InvalidCastException).GetField("CounterField");

            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Dup);
            //il.Emit(OpCodes.Ldfld, field);
            //il.Emit(OpCodes.Ldc_I4_1);
            //il.Emit(OpCodes.Add);
            //il.Emit(OpCodes.Stfld, field);
            //il.Emit(OpCodes.Ret);

            //var @delegate = method.CreateDelegate(typeof(Action<Issue181_TryEmitIncDecAssign_InvalidCastException>), null);

            //var func = (Action<Issue181_TryEmitIncDecAssign_InvalidCastException>)@delegate;

            //func.Invoke(this);

            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PreIncrementAssign(
                    Property(p, nameof(CounterProperty))
                ),
                p);

            var del = lambda.CompileFast();

            var expectedValue = CounterField + 1;
            del.Invoke(this);

            Assert.AreEqual(expectedValue, CounterField);
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

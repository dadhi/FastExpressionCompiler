using System;

#pragma warning disable 659
#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    using System.Reflection;

    public class Issue181_TryEmitIncDecAssign_InvalidCastException : ITest
    {
        public int Run()
        {
            TryEmitIncDecAssign_Supports_PreIncrement_Property_Action();
            TryEmitIncDecAssign_Supports_PreIncrement_Nested_Property_Action();
            TryEmitIncDecAssign_Supports_PreIncrement_Field_Action();
            TryEmitIncDecAssign_Supports_PreIncrement_Nested_Field_Action();
            TryEmitIncDecAssign_Supports_PostIncrement_Property_Action();
            TryEmitIncDecAssign_Supports_PreDecrement_Nested_Property_Action();
            TryEmitIncDecAssign_Supports_PostDecrement_Field_Action();
            TryEmitIncDecAssign_Supports_PreIncrement_Nested_Property_Func();
            TryEmitIncDecAssign_Supports_PostIncrement_Field_Func();
            TryEmitIncDecAssign_Supports_PostIncrement_Static_Field_Func();
            TryEmitIncDecAssign_Supports_PreDecrement_Property_Func();
            TryEmitIncDecAssign_Supports_PreDecrement_Static_Property_Func();
            TryEmitIncDecAssign_Supports_PostDecrement_Nested_Field_Func();
            return 14;
        }

        // originally seen in a Rezolver example, which I've tried to replicate as close as possible

        public int CounterProperty { get; set; } = 1;

        public int CounterField = 2;

        public Counter CounterObjField = new Counter();

        public class Counter
        {
            public int CounterProperty { get; set; } = 3;

            public int CounterField = 4;
        }

        public static int CounterPropertyStatic { get; set; } = 5;

        public static int CounterFieldStatic = 6;


        public void TryEmitIncDecAssign_Supports_PreIncrement_Property_Action()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PreIncrementAssign(
                    Property(p, nameof(CounterProperty))
                ),
                p);

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);

            var expectedValue = CounterProperty + 1;
            del.Invoke(this);

            Asserts.AreEqual(expectedValue, CounterProperty);
        }


        public void TryEmitIncDecAssign_Supports_PreIncrement_Nested_Property_Action()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PreIncrementAssign(
                    Property(Field(p, nameof(CounterObjField)), nameof(CounterProperty))
                ),
                p);

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);

            var expectedValue = CounterObjField.CounterProperty + 1;
            del.Invoke(this);

            Asserts.AreEqual(expectedValue, CounterObjField.CounterProperty);
        }


        public void TryEmitIncDecAssign_Supports_PreIncrement_Field_Action()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PreIncrementAssign(
                    Field(p, nameof(CounterField))
                ),
                p);

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);

            var expectedValue = CounterField + 1;
            del.Invoke(this);

            Asserts.AreEqual(expectedValue, CounterField);
        }


        public void TryEmitIncDecAssign_Supports_PreIncrement_Nested_Field_Action()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PreIncrementAssign(
                    Field(Field(p, nameof(CounterObjField)), nameof(CounterField))
                ),
                p);

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);

            var expectedValue = CounterObjField.CounterField + 1;
            del.Invoke(this);

            Asserts.AreEqual(expectedValue, CounterObjField.CounterField);
        }


        public void TryEmitIncDecAssign_Supports_PostIncrement_Property_Action()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PostIncrementAssign(
                    Property(p, nameof(CounterProperty))
                ),
                p);

            lambda.PrintCSharp();

            var fs = lambda.CompileSys();
            fs.PrintIL();

            var expectedValue = CounterProperty + 1;
            fs.Invoke(this);
            Asserts.AreEqual(expectedValue, CounterProperty);

            var ff = lambda.CompileFast(true);
            ff.PrintIL();

            expectedValue = CounterProperty + 1;
            ff.Invoke(this);
            Asserts.AreEqual(expectedValue, CounterProperty);
        }


        public void TryEmitIncDecAssign_Supports_PreDecrement_Nested_Property_Action()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PreDecrementAssign(
                    Property(Field(p, nameof(CounterObjField)), nameof(CounterProperty))
                ),
                p);

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);

            var expectedValue = CounterObjField.CounterProperty - 1;
            del.Invoke(this);

            Asserts.AreEqual(expectedValue, CounterObjField.CounterProperty);
        }


        public void TryEmitIncDecAssign_Supports_PostDecrement_Field_Action()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Action<Issue181_TryEmitIncDecAssign_InvalidCastException>>(
                PostDecrementAssign(
                    Field(p, nameof(CounterField))
                ),
                p);

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);

            var expectedValue = CounterField - 1;
            del.Invoke(this);

            Asserts.AreEqual(expectedValue, CounterField);
        }


        public void TryEmitIncDecAssign_Supports_PreIncrement_Nested_Property_Func()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Func<Issue181_TryEmitIncDecAssign_InvalidCastException, int>>(
                PreIncrementAssign(
                    Property(Field(p, nameof(CounterObjField)), nameof(CounterProperty))
                ),
                p);

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);
            Asserts.AreEqual(CounterObjField.CounterProperty + 1, del.Invoke(this));
        }


        public void TryEmitIncDecAssign_Supports_PostIncrement_Field_Func()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Func<Issue181_TryEmitIncDecAssign_InvalidCastException, int>>(
                PostIncrementAssign(
                    Field(p, nameof(CounterField))
                ),
                p);

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);

            var startValue = CounterField;

            Asserts.AreEqual(startValue, del.Invoke(this));
            Asserts.AreEqual(startValue + 1, CounterField);
        }


        public void TryEmitIncDecAssign_Supports_PostIncrement_Static_Field_Func()
        {
            var staticField = typeof(Issue181_TryEmitIncDecAssign_InvalidCastException)
                .GetField(nameof(CounterFieldStatic), BindingFlags.Public | BindingFlags.Static);

            var lambda = Lambda<Func<int>>(
                PostIncrementAssign(
                    Field(null, staticField)
                ));

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);

            var startValue = CounterFieldStatic;

            Asserts.AreEqual(startValue, del.Invoke());
            Asserts.AreEqual(startValue + 1, CounterFieldStatic);
        }


        public void TryEmitIncDecAssign_Supports_PreDecrement_Property_Func()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Func<Issue181_TryEmitIncDecAssign_InvalidCastException, int>>(
                PreDecrementAssign(
                    Property(p, nameof(CounterProperty))
                ),
                p);

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);
            Asserts.AreEqual(CounterProperty - 1, del.Invoke(this));
        }


        public void TryEmitIncDecAssign_Supports_PreDecrement_Static_Property_Func()
        {
            var staticProperty = typeof(Issue181_TryEmitIncDecAssign_InvalidCastException)
                .GetProperty(nameof(CounterPropertyStatic), BindingFlags.Public | BindingFlags.Static);

            var lambda = Lambda<Func<int>>(
                PreDecrementAssign(
                    Property(null, staticProperty)
                ));

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);
            Asserts.AreEqual(CounterPropertyStatic - 1, del.Invoke());
        }


        public void TryEmitIncDecAssign_Supports_PostDecrement_Nested_Field_Func()
        {
            var p = Parameter(typeof(Issue181_TryEmitIncDecAssign_InvalidCastException));

            var lambda = Lambda<Func<Issue181_TryEmitIncDecAssign_InvalidCastException, int>>(
                PostDecrementAssign(
                    Field(Field(p, nameof(CounterObjField)), nameof(CounterField))
                ),
                p);

            var del = lambda.CompileFast(true);

            Asserts.IsNotNull(del);

            var startValue = CounterObjField.CounterField;

            Asserts.AreEqual(startValue, del.Invoke(this));
            Asserts.AreEqual(startValue - 1, CounterObjField.CounterField);
        }
    }
}

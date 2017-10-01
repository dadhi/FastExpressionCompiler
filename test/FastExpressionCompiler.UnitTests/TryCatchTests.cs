using System;
using NUnit.Framework;
using NUnit.Framework.Internal;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class TryCatchTests
    {
        [Test]
        public void Can_catch_exception()
        {
            var expr = Lambda<Action>(TryCatch(
                    Throw(Constant(new DivideByZeroException())),
                    Catch(typeof(DivideByZeroException), 
                        Throw(Constant(new InvalidTimeZoneException())
                    )
                )
            ));

            var func = expr.CompileFast(true);

            Assert.IsNotNull(func);
            Assert.Throws<InvalidTimeZoneException>(() => func());
        }

        [Test]
        public void Can_execute_finally()
        {
            var expr = Lambda<Action>(TryCatchFinally(
                Throw(Constant(new DivideByZeroException())),
                Throw(Constant(new InvalidDataSourceException())),
                Catch(typeof(DivideByZeroException),
                    Throw(Constant(new InvalidTimeZoneException())
                    )
                )
            ));

            var func = expr.CompileFast(true);

            Assert.IsNotNull(func);
            Assert.Throws<InvalidDataSourceException>(() => func());
        }

        /*TODO: Add suport for usage of exception parameter
        [Test]
        public void Can_use_exception_parameter()
        {
            var parExcep = Parameter(typeof(Exception), "exc");
            MethodInfo getExceptionMessage = typeof(Exception)
                .GetProperty(nameof(Exception.Message), BindingFlags.Public | BindingFlags.Instance).GetMethod;
            MethodInfo writeLine = typeof(Console).GetMethod(nameof(Console.WriteLine), new [] { typeof(string) });

            var expr = Lambda<Action>(TryCatch(
                Throw(Constant(new DivideByZeroException())),
                Catch(
                    parExcep,
                    Call(
                        writeLine,
                        Call(parExcep, getExceptionMessage)
                    )
                )
            ));

            var func = expr.CompileFast(true);
            Assert.IsNotNull(func);
            Assert.DoesNotThrow(()=> func());
        }*/

        /*TODO: Add suport of try-catch expression in non-void method.
        [Test]
        public void Can_return_from_catch_block()
        {
            Expression<Func<bool>> expr = Lambda<Func<bool>>(TryCatch(
                Block(
                    Throw(Constant(new DivideByZeroException())),
                    Constant(false)
                ),
                Catch(
                    typeof(DivideByZeroException),
                    Constant(true)
                )
            ));

            var func = expr.CompileFast(true);

            Assert.IsNotNull(func);
            Assert.IsTrue(func());
        }*/

        [Test]
        public void Can_throw_an_exception()
        {
            var expr = Lambda<Action>(Throw(Constant(new DivideByZeroException())));

            var func = expr.CompileFast(true);

            Assert.IsNotNull(func);
            Assert.Throws<DivideByZeroException>(() => func());
        }
    }
}

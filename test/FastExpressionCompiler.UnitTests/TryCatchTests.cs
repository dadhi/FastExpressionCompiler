using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Internal;
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
            var expr = Lambda<Action>(
                TryCatchFinally(
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

        [Test]
        public void Can_handle_the_exception_and_return_result_from_TryCatch_block()
        {
            // Test expression
            // (string a) => {
            //      try { return int.Parse(a); }
            //      catch (Exception ex) { return ex.Message.Length; }
            // }

            var aParamExpr = Parameter(typeof(string), "a");
            var exParamExpr = Parameter(typeof(Exception), "ex");

            var expr = TryCatch(
                Call(typeof(int).GetTypeInfo()
                    .DeclaredMethods.First(m => m.Name == nameof(int.Parse)),
                    aParamExpr
                ),
                Catch(exParamExpr,
                    Property(
                        Property(exParamExpr, typeof(Exception).GetTypeInfo()
                            .DeclaredProperties.First(p => p.Name == nameof(Exception.Message))),
                        typeof(string).GetTypeInfo()
                            .DeclaredProperties.First(p => p.Name == nameof(string.Length))
                    )
                )
            );

            // Test that expression is valid with system Compile
            var fExpr = Lambda<Func<string, int>>(expr, aParamExpr);

            var ff = fExpr.CompileFast(ifFastFailedReturnNull: true);
            Assert.IsNotNull(ff);

            Assert.AreEqual(41, ff("A"));
            Assert.AreEqual(123, ff("123"));
        }

        //TODO: Add support for usage of exception parameter in void action
        //[Test]
        //public void Can_use_exception_parameter()
        //{
        //    var exPar = Parameter(typeof(Exception), "exc");
        //    var getExceptionMessage = typeof(Exception)
        //        .GetProperty(nameof(Exception.Message), BindingFlags.Public | BindingFlags.Instance).GetMethod;
        //    var writeLine = typeof(Console).GetMethod(nameof(Console.WriteLine), new [] { typeof(string) });

        //    var expr = Lambda<Action>(TryCatch(
        //        Throw(Constant(new DivideByZeroException())),
        //        Catch(
        //            exPar,
        //            Call(
        //                writeLine,
        //                Call(exPar, getExceptionMessage)
        //            )
        //        )
        //    ));

        //    var func = expr.CompileFast(true);
        //    Assert.IsNotNull(func);
        //    Assert.DoesNotThrow(()=> func());
        //}

        [Test]
        public void Can_return_from_catch_block()
        {
            var expr = Lambda<Func<bool>>(TryCatch(
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
        }

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

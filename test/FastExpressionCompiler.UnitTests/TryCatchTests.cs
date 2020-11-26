using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Internal;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class TryCatchTests : ITest
    {
        public int Run()
        {
            Can_catch_exception();
            Can_execute_finally();
            Can_handle_the_exception_and_return_result_from_TryCatch_block();
            Can_use_exception_parameter();
            Can_return_from_catch_block();
            Can_throw_an_exception();
            Can_return_from_try_block_using_label();
            Can_return_from_catch_block_using_label();
            Can_return_try_block_result_using_label();
            Can_return_nested_catch_block_result();

            return 10;
        }

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
                    Condition(
                        GreaterThan(
                            Property(
                                Property(exParamExpr, typeof(Exception).GetTypeInfo()
                                    .DeclaredProperties.First(p => p.Name == nameof(Exception.Message))),
                                typeof(string).GetTypeInfo()
                                    .DeclaredProperties.First(p => p.Name == nameof(string.Length))
                            ),
                            Constant(0)),
                        Constant(47),
                        Constant(0)
                    ))
            );

            // Test that expression is valid with system Compile
            var fExpr = Lambda<Func<string, int>>(expr, aParamExpr);

            var ff = fExpr.CompileFast(ifFastFailedReturnNull: true);
            Assert.IsNotNull(ff);

            Assert.AreEqual(47, ff("A"));
            Assert.AreEqual(123, ff("123"));
        }

        [Test]
        public void Can_use_exception_parameter()
        {
            var exPar = Parameter(typeof(Exception), "exc");
            var getExceptionMessage = typeof(Exception)
                .GetProperty(nameof(Exception.Message), BindingFlags.Public | BindingFlags.Instance).GetMethod;
            var writeLine = typeof(Debug).GetMethod(nameof(Debug.WriteLine), new[] { typeof(string) });

            var expr = Lambda<Action>(TryCatch(
                Throw(Constant(new DivideByZeroException())),
                Catch(
                    exPar,
                    Call(
                        writeLine,
                        Call(exPar, getExceptionMessage)
                    )
                )
            ));

            var func = expr.CompileFast(true);
            Assert.IsNotNull(func);
            Assert.DoesNotThrow(() => func());
        }

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

        [Test]
        public void Can_return_from_try_block_using_label()
        {
            var returnLabel = Label(typeof(string));

            var expr = Lambda<Func<string>>(Block(
                TryCatch(
                    Return(returnLabel, Constant("From Try block"), typeof(string)),
                    Catch(
                        typeof(Exception),
                        Return(returnLabel, Constant("From Catch block"), typeof(string))
                    )
                ),
                Label(returnLabel, Default(returnLabel.Type))));

            var func = expr.CompileFast(true);

            Assert.IsNotNull(func);
            Assert.AreEqual("From Try block", func());
        }

        [Test]
        public void Can_return_from_catch_block_using_label()
        {
            var returnLabel = Label(typeof(string));

            var expr = Lambda<Func<string>>(
                Block(
                    TryCatch(
                        Throw(New(typeof(Exception).GetConstructor(Type.EmptyTypes)), typeof(string)),
                        Catch(typeof(Exception), Return(returnLabel, Constant("From Catch block"), typeof(string)))
                    ),
                    Label(returnLabel, Default(returnLabel.Type))
                ));

            expr.PrintCSharp();

            var funcSys = expr.CompileSys();
            Assert.AreEqual("From Catch block", funcSys());

            var func = expr.CompileFast(true);
            Assert.AreEqual("From Catch block", func());

            var funcWithoutClosure = expr.TryCompileWithoutClosure<Func<string>>();// ?? expr.CompileSys();
            Assert.IsNull(funcWithoutClosure);
        }

        [Test]
        public void Can_return_try_block_result_using_label()
        {
            var returnType = typeof(string);
            var innerReturnLabel = Label(returnType);
            var outerReturnLabel = Label(returnType);

            var expr = Lambda<Func<string>>(Block(
                TryCatch(
                    Return(
                        outerReturnLabel,
                        Block(
                            TryCatch(
                                Return(innerReturnLabel, Constant("From inner Try block"), returnType),
                                Catch(
                                    typeof(Exception),
                                    Return(innerReturnLabel, Constant("From inner Catch block"), returnType)
                                )
                            ),
                            Label(innerReturnLabel, Default(innerReturnLabel.Type))),
                        returnType),
                    Catch(
                        typeof(Exception),
                        Return(outerReturnLabel, Constant("From outer Catch block"), returnType)
                    )
                ),
                Label(outerReturnLabel, Default(outerReturnLabel.Type))));

            var func = expr.CompileFast(true);

            Assert.IsNotNull(func);
            Assert.AreEqual("From inner Try block", func());
        }

        [Test]
        public void Can_return_nested_catch_block_result()
        {
            var returnType = typeof(string);
            var innerReturnLabel = Label(returnType);
            var outerReturnLabel = Label(returnType);

            var expr = Lambda<Func<string>>(Block(
                TryCatch(
                    Return(
                        outerReturnLabel,
                        Block(
                            TryCatch(
                                Throw(New(typeof(Exception).GetConstructor(Type.EmptyTypes)), returnType),
                                Catch(
                                    typeof(Exception),
                                    Return(innerReturnLabel, Constant("From inner Catch block"), returnType)
                                )
                            ),
                            Label(innerReturnLabel, Default(innerReturnLabel.Type))),
                        returnType),
                    Catch(
                        typeof(Exception),
                        Return(outerReturnLabel, Constant("From outer Catch block"), returnType)
                    )
                ),
                Label(outerReturnLabel, Default(outerReturnLabel.Type))));

            var func = expr.CompileFast(true);

            Assert.IsNotNull(func);
            Assert.AreEqual("From inner Catch block", func());
        }
    }
}

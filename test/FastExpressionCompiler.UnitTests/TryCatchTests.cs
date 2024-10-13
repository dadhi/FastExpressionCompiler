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
            // todo: @fixme
            // Issue424_Can_be_nested_in_call_expression();
            Can_be_nested_in_binary();
            Can_catch_exception();
            Can_execute_finally();
            Can_handle_the_exception_and_return_result_from_TryCatch_block();
            Can_use_exception_parameter();
            Can_return_from_catch_block();
            Can_return_with_return_goto_from_the_catch_block();
            Can_throw_an_exception();
            Can_return_from_try_block_using_label();
            Can_return_from_catch_block_using_label();
            Can_return_try_block_result_using_label_from_the_inner_try();
            Can_return_nested_catch_block_result();
            Can_return_from_try_block_using_goto_to_label_with_default_value();
            Can_return_from_try_block_using_goto_to_label_with_the_more_code_after_label();
            Can_rethrow();
            Can_rethrow_void();
            Can_rethrow_or_wrap();
            Can_rethrow_or_suppress();

            return 19;
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
            var aParamExpr = Parameter(typeof(string), "a");
            var exParamExpr = Parameter(typeof(Exception), "ex");

            var expr = TryCatch(
                Call(typeof(int).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(int.Parse)),
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

            var fe = Lambda<Func<string, int>>(expr, aParamExpr);
            fe.PrintCSharp();
            // should print this:
            var f = (Func<string, int>)((string a) => //$
            {
                try
                {
                    return int.Parse(a);
                }
                catch (Exception ex)
                {
                    return ex.Message.Length > (int)0 ?
                        (int)47 :
                        (int)0;
                }
            });

            var fs = fe.CompileSys();
            fs.PrintIL();
            Assert.AreEqual(47, fs("A"));
            Assert.AreEqual(123, fs("123"));

            var ff = fe.CompileFast(ifFastFailedReturnNull: true);
            ff.PrintIL();
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
                Catch(typeof(DivideByZeroException),
                    Constant(true)
                )
            ));

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL("sys");
            Assert.IsTrue(fs());

            var f = expr.CompileFast(true);
            f.PrintIL("fec");
            Assert.IsTrue(f());
        }

        [Test]
        public void Can_return_with_return_goto_from_the_catch_block()
        {
            var returnLabel = Label(typeof(bool));

            var expr = Lambda<Func<bool>>(TryCatch(
                Block(
                    Throw(Constant(new DivideByZeroException())),
                    Constant(false)
                ),
                Catch(typeof(DivideByZeroException),
                    Block(
                        Return(returnLabel, Constant(true)),
                        Label(returnLabel, Constant(false))
                    )
                )
            ));

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL("sys");
            Assert.IsTrue(fs());

            var f = expr.CompileFast();
            f.PrintIL("fec");
            Assert.IsTrue(f());
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

            var expr = Lambda<Func<string>>(
                Block(
                    TryCatch(
                        Return(returnLabel, Constant("From Try block"), typeof(string)),
                        Catch(
                            typeof(Exception),
                            Return(returnLabel, Constant("From Catch block"), typeof(string))
                        )
                    ),
                    Label(returnLabel, Default(returnLabel.Type))));

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Assert.AreEqual("From Try block", fs());

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            Assert.AreEqual("From Try block", ff());
        }

        [Test]
        public void Can_return_from_try_block_using_goto_to_label_with_default_value()
        {
            var label = Label(typeof(string));
            var result = Parameter(typeof(string), "s");

            var expr = Lambda<Func<string>>(Block(
                typeof(string),
                new[] { result },
                TryCatch(
                        Goto(label, Constant("From Try block"), typeof(string)),
                    Catch(typeof(Exception),
                        Goto(label, Constant("From Catch block"), typeof(string))
                    )
                ),
                Label(label, result)
            ));

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Assert.AreEqual("From Try block", fs());

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            Assert.AreEqual("From Try block", ff());
        }

        [Test]
        public void Can_return_from_try_block_using_goto_to_label_with_the_more_code_after_label()
        {
            var label = Label(typeof(void));
            var result = Parameter(typeof(string), "s");

            var expr = Lambda<Func<string>>(Block(
                new[] { result },
                TryCatch(
                        // Block(
                        // Assign(result, Constant("trying...")),
                        Goto(label, Constant("From Try block"), typeof(string)),
                    Catch(typeof(Exception),
                        // Block(
                        // Assign(result, Constant("catching..."))
                        Goto(label, Constant("From Catch block"), typeof(string))
                    )
                ),
                Label(label),
                Assign(result, Constant("the end")),
                result
            ));

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Assert.AreEqual("the end", fs());

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            Assert.AreEqual("the end", ff());
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
        public void Can_return_try_block_result_using_label_from_the_inner_try()
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
                                Catch(typeof(Exception),
                                    Return(innerReturnLabel, Constant("From inner Catch block"), returnType)
                                )
                            ),
                            Label(innerReturnLabel, Default(innerReturnLabel.Type))),
                        returnType),
                    Catch(typeof(Exception),
                        Return(outerReturnLabel, Constant("From outer Catch block"), returnType)
                    )
                ),
                Label(outerReturnLabel, Default(outerReturnLabel.Type))));

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Assert.AreEqual("From inner Try block", fs());

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            Assert.AreEqual("From inner Try block", ff());
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

        [Test]
        public void Can_rethrow()
        {
            var exceptions = new System.Collections.Generic.List<Exception>();
            var pFn = Parameter(typeof(Func<string>), "fn");
            var pEx = Parameter(typeof(Exception), "ex");
            var expr = Lambda<Func<Func<string>, string>>(
                TryCatch(
                    Invoke(pFn),
                    Catch(
                        pEx,
                        Block(
                            Invoke(Constant(new Action<Exception>(exceptions.Add)), pEx),
                            Rethrow(typeof(string))
                        )
                    )
                ),
                pFn
            );

            var compiledFast = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
            var compiled = expr.CompileSys();
            compiledFast.PrintIL();

            // no exception
            Assert.AreEqual("ok", compiledFast(() => "ok"));
            Assert.AreEqual("ok", compiled(() => "ok"));
            // rethrown exception
            Assert.IsEmpty(exceptions);
            Assert.Throws<ArgumentException>(() => compiledFast(() => { throw new ArgumentException(); }));
            Assert.That(exceptions, Has.Count.EqualTo(1));
            Assert.Throws<ArgumentException>(() => compiled(() => { throw new ArgumentException(); }));
            Assert.That(exceptions, Has.Count.EqualTo(2));
        }

        [Test]
        public void Can_rethrow_void()
        {
            var pFn = Parameter(typeof(Action), "fn");
            var pEx = Parameter(typeof(Exception), "ex");
            var expr = Lambda<Action<Action>>(
                TryCatch(
                    Invoke(pFn),
                    Catch(
                        pEx,
                        Rethrow(typeof(void))
                    )
                ),
                pFn
            );

            var compiledFast = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
            var compiled = expr.CompileSys();
            compiledFast.PrintIL();

            // no exception
            var executed = 0;
            compiledFast(() => executed++);
            Assert.AreEqual(1, executed);
            compiled(() => executed++);
            Assert.AreEqual(2, executed);
            // rethrown exception
            Assert.Throws<ArgumentException>(() => compiledFast(() => { throw new ArgumentException(); }));
            Assert.Throws<ArgumentException>(() => compiled(() => { throw new ArgumentException(); }));
        }

        [Test]
        public void Can_rethrow_or_wrap()
        {
            var pFn = Parameter(typeof(Func<string>), "fn");
            var pEx = Parameter(typeof(Exception), "ex");
            var expr = Lambda<Func<Func<string>, string>>(
                TryCatch(
                    Invoke(pFn),
                    Catch(
                        pEx,
                        // ex is ArgumentException ? throw : throw new Exception("wrapped", ex);
                        Condition(
                            TypeIs(pEx, typeof(ArgumentException)),
                            Rethrow(typeof(string)),
                            Throw(
                                New(typeof(Exception).GetConstructor(new[] { typeof(string), typeof(Exception) }), Constant("wrapped"), pEx),
                                typeof(string)
                            )
                        )
                    )
                ),
                pFn
            );

            var compiledFast = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
            var compiled = expr.CompileSys();
            compiledFast.PrintIL();

            // no exception
            Assert.AreEqual("ok", compiledFast(() => "ok"));
            Assert.AreEqual("ok", compiled(() => "ok"));
            // rethrown exception
            Assert.Throws<ArgumentException>(() => compiledFast(() => { throw new ArgumentException(); }));
            Assert.Throws<ArgumentException>(() => compiled(() => { throw new ArgumentException(); }));
            // wrapped exception
            var exception = Assert.Throws<Exception>(() => compiledFast(() => { throw new InvalidOperationException(); }));
            Assert.AreEqual("wrapped", exception.Message);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
            exception = Assert.Throws<Exception>(() => compiled(() => { throw new InvalidOperationException(); }));
            Assert.AreEqual("wrapped", exception.Message);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
        }

        [Test]
        public void Can_rethrow_or_suppress()
        {
            var pFn = Parameter(typeof(Func<string>), "fn");
            var pEx = Parameter(typeof(Exception), "ex");
            var expr = Lambda<Func<Func<string>, string>>(
                TryCatch(
                    Invoke(pFn),
                    Catch(
                        pEx,
                        // ex is ArgumentException ? throw : "exception suppressed";
                        Condition(
                            TypeIs(pEx, typeof(ArgumentException)),
                            Rethrow(typeof(string)),
                            Constant("exception suppressed")
                        )
                    )
                ),
                pFn
            );

            var compiledFast = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
            var compiled = expr.CompileSys();
            compiledFast.PrintIL();

            // no exception
            Assert.AreEqual("ok", compiledFast(() => "ok"));
            Assert.AreEqual("ok", compiled(() => "ok"));
            // rethrown exception
            Assert.Throws<ArgumentException>(() => compiledFast(() => { throw new ArgumentException(); }));
            Assert.Throws<ArgumentException>(() => compiled(() => { throw new ArgumentException(); }));
            // caught exception
            Assert.AreEqual("exception suppressed", compiledFast(() => { throw new InvalidOperationException(); }));
            Assert.AreEqual("exception suppressed", compiled(() => { throw new InvalidOperationException(); }));

            // throw null;
        }

        [Test]
        public void Can_be_nested_in_binary()
        {
            var p = Parameter(typeof(Func<int>), "p");
            var expr = Lambda<Func<Func<int>, int>>(Add(
                Constant(1),
                TryCatch(
                    Invoke(p),
                    Catch(typeof(Exception),
                        Constant(0)
                    )
                )
            ), p);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            Assert.AreEqual(3, fs(() => 2));
            Assert.AreEqual(1, fs(() => throw new Exception()));

            var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
            Assert.AreEqual(3, ff(() => 2));
            Assert.AreEqual(1, ff(() => throw new Exception()));
        }

        [Test]
        public void Issue424_Can_be_nested_in_call_expression()
        {
            var pa = Parameter(typeof(Func<string>), "pa");
            var pb = Parameter(typeof(Func<string>), "pb");
            var pc = Parameter(typeof(Func<string>), "pc");
            var ex = Parameter(typeof(Exception), "ex");
            var expr = Lambda<Func<Func<string>, Func<string>, Func<string>, string>>(
                Call(
                    typeof(TryCatchTests).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(TestMethod)),
                    TryCatch(
                        Invoke(pa),
                        Catch(ex,
                            Property(ex, "Message")
                        )
                    ),
                    TryCatch(
                        Invoke(pb),
                        Catch(ex,
                            Property(ex, "Message")
                        )
                    ),
                    TryCatch(
                        Invoke(pc),
                        Catch(ex,
                            Property(ex, "Message")
                        )
                    )
                ),
                pa, pb, pc);

            expr.PrintCSharp();
            // var @cs = (Func<Func<string>, Func<string>, Func<string>, string>)((
            //     Func<string> pa, 
            //     Func<string> pb, 
            //     Func<string> pc) => //string
            //     TryCatchTests.TestMethod(
            //     try
            //     {
            //         return pa.Invoke();
            //     }
            //     catch (Exception ex)
            //     {
            //         return ex.Message;
            //     },
            //     try
            //     {
            //         return pb.Invoke();
            //     }
            //     catch (Exception ex)
            //     {
            //         return ex.Message;
            //     },
            //     try
            //     {
            //         return pc.Invoke();
            //     }
            //     catch (Exception ex)
            //     {
            //         return ex.Message;
            //     }));

            var fs = expr.CompileSys();
            Assert.AreEqual("a b c", fs(() => "a", () => "b", () => "c"));
            Assert.AreEqual("a errB c", fs(() => "a", () => throw new Exception("errB"), () => "c"));

            var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
            Assert.AreEqual("a b c", ff(() => "a", () => "b", () => "c"));
            Assert.AreEqual("a errB c", ff(() => "a", () => throw new Exception("errB"), () => "c"));
        }

        public static string TestMethod(string a, string b, string c) => $"{a} {b} {c}";
    }
}

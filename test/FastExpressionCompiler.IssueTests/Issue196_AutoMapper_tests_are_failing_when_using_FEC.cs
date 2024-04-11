using System.Linq;
using System.Reflection;
using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
using static System.Environment;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue196_AutoMapper_tests_are_failing_when_using_FEC
    {
        public class FastExpressionCompilerBug : ITest
        {
            public int Run()
            {
                Test_the_tmp_var_block_reduction();
                Coalesce_should_work_with_throw();
                Coalesce_should_produce_optimal_opcodes();
                Comparison_with_null_should_produce_optimal_Brtrue_or_Brfalse_opcodes();
                Logical_OrElse_should_be_reduced_if_one_of_operands_is_known_boolean_value();
                Setting_the_outside_variable();
                TryCatch_setting_the_outside_variable();
                TryCatch_with_void_rethrows_error_in_catch();
                TryCatch_with_rethrow_error_in_catch_and_the_unreachable_code_after_the_throw();
                TryCatch_with_non_void_rethrows_error_in_catch();

                return 10;
            }

            public class Source
            {
                public int Value { get; set; }
            }

            public class Dest
            {
                public int Value { get; set; }
            }

            [Test]
            public void Comparison_with_null_should_produce_optimal_Brtrue_or_Brfalse_opcodes()
            {
                var p = Parameter(typeof(Source), "source");
                var sourceParam = p;
                var body = Condition(
                    Equal(sourceParam, Constant(null, typeof(Source))),
                    Constant(null, typeof(Dest)),
                    MemberInit(New(typeof(Dest).GetTypeInfo().DeclaredConstructors.First()),
                        Bind(typeof(Dest).GetTypeInfo().DeclaredProperties.First(),
                            Property(sourceParam, nameof(Source.Value)))));

                var expr = Lambda<Func<Source, Dest>>(body, sourceParam);

#if LIGHT_EXPRESSION

// todo: @improve to fragile for the test
//                 var exprCode = expr.CodeString;
// var expectedCode = 
// "Lambda(typeof(Func<Source, Dest>)," + NewLine +
// "Condition(Equal(" + NewLine +
// "Parameter(typeof(Source), \"source\")," + NewLine +
// "Constant(null, typeof(Source)))," + NewLine +
// "Constant(null, typeof(Dest))," + NewLine +
// "MemberInit(New(typeof(Dest).GetTypeInfo().DeclaredConstructors.ToArray()[0]," + NewLine +
// "new Expression[0])," + NewLine +
// "Bind(typeof(Dest).GetTypeInfo().DeclaredMembers.ToArray()[3]," + NewLine +
// "Property(Parameter(typeof(Source), \"source\")," + NewLine +
// "typeof(Source).GetTypeInfo().DeclaredProperties.ToArray()[0]))))," + NewLine +
// "Parameter(typeof(Source), \"source\"))";

//                 Assert.AreEqual(expectedCode, exprCode);

                var reincarnatedExpr = 
                    Lambda(typeof(Func<Source, Dest>),
                    Condition(Equal(
                    p,
                    Constant(null, typeof(Source))),
                    Constant(null, typeof(Dest)),
                    MemberInit(New(typeof(Dest).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                    new Expression[0]),
                    Bind(typeof(Dest).GetTypeInfo().DeclaredMembers.ToArray()[3], Property(p,
                    typeof(Source).GetTypeInfo().DeclaredProperties.ToArray()[0])))),
                    p);

                var reincarnatedCompiled = reincarnatedExpr.CompileFast(true);
                Assert.NotNull(reincarnatedCompiled);
#endif

                var ff = expr.CompileFast(true);

                var dest = ff(new Source {Value = 42});
                Assert.AreEqual(42, dest.Value);
            }

            [Test]
            public void Logical_OrElse_should_be_reduced_if_one_of_operands_is_known_boolean_value()
            {
                var sourceParam = Parameter(typeof(Source), "source");
                var body = Condition(
                    OrElse(Equal(sourceParam, Constant(null, typeof(Source))), Constant(false)),
                    Constant(null, typeof(Dest)),
                    MemberInit(New(typeof(Dest).GetTypeInfo().DeclaredConstructors.First()),
                        Bind(typeof(Dest).GetTypeInfo().DeclaredProperties.First(),
                            Property(sourceParam, nameof(Source.Value)))));

                var expr = Lambda<Func<Source, Dest>>(body, sourceParam);

                //var fs = expr.Compile();
                var ff = expr.CompileFast(true);

                var dest = ff(new Source { Value = 42 });
                Assert.AreEqual(42, dest.Value);
            }

            [Test]
            public void Coalesce_should_produce_optimal_opcodes()
            {
                var sourceParam = Parameter(typeof(Source), "source");
                var body = Condition(
                    Equal(sourceParam, Constant(null)),
                    Constant(null, typeof(Dest)),
                    Coalesce(
                        Constant(new Dest { Value = 13 }),
                        MemberInit(New(typeof(Dest).GetTypeInfo().DeclaredConstructors.First()),
                            Bind(typeof(Dest).GetTypeInfo().DeclaredProperties.First(),
                                Property(sourceParam, nameof(Source.Value))))));

                var expr = Lambda<Func<Source, Dest>>(body, sourceParam);
                expr.PrintCSharp();

                var fs = expr.CompileSys();
                fs.PrintIL();
                var dest = fs(new Source { Value = 42 });
                Assert.AreEqual(13, dest.Value);

                var ff = expr.CompileFast(true);
                ff.PrintIL();
                dest = ff(new Source { Value = 42 });
                Assert.AreEqual(13, dest.Value);
            }

            [Test]
            public void Coalesce_should_work_with_throw()
            {
                var srcParam = Parameter(typeof(Source), "src");
                var dstParam = Parameter(typeof(Dest), "dst");

                var expr = Lambda<Func<Source, Dest, Dest>>(
                    Condition(
                        Equal(srcParam, Constant(null)),
                        Constant(null, typeof(Dest)),
                        Coalesce(dstParam, Throw(Constant(new ArgumentNullException("meh!")), typeof(Dest)))
                    ), 
                    srcParam, dstParam);

                expr.PrintCSharp();

                var fs = expr.CompileSys();
                fs.PrintIL();
                var ex = Assert.Throws<ArgumentNullException>(() => 
                    fs(new Source { Value = 42 }, null));
                StringAssert.Contains("meh!", ex.Message);

                var ff = expr.CompileFast(true);
                ff.PrintIL();
                ex = Assert.Throws<ArgumentNullException>(() => 
                    ff(new Source { Value = 42 }, null));
                StringAssert.Contains("meh!", ex.Message);
            }

            [Test]
            public void Test_the_tmp_var_block_reduction()
            {
                var srcParam = Parameter(typeof(Source), "src");
                var dstParam = Parameter(typeof(Dest), "dst");
                var tmpVar = Parameter(typeof(int), "tmp");

                var expr = Lambda<Func<Source, Dest, Dest>>(
                    Condition(
                        Equal(srcParam, Constant(null)),
                        Constant(null, typeof(Dest)),
                        Block(
                            Assign(dstParam, Coalesce(dstParam, New(typeof(Dest)))),
                            Block(new[] { tmpVar },
                                Assign(tmpVar, Property(srcParam, "Value")),
                                Assign(Property(dstParam, "Value"), tmpVar)
                            ),
                            dstParam
                        )
                    ), 
                    srcParam, dstParam);

                expr.PrintCSharp();

                var fs = expr.CompileSys();
                fs.PrintIL();
                var dst = fs(new Source { Value = 42 }, null);
                Assert.AreEqual(42, dst.Value);

                var ff = expr.CompileFast(true);
                ff.PrintIL();
                dst = ff(new Source { Value = 42 }, null);
                Assert.AreEqual(42, dst.Value);
            }

            public class ResolutionContext { }

            public class AutoMapperException : Exception
            {
                public AutoMapperException(string message, Exception innerException) : base(message, innerException) {}
            }

            [Test]
            public void Setting_the_outside_variable()
            {
                var resultVar = Parameter(typeof(int), "i");

                var expression = Lambda<Func<int>>(
                    Block(
                        new[] { resultVar },
                        Block(
                            Assign(resultVar, Constant(77))),
                        resultVar));

                //var fs = expression.CompileSys();
                //Assert.IsNotNull(fs);
                //Assert.AreEqual(77, fs());

                var ff = expression.CompileFast(true);
                Assert.AreEqual(77, ff());
            }

            [Test]
            public void TryCatch_setting_the_outside_variable()
            {
                var resultVar = Parameter(typeof(int), "i");
                var exVar = Parameter(typeof(Exception), "ex");

                var expression = Lambda<Func<int>>(
                    Block(
                        new[] { resultVar },
                        TryCatch(
                            Assign(resultVar, Constant(77)),
                            Catch(exVar,
                                Throw(
                                    New(typeof(AutoMapperException).GetTypeInfo().DeclaredConstructors.First(),
                                        Constant("Blah"),
                                        exVar),
                                    typeof(int)))),
                        resultVar));

                //var fs = expression.CompileSys();
                //Assert.IsNotNull(fs);
                //Assert.AreEqual(77, fs());

                var ff = expression.CompileFast(true);
                Assert.AreEqual(77, ff());
            }

            [Test]
            public void TryCatch_with_void_rethrows_error_in_catch()
            {
                var srcParam = Parameter(typeof(Source), "source");
                var destParam = Parameter(typeof(Dest), "dest");

                var typeMapDestVar = Parameter(typeof(Dest), "d");
                var resolvedValueVar = Parameter(typeof(int), "val");
                var exceptionVar = Parameter(typeof(Exception), "ex");

                var expression = Lambda<Func<Source, Dest, ResolutionContext, Dest>>(
                    Block(
                        Condition(
                            Equal(srcParam, Constant(null)),
                            Default(typeof(Dest)),
                            Block(typeof(Dest), new[] { typeMapDestVar },
                                Assign(
                                    typeMapDestVar,
                                    Coalesce(destParam, New(typeof(Dest).GetTypeInfo().DeclaredConstructors.First()))),
                                TryCatch(
                                    /* Assign src.Value */
                                    Block(typeof(void), new[] { resolvedValueVar },
                                        Block(
                                            Assign(resolvedValueVar,
                                                Condition(Or(Equal(srcParam, Constant(null)), Constant(false)),
                                                    Default(typeof(int)),
                                                    Property(srcParam, "Value"))
                                            ),
                                            Assign(Property(typeMapDestVar, "Value"), resolvedValueVar)
                                        )
                                    ),
                                    Catch(exceptionVar,
                                        Throw(New(typeof(AutoMapperException).GetTypeInfo().DeclaredConstructors.First(),
                                                Constant("Error mapping types."),
                                                exceptionVar))) // should skip this, cause does no make sense after the throw
                                ),
                                typeMapDestVar))
                    ),
                    srcParam, destParam, Parameter(typeof(ResolutionContext), "_")
                );

                var fs = expression.CompileSys();
                var ds = fs(new Source { Value = 42 }, null, new ResolutionContext());
                Assert.AreEqual(42, ds.Value);

                var ff = expression.CompileFast(true);
                var df = ff(new Source { Value = 42 }, null, new ResolutionContext());
                Assert.AreEqual(42, df.Value);

                var fa = expression.TryCompileWithoutClosure<Func<Source, Dest, ResolutionContext, Dest>>();
                var da = ff(new Source { Value = 42 }, null, new ResolutionContext());
                Assert.AreEqual(42, da.Value);
            }

            [Test]
            public void TryCatch_with_rethrow_error_in_catch_and_the_unreachable_code_after_the_throw()
            {
                var srcParam = Parameter(typeof(Source), "source");
                var destParam = Parameter(typeof(Dest), "dest");

                var typeMapDestVar = Parameter(typeof(Dest), "d");
                var resolvedValueVar = Parameter(typeof(int), "val");
                var exceptionVar = Parameter(typeof(Exception), "ex");

                var expression = Lambda<Func<Source, Dest, ResolutionContext, Dest>>(
                    Block(
                        Condition(
                            Equal(srcParam, Constant(null)),
                            Default(typeof(Dest)),
                            Block(typeof(Dest), new[] { typeMapDestVar },
                                Assign(
                                    typeMapDestVar, 
                                    Coalesce(destParam, New(typeof(Dest).GetTypeInfo().DeclaredConstructors.First()))),
                                TryCatch(
                                    /* Assign src.Value */
                                    Block(new[] { resolvedValueVar },
                                        Block(
                                            Assign(resolvedValueVar,
                                                Condition(Or(Equal(srcParam, Constant(null)), Constant(false)),
                                                    Default(typeof(int)),
                                                    Property(srcParam, "Value"))
                                            ),
                                            Assign(Property(typeMapDestVar, "Value"), resolvedValueVar)
                                        )
                                    ),
                                    Catch(exceptionVar, Block(
                                        Throw(New(typeof(AutoMapperException).GetTypeInfo().DeclaredConstructors.First(),
                                                Constant("Error mapping types."),
                                                exceptionVar)),
                                        Default(typeof(int)))) // should skip this, cause does no make sense after the throw
                                ),
                                typeMapDestVar))
                    ),
                    srcParam, destParam, Parameter(typeof(ResolutionContext), "_")
                );

                var fs = expression.CompileSys();
                var ds = fs(new Source { Value = 42 }, null, new ResolutionContext());
                Assert.AreEqual(42, ds.Value);

                var ff = expression.CompileFast(true);
                Assert.IsNotNull(ff);

                var df = ff(new Source { Value = 42 }, null, new ResolutionContext());
                Assert.AreEqual(42, df.Value);
            }

            [Test]
            public void TryCatch_with_non_void_rethrows_error_in_catch()
            {
                var srcParam = Parameter(typeof(Source), "source");
                var destParam = Parameter(typeof(Dest), "dest");

                var typeMapDestVar = Parameter(typeof(Dest), "d");
                var resolvedValueVar = Parameter(typeof(int), "val");
                var exceptionVar = Parameter(typeof(Exception), "ex");

                var expression = Lambda<Func<Source, Dest, ResolutionContext, Dest>>(
                    Block(
                        Condition(
                            Equal(srcParam, Constant(null)),
                            Default(typeof(Dest)),
                            Block(typeof(Dest), new[] { typeMapDestVar },
                                Assign(
                                    typeMapDestVar,
                                    Coalesce(destParam, New(typeof(Dest).GetTypeInfo().DeclaredConstructors.First()))),
                                TryCatch(
                                    /* Assign src.Value */
                                    Block(new[] { resolvedValueVar },
                                        Block(
                                            Assign(resolvedValueVar,
                                                Condition(Or(Equal(srcParam, Constant(null)), Constant(false)),
                                                    Default(typeof(int)),
                                                    Property(srcParam, "Value"))
                                            ),
                                            Assign(Property(typeMapDestVar, "Value"), resolvedValueVar)
                                        )
                                    ),
                                    Catch(exceptionVar,
                                        Throw(
                                            New(typeof(AutoMapperException).GetTypeInfo().DeclaredConstructors.First(),
                                                Constant("Error mapping types."),
                                                exceptionVar),
                                            typeof(int))) // should skip this, cause does no make sense after the throw
                                ),
                                typeMapDestVar))
                    ),
                    srcParam, destParam, Parameter(typeof(ResolutionContext), "_")
                );

                var fs = expression.CompileSys();
                var ds = fs(new Source { Value = 42 }, null, new ResolutionContext());
                Assert.AreEqual(42, ds.Value);

                var ff = expression.CompileFast(true);
                Assert.IsNotNull(ff);

                var df = ff(new Source { Value = 42 }, null, new ResolutionContext());
                Assert.AreEqual(42, df.Value);
            }
        }
    }
}

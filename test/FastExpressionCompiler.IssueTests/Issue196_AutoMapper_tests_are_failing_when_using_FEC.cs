#if !LIGHT_EXPRESSION
using System.Linq;
using System.Reflection;
using System;
using System.Linq.Expressions;
using AutoMapper;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue196_AutoMapper_tests_are_failing_when_using_FEC
    {
        public class FastExpressionCompilerBug
        {
            [Test]
            public void ShouldWork()
            {
                var config = new MapperConfiguration(cfg => cfg.CreateMap<Source, Dest>());
                var mapper = config.CreateMapper();
                var expression = mapper.ConfigurationProvider.ExpressionBuilder.CreateMapExpression(typeof(Source), typeof(Dest));
                Assert.IsNotNull(((LambdaExpression)expression).CompileFast(true));

                var source = new Source { Value = 5 };
                var dest = mapper.Map<Dest>(source);

                Assert.AreEqual(source.Value, dest.Value);
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
                var sourceParam = Parameter(typeof(Source), "source");
                var body = Condition(
                    Equal(sourceParam, Constant(null, typeof(Source))),
                    Constant(null, typeof(Dest)),
                    MemberInit(New(typeof(Dest).GetTypeInfo().DeclaredConstructors.First()),
                        Bind(typeof(Dest).GetTypeInfo().DeclaredProperties.First(),
                            Property(sourceParam, nameof(Source.Value)))));

                var expr = Lambda<Func<Source, Dest>>(body, sourceParam);

                //var fs = expr.Compile();
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

                //var fs = expr.Compile();
                var ff = expr.CompileFast(true);

                var dest = ff(new Source { Value = 42 });
                Assert.AreEqual(13, dest.Value);
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

                var fs = expression.Compile();
                Assert.IsNotNull(fs);
                Assert.AreEqual(77, fs());

                var ff = expression.CompileFast(true);
                Assert.IsNotNull(ff);
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

                var fs = expression.Compile();
                Assert.IsNotNull(fs);
                Assert.AreEqual(77, fs());

                var ff = expression.CompileFast(true);
                Assert.IsNotNull(ff);
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

                var fs = expression.Compile();
                var ds = fs(new Source { Value = 42 }, null, new ResolutionContext());
                Assert.AreEqual(42, ds.Value);

                var ff = expression.CompileFast(true);
                Assert.IsNotNull(ff);

                var df = ff(new Source { Value = 42 }, null, new ResolutionContext());
                Assert.AreEqual(42, df.Value);
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

                var fs = expression.Compile();
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

                var fs = expression.Compile();
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
#endif

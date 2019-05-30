

using static System.Linq.Expressions.Expression;
#if !LIGHT_EXPRESSION
using System.Linq;
using System.Reflection;
using System;
using System.Linq.Expressions;
using AutoMapper;
using NUnit.Framework;

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

                var fs = expr.Compile();
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
        }
    }
}
#endif
using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class ToCSharpStringTests : ITest
    {
        public int Run()
        {
            Outputs_closed_generic_type_constant_correctly();
            return 1;
        }

        [Test]
        public void Outputs_closed_generic_type_constant_correctly()
        {
            var e = Lambda<Func<Type>>(Constant(typeof(A<string>)));

            var cs = e.ToCSharpString();

            StringAssert.Contains("A<string>", cs);

            var f = e.CompileFast(true);
            Assert.AreEqual(typeof(A<string>), f());
        }

        [Test]
        public void Lambda_with_block_body()
        {
            var variable = Parameter(typeof(int), "variable");
            var e = Lambda<Func<int>>(Block(new [] { variable }, Assign(variable,  Constant(1)), Add(variable, Constant(2))));

            var cs = e.ToCSharpString();

            Assert.AreEqual("""
                (Func<int>)(() =>
                {
                    int variable = default;
                    variable = 1;
                    return (variable + 2);
                });
                """, cs);
        }

        [Test]
        public void Nested_blocks()
        {
            var v1 = Parameter(typeof(int), "v1");
            var v2 = Parameter(typeof(int), "v2");
            var cs = Block(new [] { v1 },
                Assign(v1, Constant(2)),
                Block(new [] { v2 },
                    Assign(v2, Constant(3)),
                    AddAssign(v1, v2),
                    IfThen(
                        Equal(v1, Constant(5)),
                        Block(
                            Assign(v2, Constant(7)),
                            AddAssign(v1, v2)
                        )
                    )
                ),
                v1
            ).ToCSharpString();
            Console.WriteLine(cs);
            Assert.AreEqual("""
                    {
                        int v1 = default;
                        v1 = 2;
                        int v2 = default;
                        v2 = 3;
                        v1 += v2;
                        if (v1 == 5)
                        {
                            v2 = 7;
                            v1 += v2;
                        }
                        v1;
                    };
                    """, cs);
        }


        [Test]
        public void Somehow_handles_block_in_expression()
        {
            // it's probably not possible to output compilable C# for expressions like this, but at least it can be easy to read
            var variable = Parameter(typeof(int), "variable");
            var cs = Add(Constant(1), Block(new [] { variable },
                Assign(variable, Constant(2)),
                variable
            )).ToCSharpString();
            Assert.AreEqual("""
                    (1 + { /* BlockExpression cannot be written in C#. Please rewrite the code inside these braces as a C# expression, or reorganize the parent expression as a block. */
                        int variable = default;
                        variable = 2;
                        variable;/* <- block result */
                    });
                    """, cs);
        }


        class A<X> {}
    }
}

using System;
using NUnit.Framework;
#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue197_Operation_could_destabilize_the_runtime  : ITest
    {
        public int Run()
        {
            Test();
            Test2();
            return 2;
        }

        [Test]
        public void Test()
        {
            /*
            src =>
            {
                var dest = new FastExpressionCompilerBug.DefaultEnumValueToString.Destination();
                var resolvedValue = src.Color;
                var propertyValue = resolvedValue.ToString();
                dest.Color = propertyValue;

                return dest;
            }
            */

            var srcParam = Parameter(typeof(Source), "src");
            var destParam = Parameter(typeof(Destination), "dest");
            var resolvedValueParam = Parameter(typeof(ConsoleColor), "resolvedValue");
            var propertyValueParam = Parameter(typeof(string), "propertyValue");

            var expression = Lambda<Func<Source, Destination>>(
                Block(typeof(Destination), new[] { destParam, resolvedValueParam, propertyValueParam },
                    Assign(destParam, New(typeof(Destination).GetConstructors()[0])),
                    Assign(resolvedValueParam, Property(srcParam, "Color")),
                    Assign(propertyValueParam, Call(resolvedValueParam, "ToString", new Type[0])),
                    Assign(Property(destParam, "Color"), propertyValueParam),
                    destParam
                ),
                srcParam
            );

            var src = new Source();

            //var funcSys = expression.Compile();
            //var dest1 = funcSys(src);

            var func = expression.CompileFast(true);
            var dest = func(src);
            Assert.AreEqual("Black", dest.Color);
        }

        [Test]
        public void Test2()
        {
            /*
            src =>
            {
                var dest = new FastExpressionCompilerBug.DefaultEnumValueToString.Destination();
                var propertyValue = src.Color.ToString();
                dest.Color = propertyValue;

                return dest;
            }
            */

            var srcParam = Parameter(typeof(Source), "src");
            var destParam = Parameter(typeof(Destination), "dest");
            var propertyValueParam = Parameter(typeof(string), "propertyValue");


            var expression = Lambda<Func<Source, Destination>>(
                Block(typeof(Destination), new[] { destParam, propertyValueParam },
                    Assign(destParam, New(typeof(Destination).GetConstructors()[0])),
                    Assign(propertyValueParam, Call(Property(srcParam, "Color"), "ToString", new Type[0])),
                    Assign(Property(destParam, "Color"), propertyValueParam),
                    destParam
                ),
                srcParam
            );

            var src = new Source();

            var func = expression.CompileFast(true);
            var dest = func(src);
            Assert.AreEqual("Black", dest.Color);
        }

        class Source
        {
            public ConsoleColor Color { get; set; }
        }

        class Destination
        {
            public string Color { get; set; }
        }
    }
}

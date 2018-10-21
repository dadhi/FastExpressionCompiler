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
    public class Issue127_Switch_is_supported
    {
        [Test]
        public void SwitchIsSupported1()
        {
            var eVar = Parameter(typeof(int));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(1)),
                    SwitchCase(Constant("B"), Constant(2)),
                    SwitchCase(Constant("C"), Constant(5))  //Difference of 3 creates empty branches, more creates Conditions
                );

            var lambda = Lambda<Func<int, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual("B", fastCompiled(2));
        }

        public enum MyEnum
        {
           a,b,c 
        }

        [Test]
        public void SwitchIsSupportedEnum()
        {
            var eVar = Parameter(typeof(MyEnum?));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(MyEnum.a, typeof(MyEnum?))),
                    SwitchCase(Constant("B"), Constant(MyEnum.b, typeof(MyEnum?))),
                    SwitchCase(Constant("C"), Constant(MyEnum.c, typeof(MyEnum?)))
                );

            var lambda = Lambda<Func<MyEnum?, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual("B", fastCompiled(MyEnum.b));
        }

        [Test]
        public void SwitchIsSupportedEnum2()
        {
            var eVar = Parameter(typeof(MyEnum));
            var blockExpr =
                Switch(eVar,
                    Constant(null, typeof(long?)),
                    SwitchCase(Constant(1L, typeof(long?)), Constant(MyEnum.a)),
                    SwitchCase(Constant(2L, typeof(long?)), Constant(MyEnum.b))
                );

            var lambda = Lambda<Func<MyEnum, long?>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(2L, fastCompiled(MyEnum.b));
            Assert.AreEqual(null, fastCompiled(MyEnum.c));
        }

        [Test]
        public void SwitchIsSupported11()
        {
            var eVar = Parameter(typeof(int));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(1)),
                    SwitchCase(Constant("B"), Constant(2)),
                    SwitchCase(Constant("C"), Constant(7)),  //Difference of 3 creates empty branches, more creates Conditions
                    SwitchCase(Constant("C"), Constant(8))  //Difference of 3 creates empty branches, more creates Conditions
                );

            var lambda = Lambda<Func<int, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual("B", fastCompiled(2));
        }

        [Test]
        public void SwitchIsSupported12()
        {
            var eVar = Parameter(typeof(int));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(1)),
                    SwitchCase(Constant("B"), Constant(2)),
                    SwitchCase(Constant("C"), Constant(7))  //Difference of 3 creates empty branches, more creates Conditions
                );

            var lambda = Lambda<Func<int, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual("B", fastCompiled(2));
        }


        [Test]
        public void SwitchIsSupported2()
        {
            var eVar = Parameter(typeof(long));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(1L)),
                    SwitchCase(Constant("B"), Constant(2L)),
                    SwitchCase(Constant("C"), Constant(3L))
                );

            var lambda = Lambda<Func<long, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual("B", fastCompiled(2));
        }

        [Test]
        public void SwitchIsSupported30()
        {
            var eVar = Parameter(typeof(int?));
            var blockExpr = Equal(eVar, Constant(94, typeof(int?)));

            var lambda = Lambda<Func<int?, bool>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(true, fastCompiled(94));
        }

        [Test]
        public void SwitchIsSupported33()
        {
            var blockExpr = Convert(Constant(94), typeof(int?));

            var lambda = Lambda<Func<int?>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(94, fastCompiled());
        }

        [Test]
        public void SwitchIsSupported3()
        {
            var eVar = Parameter(typeof(int?));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant((int?)94, typeof(int?))),
                    SwitchCase(Constant("B"), Constant((int?)96, typeof(int?))),
                    SwitchCase(Constant("C"), Constant((int?)98, typeof(int?)))
                );

            var lambda = Lambda<Func<int?, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual("B", fastCompiled(96));
        }

        [Test]
        public void SwitchIsSupported31()
        {
            var eVar = Parameter(typeof(int));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(1))
                );

            var lambda = Lambda<Func<int, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual("A", fastCompiled(1));
        }

        [Test]
        public void SwitchIsSupported4()
        {
            var eVar = Parameter(typeof(bool));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(true)),
                    SwitchCase(Constant("B"), Constant(false))
                );

            var lambda = Lambda<Func<bool, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual("B", fastCompiled(false));
        }

        [Test]
        public void SwitchIsSupported5()
        {
            var eVar = Parameter(typeof(string));
            var blockExpr =
                Switch(eVar,
                    Constant("C"),
                    SwitchCase(Constant("A"), Constant("A")),
                    SwitchCase(Constant("B"), Constant("B"))
                );

            var lambda = Lambda<Func<string, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual("A", fastCompiled("A"));
            Assert.AreEqual("C", fastCompiled("Z"));
        }

        class Helper
        {
            public string V { get; set; }
        }

        [Test]
        public void SwitchIsSupported6()
        {
            var eVar = Parameter(typeof(Helper));
            var blockExpr =
                Switch(Property(eVar, "V"),
                    Constant("C"),
                    SwitchCase(Constant("A"), Constant("A")),
                    SwitchCase(Constant("B"), Constant("B")),
                    SwitchCase(Constant("C"), Constant("C")),
                    SwitchCase(Constant("D"), Constant("D")),
                    SwitchCase(Constant("E"), Constant("E")),
                    SwitchCase(Constant("F"), Constant("F")),
                    SwitchCase(Constant("G"), Constant("G")),
                    SwitchCase(Constant("H"), Constant("H")),
                    SwitchCase(Constant("I"), Constant("I"))
                );

            var lambda = Lambda<Func<Helper, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual("A", fastCompiled(new Helper() { V = "A" }));
            Assert.AreEqual("C", fastCompiled(new Helper() { V = "Z" }));
        }
    }
}

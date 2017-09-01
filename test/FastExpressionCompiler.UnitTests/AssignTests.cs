using System;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class AssignTests
    {
        [Test]
        public void Basic_test()
        {
            var sParamExpr = Parameter(typeof(string), "s");
            var expr = Lambda<Func<string, string>>(
                Assign(sParamExpr, Constant("aaa")),
                sParamExpr);

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual("aaa", f("ignored"));
        }

        [Test]
        public void Member_test_prop()
        {
            var a = new Test();
            var expr = Lambda<Func<int>>(
                Assign(Property(Constant(a), "Prop"), 
                Constant(5)));

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual(5, f());
            Assert.AreEqual(5, a.Prop);
        }

        [Test]
        public void Member_test_field()
        {
            var a = new Test();
            var expr = Lambda<Func<int>>(
                Assign(Field(Constant(a), "Field"),
                Constant(5)));

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual(5, f());
            Assert.AreEqual(5, a.Field);
        }

        public class Test
        {
            public int Prop { get; set; }
            public int Field;
        }
    }
}

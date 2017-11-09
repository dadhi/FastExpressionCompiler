using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class ValueTypeTests
    {
        [Test]
        public void Should_support_struct_params()
        {
            Expression<Func<A, int>> expr = a => a.N;

            var getN = expr.CompileFast(true);

            Assert.AreEqual(42, getN(new A { N = 42 }));
        }

        [Test]
        public void Should_support_struct_methods_requiring_boxing()
        {
            Expression<Func<A, string>> expr = a => a.ToString();

            var getN = expr.CompileFast(true);

            Assert.AreEqual("42", getN(new A { N = 42 }));
        }

        [Test]
        public void Can_create_struct()
        {
            Expression<Func<A>> expr = () => new A();

            var newA = expr.CompileFast<Func<A>>(true);

            Assert.AreEqual(0, newA().N);
        }

        [Test]
        public void Can_init_struct_member()
        {
            Expression<Func<A>> expr = () => new A { N = 43, M = 34, Sf = "sf", Sp = "sp" };

            var newA = expr.CompileFast<Func<A>>(true);

            var a = newA();
            Assert.AreEqual(43, a.N);
            Assert.AreEqual(34, a.M);
            Assert.AreEqual("sf", a.Sf);
            Assert.AreEqual("sp", a.Sp);
        }

        [Test]
        public void Can_get_struct_member()
        {
            Expression<Func<int>> exprN = () => new A { N = 43, M = 34, Sf = "sf", Sp = "sp" }.N;
            Expression<Func<int>> exprM = () => new A { N = 43, M = 34, Sf = "sf", Sp = "sp" }.M;
            Expression<Func<string>> exprSf = () => new A { N = 43, M = 34, Sf = "sf", Sp = "sp" }.Sf;
            Expression<Func<string>> exprSp = () => new A { N = 43, M = 34, Sf = "sf", Sp = "sp" }.Sp;


            var n = exprN.CompileFast<Func<int>>(true);
            var m = exprM.CompileFast<Func<int>>(true);
            var sf = exprSf.CompileFast<Func<string>>(true);
            var sp = exprSp.CompileFast<Func<string>>(true);

            Assert.AreEqual(43, n());
            Assert.AreEqual(34, m());
            Assert.AreEqual("sf", sf());
            Assert.AreEqual("sp", sp());
        }

        struct A
        {
            public int N;
            public int M { get; set; }
            public string Sf;
            public string Sp { get; set; }

            public override string ToString() => N.ToString();
        }

        [Test]
        public void Action_using_with_struct_closure_field()
        {
            var s = new SS();
            Expression<Action<string>> expr = a => s.SetValue(a);

            var lambda = expr.CompileFast(ifFastFailedReturnNull: true);

            lambda("a");
            Assert.IsNull(s.Value);
        }

        public struct SS
        {
            public string Value;

            public void SetValue(string s)
            {
                Value = s;
            }
        }
    }
}

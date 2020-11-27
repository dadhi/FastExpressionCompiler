using System;
using System.Drawing;
using System.Linq.Expressions;
using NUnit.Framework;
#pragma warning disable 219

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue67_Equality_comparison_with_nullables_throws_at_delegate_invoke : ITest
    {
        public int Run()
        {
            Comparing_nullable_equal_works();
            Comparing_struct_equal_works();
            Comparing_int_equal_works();
            Comparing_nullable_equal_point_works();
            Comparing_nullable_unequal_works();
            Comparing_nullable_greater_works();
            Comparing_nullable_greaterOrEqual_works();
            Comparing_nullable_less_works();
            Comparing_nullable_lessOrEqual_works();
            Compare_nullable_to_null_should_work();
            return 10;
        }

        class Foo
        {
            public int? Prop { get; set; }

            public Point? PropP { get; set; }

            public int Prop3 { get; set; }

            public aa Prop4 { get; set; }
        }

        public struct aa
        {
            public int b;

            public override bool Equals(Object obj)
            {
                return obj is aa && this == (aa)obj;
            }
            public override int GetHashCode()
            {
                return b.GetHashCode();
            }
            public static bool operator ==(aa x, aa y)
            {
                return x.b == y.b;
            }
            public static bool operator !=(aa x, aa y)
            {
                return !(x == y);
            }
        }

        [Test]
        public void Comparing_nullable_equal_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop == int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Assert.IsNotNull(f);

            Assert.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Assert.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Assert.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Assert.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));
        }

        [Test]
        public void Comparing_struct_equal_works()
        {
            var aaComparand = new aa();
            Expression<Func<Foo, bool>> e = foo => foo.Prop4 == aaComparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Assert.IsNotNull(f);

            Assert.AreEqual(f2(new Foo() { Prop4 = aaComparand }), f(new Foo() { Prop4 = aaComparand }));
        }

        [Test]
        public void Comparing_int_equal_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop3 == int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Assert.IsNotNull(f);

            Assert.AreEqual(f2(new Foo() { Prop3 = 1 }), f(new Foo() { Prop3 = 1 }));
            Assert.AreEqual(f2(new Foo() { Prop3 = -1 }), f(new Foo() { Prop3 = -1 }));
            Assert.AreEqual(f2(new Foo() { Prop3 = 0 }), f(new Foo() { Prop3 = 0 }));
        }

        [Test]
        public void Comparing_nullable_equal_point_works()
        {
            var pComparand = new Point(4, 6);
            Expression<Func<Foo, bool>> e = foo => foo.PropP == pComparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Assert.IsNotNull(f);

            Assert.AreEqual(f2(new Foo() { PropP = new Point(4, 6) }), f(new Foo() { PropP = new Point(4, 6) }));
            Assert.AreEqual(f2(new Foo() { PropP = null }), f(new Foo() { PropP = null }));
            Assert.AreEqual(f2(new Foo() { PropP = new Point(4, 7) }), f(new Foo() { PropP = new Point(4, 7) }));
        }

        [Test]
        public void Comparing_nullable_unequal_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop != int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Assert.IsNotNull(f);

            Assert.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Assert.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Assert.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Assert.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));

            int? int32Comparand2 = null;
            Expression<Func<Foo, bool>> e2 = foo => foo.Prop != int32Comparand;

            var fa = e2.CompileFast(true);
            var fa2 = e2.Compile();
            Assert.IsNotNull(fa);

            Assert.AreEqual(fa2(new Foo() { Prop = 1 }), fa(new Foo() { Prop = 1 }));
            Assert.AreEqual(fa2(new Foo() { Prop = null }), fa(new Foo() { Prop = null }));
            Assert.AreEqual(fa2(new Foo() { Prop = -1 }), fa(new Foo() { Prop = -1 }));
            Assert.AreEqual(fa2(new Foo() { Prop = 0 }), fa(new Foo() { Prop = 0 }));
        }

        [Test]
        public void Comparing_nullable_greater_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop > int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Assert.IsNotNull(f);

            Assert.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Assert.AreEqual(f2(new Foo() { Prop = 2 }), f(new Foo() { Prop = 2 }));
            Assert.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Assert.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Assert.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));

            int? int32Comparand2 = null;
            Expression<Func<Foo, bool>> e2 = foo => foo.Prop > int32Comparand;

            var fa = e2.CompileFast(true);
            var fa2 = e2.Compile();
            Assert.IsNotNull(fa);

            Assert.AreEqual(fa2(new Foo() { Prop = 1 }), fa(new Foo() { Prop = 1 }));
            Assert.AreEqual(fa2(new Foo() { Prop = 2 }), fa(new Foo() { Prop = 2 }));
            Assert.AreEqual(fa2(new Foo() { Prop = null }), fa(new Foo() { Prop = null }));
            Assert.AreEqual(fa2(new Foo() { Prop = -1 }), fa(new Foo() { Prop = -1 }));
            Assert.AreEqual(fa2(new Foo() { Prop = 0 }), fa(new Foo() { Prop = 0 }));
        }

        [Test]
        public void Comparing_nullable_greaterOrEqual_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop >= int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Assert.IsNotNull(f);

            Assert.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Assert.AreEqual(f2(new Foo() { Prop = 2 }), f(new Foo() { Prop = 2 }));
            Assert.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Assert.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Assert.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));

            int? int32Comparand2 = null;
            Expression<Func<Foo, bool>> e2 = foo => foo.Prop >= int32Comparand;

            var fa = e2.CompileFast(true);
            var fa2 = e2.Compile();
            Assert.IsNotNull(fa);

            Assert.AreEqual(fa2(new Foo() { Prop = 1 }), fa(new Foo() { Prop = 1 }));
            Assert.AreEqual(fa2(new Foo() { Prop = 2 }), fa(new Foo() { Prop = 2 }));
            Assert.AreEqual(fa2(new Foo() { Prop = null }), fa(new Foo() { Prop = null }));
            Assert.AreEqual(fa2(new Foo() { Prop = -1 }), fa(new Foo() { Prop = -1 }));
            Assert.AreEqual(fa2(new Foo() { Prop = 0 }), fa(new Foo() { Prop = 0 }));
        }

        [Test]
        public void Comparing_nullable_less_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop < int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Assert.IsNotNull(f);

            Assert.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Assert.AreEqual(f2(new Foo() { Prop = 2 }), f(new Foo() { Prop = 2 }));
            Assert.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Assert.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Assert.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));

            int? int32Comparand2 = null;
            Expression<Func<Foo, bool>> e2 = foo => foo.Prop < int32Comparand;

            var fa = e2.CompileFast(true);
            var fa2 = e2.Compile();
            Assert.IsNotNull(fa);

            Assert.AreEqual(fa2(new Foo() { Prop = 1 }), fa(new Foo() { Prop = 1 }));
            Assert.AreEqual(fa2(new Foo() { Prop = 2 }), fa(new Foo() { Prop = 2 }));
            Assert.AreEqual(fa2(new Foo() { Prop = null }), fa(new Foo() { Prop = null }));
            Assert.AreEqual(fa2(new Foo() { Prop = -1 }), fa(new Foo() { Prop = -1 }));
            Assert.AreEqual(fa2(new Foo() { Prop = 0 }), fa(new Foo() { Prop = 0 }));
        }

        [Test]
        public void Comparing_nullable_lessOrEqual_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop <= int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Assert.IsNotNull(f);

            Assert.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Assert.AreEqual(f2(new Foo() { Prop = 2 }), f(new Foo() { Prop = 2 }));
            Assert.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Assert.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Assert.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));

            int? int32Comparand2 = null;
            Expression<Func<Foo, bool>> e2 = foo => foo.Prop <= int32Comparand;

            var fa = e2.CompileFast(true);
            var fa2 = e2.Compile();
            Assert.IsNotNull(fa);

            Assert.AreEqual(fa2(new Foo() { Prop = 1 }), fa(new Foo() { Prop = 1 }));
            Assert.AreEqual(fa2(new Foo() { Prop = 2 }), fa(new Foo() { Prop = 2 }));
            Assert.AreEqual(fa2(new Foo() { Prop = null }), fa(new Foo() { Prop = null }));
            Assert.AreEqual(fa2(new Foo() { Prop = -1 }), fa(new Foo() { Prop = -1 }));
            Assert.AreEqual(fa2(new Foo() { Prop = 0 }), fa(new Foo() { Prop = 0 }));
        }

        [Test]
        public void Compare_nullable_to_null_should_work()
        {
            Expression<Func<Foo, bool>> e = foo => Nullable.Equals(foo.Prop, null);

            var f = e.CompileFast(true);
            Assert.IsNotNull(f);

            Assert.IsTrue(f(new Foo { Prop = null }));
            Assert.IsFalse(f(new Foo { Prop = 3 }));
        }
    }
}

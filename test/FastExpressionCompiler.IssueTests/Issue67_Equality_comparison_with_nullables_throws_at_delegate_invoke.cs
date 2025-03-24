using System;
using System.Drawing;
using System.Linq.Expressions;

#pragma warning disable 219

namespace FastExpressionCompiler.IssueTests
{

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

            public Aa Prop4 { get; set; }
        }

        public struct Aa
        {
            public int b;

            public override bool Equals(Object obj)
            {
                return obj is Aa && this == (Aa)obj;
            }
            public override int GetHashCode()
            {
                return b.GetHashCode();
            }
            public static bool operator ==(Aa x, Aa y)
            {
                return x.b == y.b;
            }
            public static bool operator !=(Aa x, Aa y)
            {
                return !(x == y);
            }
        }


        public void Comparing_nullable_equal_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop == int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Asserts.IsNotNull(f);

            Asserts.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Asserts.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));
        }


        public void Comparing_struct_equal_works()
        {
            var aaComparand = new Aa();
            Expression<Func<Foo, bool>> e = foo => foo.Prop4 == aaComparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Asserts.IsNotNull(f);

            Asserts.AreEqual(f2(new Foo() { Prop4 = aaComparand }), f(new Foo() { Prop4 = aaComparand }));
        }


        public void Comparing_int_equal_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop3 == int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Asserts.IsNotNull(f);

            Asserts.AreEqual(f2(new Foo() { Prop3 = 1 }), f(new Foo() { Prop3 = 1 }));
            Asserts.AreEqual(f2(new Foo() { Prop3 = -1 }), f(new Foo() { Prop3 = -1 }));
            Asserts.AreEqual(f2(new Foo() { Prop3 = 0 }), f(new Foo() { Prop3 = 0 }));
        }


        public void Comparing_nullable_equal_point_works()
        {
            var pComparand = new Point(4, 6);
            Expression<Func<Foo, bool>> e = foo => foo.PropP == pComparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Asserts.IsNotNull(f);

            Asserts.AreEqual(f2(new Foo() { PropP = new Point(4, 6) }), f(new Foo() { PropP = new Point(4, 6) }));
            Asserts.AreEqual(f2(new Foo() { PropP = null }), f(new Foo() { PropP = null }));
            Asserts.AreEqual(f2(new Foo() { PropP = new Point(4, 7) }), f(new Foo() { PropP = new Point(4, 7) }));
        }


        public void Comparing_nullable_unequal_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop != int32Comparand;

            var f2 = e.Compile();
            var f = e.CompileFast(true);

            Asserts.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Asserts.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));

            int? int32Comparand2 = null;
            Expression<Func<Foo, bool>> e2 = foo => foo.Prop != int32Comparand;

            var fa2 = e2.Compile();
            var fa = e2.CompileFast(true);

            Asserts.AreEqual(fa2(new Foo() { Prop = 1 }), fa(new Foo() { Prop = 1 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = null }), fa(new Foo() { Prop = null }));
            Asserts.AreEqual(fa2(new Foo() { Prop = -1 }), fa(new Foo() { Prop = -1 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = 0 }), fa(new Foo() { Prop = 0 }));
        }


        public void Comparing_nullable_greater_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop > int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Asserts.IsNotNull(f);

            Asserts.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = 2 }), f(new Foo() { Prop = 2 }));
            Asserts.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Asserts.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));

            int? int32Comparand2 = null;
            Expression<Func<Foo, bool>> e2 = foo => foo.Prop > int32Comparand;

            var fa = e2.CompileFast(true);
            var fa2 = e2.Compile();
            Asserts.IsNotNull(fa);

            Asserts.AreEqual(fa2(new Foo() { Prop = 1 }), fa(new Foo() { Prop = 1 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = 2 }), fa(new Foo() { Prop = 2 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = null }), fa(new Foo() { Prop = null }));
            Asserts.AreEqual(fa2(new Foo() { Prop = -1 }), fa(new Foo() { Prop = -1 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = 0 }), fa(new Foo() { Prop = 0 }));
        }


        public void Comparing_nullable_greaterOrEqual_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop >= int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Asserts.IsNotNull(f);

            Asserts.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = 2 }), f(new Foo() { Prop = 2 }));
            Asserts.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Asserts.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));

            int? int32Comparand2 = null;
            Expression<Func<Foo, bool>> e2 = foo => foo.Prop >= int32Comparand;

            var fa = e2.CompileFast(true);
            var fa2 = e2.Compile();
            Asserts.IsNotNull(fa);

            Asserts.AreEqual(fa2(new Foo() { Prop = 1 }), fa(new Foo() { Prop = 1 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = 2 }), fa(new Foo() { Prop = 2 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = null }), fa(new Foo() { Prop = null }));
            Asserts.AreEqual(fa2(new Foo() { Prop = -1 }), fa(new Foo() { Prop = -1 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = 0 }), fa(new Foo() { Prop = 0 }));
        }


        public void Comparing_nullable_less_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop < int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Asserts.IsNotNull(f);

            Asserts.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = 2 }), f(new Foo() { Prop = 2 }));
            Asserts.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Asserts.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));

            int? int32Comparand2 = null;
            Expression<Func<Foo, bool>> e2 = foo => foo.Prop < int32Comparand;

            var fa = e2.CompileFast(true);
            var fa2 = e2.Compile();
            Asserts.IsNotNull(fa);

            Asserts.AreEqual(fa2(new Foo() { Prop = 1 }), fa(new Foo() { Prop = 1 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = 2 }), fa(new Foo() { Prop = 2 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = null }), fa(new Foo() { Prop = null }));
            Asserts.AreEqual(fa2(new Foo() { Prop = -1 }), fa(new Foo() { Prop = -1 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = 0 }), fa(new Foo() { Prop = 0 }));
        }


        public void Comparing_nullable_lessOrEqual_works()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop <= int32Comparand;

            var f = e.CompileFast(true);
            var f2 = e.Compile();
            Asserts.IsNotNull(f);

            Asserts.AreEqual(f2(new Foo() { Prop = 1 }), f(new Foo() { Prop = 1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = 2 }), f(new Foo() { Prop = 2 }));
            Asserts.AreEqual(f2(new Foo() { Prop = null }), f(new Foo() { Prop = null }));
            Asserts.AreEqual(f2(new Foo() { Prop = -1 }), f(new Foo() { Prop = -1 }));
            Asserts.AreEqual(f2(new Foo() { Prop = 0 }), f(new Foo() { Prop = 0 }));

            int? int32Comparand2 = null;
            Expression<Func<Foo, bool>> e2 = foo => foo.Prop <= int32Comparand;

            var fa = e2.CompileFast(true);
            var fa2 = e2.Compile();
            Asserts.IsNotNull(fa);

            Asserts.AreEqual(fa2(new Foo() { Prop = 1 }), fa(new Foo() { Prop = 1 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = 2 }), fa(new Foo() { Prop = 2 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = null }), fa(new Foo() { Prop = null }));
            Asserts.AreEqual(fa2(new Foo() { Prop = -1 }), fa(new Foo() { Prop = -1 }));
            Asserts.AreEqual(fa2(new Foo() { Prop = 0 }), fa(new Foo() { Prop = 0 }));
        }


        public void Compare_nullable_to_null_should_work()
        {
            Expression<Func<Foo, bool>> e = foo => Nullable.Equals(foo.Prop, null);

            var f = e.CompileFast(true);
            Asserts.IsNotNull(f);

            Asserts.IsTrue(f(new Foo { Prop = null }));
            Asserts.IsFalse(f(new Foo { Prop = 3 }));
        }
    }
}

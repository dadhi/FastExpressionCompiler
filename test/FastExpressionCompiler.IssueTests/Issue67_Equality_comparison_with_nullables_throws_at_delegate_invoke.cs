using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue67_Equality_comparison_with_nullables_throws_at_delegate_invoke
    {
        class Foo
        {
            public int? Prop { get; set; }
        }

        [Test]
        public void Comparing_nullable_is_not_supported_yet_But_should_not_fail()
        {
            var int32Comparand = 1;
            Expression<Func<Foo, bool>> e = foo => foo.Prop == int32Comparand;

            var f = e.CompileFast(true);
            Assert.IsNull(f);

            //var f = new Foo { Prop = null };
            //Assert.IsFalse(f.Prop.GetValueOrDefault() == int32Comparand && f.Prop.HasValue);
            //Assert.IsFalse(Equals(f.Prop, int32Comparand));

            //var ar1 = ac(new Foo { Prop = null });  // throws NullReferenceException
            //var ar2 = ac(new Foo { Prop = 1 });     // throws NullReferenceException
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

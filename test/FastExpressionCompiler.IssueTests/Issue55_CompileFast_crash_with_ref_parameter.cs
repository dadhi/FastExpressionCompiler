using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue55_CompileFast_crash_with_ref_parameter
    {
        struct Bla { public int Hmm; }
        public delegate void Setter<T>(ref T obj, int value);

        [Test]
        public void Should_not_crash()
        {
            var objRef = Expression.Parameter(typeof(Bla).MakeByRefType());
            var objVal = Expression.Parameter(typeof(int));
            var prop = Expression.PropertyOrField(objRef, nameof(Bla.Hmm));
            var body = Expression.Assign(prop, objVal);
            var lambda = Expression.Lambda<Setter<Bla>>(body, objRef, objVal);

            var compiledA = lambda.Compile();

            var example = default(Bla);
            compiledA(ref example, 7);
            Assert.AreEqual(7, example.Hmm);

            var compiledB = lambda.CompileFast<Setter<Bla>>(true);
            Assert.IsNull(compiledB);
        }
    }
}

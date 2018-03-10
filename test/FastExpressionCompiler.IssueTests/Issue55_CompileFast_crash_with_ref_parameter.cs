using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue55_CompileFast_crash_with_ref_parameter
    {
        public delegate void Setter(ref int obj);

        [Test]
        public void RefDoNothingShouldNoCrash()
        {
            var objRef = Expression.Parameter(typeof(int).MakeByRefType());
            var body = Expression.Empty();
            var lambda = Expression.Lambda<Setter>(body, objRef);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(0, exampleA);

            var compiledB = lambda.CompileFast<Setter>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(0, exampleB);
        }

        [Test]
        [Ignore("Fails, but should fix")]
        public void RefFromConstant()
        {
            var objRef = Expression.Parameter(typeof(int).MakeByRefType());
            var objVal = Expression.Constant(7);
            var body = Expression.Assign(objRef, objVal);
            var lambda = Expression.Lambda<Setter>(body, objRef);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(7, exampleA);

            var compiledB = lambda.CompileFast<Setter>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(7, exampleB);
        }

        public delegate void Setter2(ref int obj, int val);

        [Test]
        [Ignore("Fails, but should fix")]
        public void RefSetFromParameter()
        {
            var objRef = Expression.Parameter(typeof(int).MakeByRefType());
            var objVal = Expression.Parameter(typeof(int));
            var body = Expression.Assign(objRef, objVal);
            var lambda = Expression.Lambda<Setter2>(body, objRef, objVal);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA, 7);
            Assert.AreEqual(7, exampleA);

            var compiledB = lambda.CompileFast<Setter2>(true);
            var exampleB = default(int);
            compiledB(ref exampleB, 7);
            Assert.AreEqual(7, exampleB);
        }

        delegate void NonGenericFieldSetter(ref Bla obj, int value);
        
        [Test]
        public void NonGenericSetterFieldShould_not_crash()
        {
            var objRef = Expression.Parameter(typeof(Bla).MakeByRefType());
            var objVal = Expression.Parameter(typeof(int));
            var prop = Expression.PropertyOrField(objRef, nameof(Bla.Hmm));
            var body = Expression.Assign(prop, objVal);
            var lambda = Expression.Lambda<NonGenericFieldSetter>(body, objRef, objVal);

            var compiledA = lambda.Compile();
            var exampleA = default(Bla);
            compiledA(ref exampleA, 7);
            Assert.AreEqual(7, exampleA.Hmm);

            var compiledB = lambda.CompileFast<NonGenericFieldSetter>(true);
            Assert.IsNull(compiledB);
        }

        struct Bla { public int Hmm; }
        public delegate void GenericSetter<T>(ref T obj, int value);

        [Test]
        public void GenericRefStuctFieldShould_not_crash()
        {
            var objRef = Expression.Parameter(typeof(Bla).MakeByRefType());
            var objVal = Expression.Parameter(typeof(int));
            var prop = Expression.PropertyOrField(objRef, nameof(Bla.Hmm));
            var body = Expression.Assign(prop, objVal);
            var lambda = Expression.Lambda<GenericSetter<Bla>>(body, objRef, objVal);

            var compiledA = lambda.Compile();
            var exampleA = default(Bla);
            compiledA(ref exampleA, 7);
            Assert.AreEqual(7, exampleA.Hmm);

            var compiledB = lambda.CompileFast<GenericSetter<Bla>>(true);
            Assert.IsNull(compiledB);
        }
    }
}

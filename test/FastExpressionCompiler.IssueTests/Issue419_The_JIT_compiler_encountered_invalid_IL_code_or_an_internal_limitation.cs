#if !LIGHT_EXPRESSION

using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests;

[TestFixture]
public class Issue419_The_JIT_compiler_encountered_invalid_IL_code_or_an_internal_limitation : ITest
{
    public int Run()
    {
        Original_Case_1();
        Case_1_Simplified_less();
        Case_1_simplified();
        Case_1_Simplified_less_reversed_mul_args();
        return 4;
    }

    public class Obj
    {
        public double? A { get; }
        public Nested Nested { get; }
        public Obj(double? a, Nested nested) => (A, Nested) = (a, nested);
    }

    public class Nested
    {
        public double? B { get; }
        public Nested(double? b) => B = b;
    }

    [Test]
    public void Original_Case_1()
    {
        Expression<Func<Obj, bool>> e = d =>
            (d == null ? null : d.A) >
            (double?)3 * ((d == null ? null : d.Nested) == null ? null : d.Nested.B);

        e.PrintCSharp();
        var @cs = (Func<Obj, bool>)((Obj d) => //bool
            (d == null ? (double?)null :
            d.A) > (((double?)3) * (((d == null ? (Nested)null :
            d.Nested) == null) ? (double?)null :
            d.Nested.B)));

        var data = new Obj(10, new Nested(5));
        Assert.IsFalse(@cs(data));

        var fs = e.CompileSys();
        fs.PrintIL();

        var ff = e.CompileFast(true);
        ff.PrintIL();

        Assert.IsFalse(fs(data));
        Assert.IsFalse(ff(data));
    }

    [Test]
    public void Case_1_Simplified_less_reversed_mul_args()
    {
        Expression<Func<Obj, bool>> e = d =>
            (d == null ? null : d.A) >
            (d.Nested == null ? null : d.Nested.B) * (double?)3;

        e.PrintCSharp();

        var data = new Obj(10, new Nested(5));

        var fs = e.CompileSys();
        fs.PrintIL();

        var ff = e.CompileFast(true);
        ff.PrintIL();

        Assert.IsFalse(fs(data));
        Assert.IsFalse(ff(data));
    }

    [Test]
    public void Case_1_Simplified_less()
    {
        Expression<Func<Obj, bool>> e = d =>
            (d == null ? null : d.A) >
            (double?)3 * (d.Nested == null ? null : d.Nested.B);

        e.PrintCSharp();

        var data = new Obj(10, new Nested(5));

        var fs = e.CompileSys();
        fs.PrintIL();

        var ff = e.CompileFast(true);
        ff.PrintIL();

        Assert.IsFalse(fs(data));
        Assert.IsFalse(ff(data));
    }

    [Test]
    public void Case_1_simplified()
    {
        Expression<Func<Obj, bool>> e = d =>
            (d == null ? null : d.A) >
            (double?)3;

        e.PrintCSharp();

        var data = new Obj(10, new Nested(5));

        var fs = e.CompileSys();
        fs.PrintIL();

        var ff = e.CompileFast(true);
        ff.PrintIL();

        Assert.IsTrue(fs(data));
        Assert.IsTrue(ff(data));
    }
}

#endif

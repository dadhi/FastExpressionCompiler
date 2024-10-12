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
        // Original_case_1();
        return 1;
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
    public void Original_case_1()
    {
        Expression<Func<Obj, bool>> e = d =>
            (d == null ? null : d.A) > (double?)3 * ((d == null ? null : d.Nested) == null ? null : d.Nested.B);

        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();

        var ff = e.CompileFast(true);
        ff.PrintIL();

        var data = new Obj(10, new Nested(5));
        Assert.IsFalse(fs(data));
        Assert.IsFalse(ff(data));
    }
}

#endif

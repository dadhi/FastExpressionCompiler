#if !LIGHT_EXPRESSION

using System;
using System.Linq.Expressions;
using System.Reflection.Emit;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests;

[TestFixture]
public class Issue374_CompileFast_doesnot_work_with_HasFlag : ITest
{
    public int Run()
    {
        // Test1();
        return 1;
    }

    [Test]
    public void Test1()
    {
        Expression<Func<Bar, bool>> e = x => x.Foo.HasFlag(Foo.Green);
        e.PrintCSharp();
        var @cs = (Func<Bar, bool>)((Bar x) =>
            x.Foo.HasFlag(Foo.Green));

        var fs = e.CompileSys();
        fs.PrintIL();

        var b1 = new Bar { Foo = Foo.Green };
        var b2 = new Bar { Foo = Foo.Red | Foo.Blue };
        Assert.IsTrue(fs(b1));
        Assert.IsFalse(fs(b2));

        var ff = e.CompileFast(true);
        ff.PrintIL();
        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldflda,     // Bar.Foo
            OpCodes.Ldc_I4_4,
            OpCodes.Box,        // Foo
            OpCodes.Castclass,  // Enum
            OpCodes.Constrained,//Foo
            OpCodes.Call,       // Enum.HasFlag
            OpCodes.Ret
        );

        b1 = new Bar { Foo = Foo.Green };
        b2 = new Bar { Foo = Foo.Red | Foo.Blue };
        Assert.IsTrue(ff(b1));
        Assert.IsFalse(ff(b2));
    }

    [Flags]
    enum Foo
    {
        None = 0,
        Black = 1,
        Red = 2,
        Green = 4,
        Blue = 8
    }

    class Bar
    {
        public Foo Foo;
    }
}

#endif

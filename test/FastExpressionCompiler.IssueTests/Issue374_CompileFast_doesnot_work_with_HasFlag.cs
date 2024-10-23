using System;
using System.Reflection.Emit;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue374_CompileFast_doesnot_work_with_HasFlag : ITest
{
    public int Run()
    {
        Test1();
        return 1;
    }

    [Test]
    public void Test1()
    {
        System.Linq.Expressions.Expression<Func<Bar, bool>> se = x => x.Foo.HasFlag(Foo.Green);
        var e = se.FromSysExpression();
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
            OpCodes.Constrained,// Foo
            OpCodes.Callvirt,   // Enum.HasFlag
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

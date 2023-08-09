using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using System.Linq.Expressions;
using FastExpressionCompiler.LightExpression;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue352_xxxAssign_does_not_work_with_MemberAccess : ITest
    {
        public int Run()
        {
            // Check_ArrayAccess_AddAssign(); // todo: @wip
            Check_ArrayAccess_PreIncrement();
            Check_ArrayAccess_Add();

            Check_MemberAccess_AddAssign();
            Check_MemberAccess_PreIncrement();

            return 4;
        }

        [Test]
        public void Check_ArrayAccess_AddAssign()
        {
            var a = Parameter(typeof(int[]), "a");
            var e = Lambda<Action<int[]>>(
                Block(typeof(void), // todo: do we need the `typeof(void)` here and the `null` for vars? 
                    AddAssign(ArrayAccess(a, Constant(2)), Constant(33))
                ),
                a
            );
            e.PrintCSharp(); // fix output of non-void block in the void lambda/Action
            var @cs = (Action<int[]>)((int[] a) =>
            {
                a[2] += 33;
            });
            var a1 = new[] { 1, 2, 9 };
            @cs(a1);
            Assert.AreEqual(42, a1[2]);

            var fs = e.CompileSys();
            fs.PrintIL();

            var a2 = new[] { 1, 2, 9 };
            fs(a2);
            Assert.AreEqual(42, a2[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            ff(a2);
            Assert.AreEqual(75, a2[2]);
        }

        [Test]
        public void Check_ArrayAccess_PreIncrement()
        {
            var a = Parameter(typeof(int[]), "a");
            var e = Lambda<Action<int[]>>(
                Block(typeof(void),
                    PreIncrementAssign(ArrayAccess(a, Constant(2)))
                ),
                a
            );
            e.PrintCSharp(); // fix output of non-void block in the void lambda/Action
            var @cs = (Action<int[]>)((int[] a) =>
            {
                ++a[2];
            });
            var a1 = new[] { 1, 2, 9 };
            @cs(a1);
            Assert.AreEqual(10, a1[2]);

            var fs = e.CompileSys();
            fs.PrintIL();

            var a2 = new[] { 1, 2, 9 };
            fs(a2);
            Assert.AreEqual(10, a2[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            ff(a2);
            Assert.AreEqual(11, a2[2]);
        }

        [Test]
        public void Check_ArrayAccess_Add()
        {
            var a = Parameter(typeof(int[]), "a");
            var e = Lambda<Action<int[]>>(
                Block(typeof(void), // todo: do we need the `typeof(void)` here and the `null` for vars? 
                    Assign(ArrayAccess(a, Constant(1)), Add(ArrayAccess(a, Constant(1)), Constant(33)))
                ),
                a
            );
            e.PrintCSharp(); // fix output of non-void block in the void lambda/Action
            var @cs = (Action<int[]>)((int[] a) =>
            {
                a[1] = a[1] + 33;
            });
            var a1 = new[] { 1, 9, 3 };
            @cs(a1);
            Assert.AreEqual(42, a1[1]);

            var fs = e.CompileSys();
            fs.PrintIL();

            var a2 = new[] { 1, 9 };
            fs(a2);
            Assert.AreEqual(42, a2[1]);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff(a2);
            Assert.AreEqual(75, a2[1]);
        }

        class Box
        {
            public int Value;
        }

        [Test]
        public void Check_MemberAccess_AddAssign()
        {
            var b = Parameter(typeof(Box), "b");
            var bValueField = typeof(Box).GetField(nameof(Box.Value));
            var e = Lambda<Action<Box>>(
                Block(typeof(void), // todo: do we need the `typeof(void)` here and the `null` for vars? 
                    AddAssign(Field(b, bValueField), Constant(33))
                ),
                b
            );
            e.PrintCSharp(); // fix output of non-void block in the void lambda/Action
            var @cs = (Action<Box>)((Box b) =>
            {
                b.Value += 33;
            });
            var b1 = new Box { Value = 9 };
            @cs(b1);
            Assert.AreEqual(42, b1.Value);

            var fs = e.CompileSys();
            fs.PrintIL();

            var box = new Box { Value = 9 };
            fs(box);
            Assert.AreEqual(42, box.Value);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            ff(box);
            Assert.AreEqual(75, box.Value);
        }

        [Test]
        public void Check_MemberAccess_PreIncrement()
        {
            var b = Parameter(typeof(Box), "b");
            var bValueField = typeof(Box).GetField(nameof(Box.Value));
            var e = Lambda<Action<Box>>(
                Block(typeof(void),
                    PreIncrementAssign(Field(b, bValueField))
                ),
                b
            );
            e.PrintCSharp(); // fix output of non-void block in the void lambda/Action
            var @cs = (Action<Box>)((Box b) =>
            {
                ++b.Value;
            });
            var b1 = new Box { Value = 9 };
            @cs(b1);
            Assert.AreEqual(10, b1.Value);

            var fs = e.CompileSys();
            fs.PrintIL();

            var box = new Box { Value = 9 };
            fs(box);
            Assert.AreEqual(10, box.Value);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            ff(box);
            Assert.AreEqual(11, box.Value);
        }
    }
}
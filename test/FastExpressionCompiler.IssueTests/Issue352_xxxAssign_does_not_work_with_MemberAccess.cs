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
            // Check_ArrayAccess_AddAssign();

            Check_MemberAccess_AddAssign();
            Check_MemberAccess_PreIncrement();
            
            return 3;
        }

        [Test]
        public void Check_ArrayAccess_AddAssign()
        {
            var a = Parameter(typeof(int[]), "a");
            var e = Lambda<Action<int[]>>(
                Block(typeof(void), // todo: do we need the `typeof(void)` here and the `null` for vars? 
                    AddAssign(ArrayAccess(a, Constant(1)), Constant(33))
                ),
                a
            );
            e.PrintCSharp(); // fix output of non-void block in the void lambda/Action
            // Assert.AreEqual(42, b1.Value);
            
            var fs = e.CompileSys();
            fs.PrintIL();

            var arr = new[] { 1, 9 };
            fs(arr);
            Assert.AreEqual(42, arr[1]);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            ff(arr);
            Assert.AreEqual(75, arr[1]);
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
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
            Check_ArrayAccess_Assign_InAction();
            Check_ArrayAccess_AddAssign_InAction();
            Check_ArrayAccess_AddAssign_ReturnResultInFunction();

            Check_ArrayAccess_PreIncrement();
            Check_ArrayAccess_Add();

            Check_MemberAccess_AddAssign();
            // Check_MemberAccess_AddAssign_ToNewExpression(); // todo: @wip @fixme
            Check_MemberAccess_PreIncrementAssign();
            Check_MemberAccess_PreIncrementAssign_Nullable();
            Check_MemberAccess_PreDecrementAssign_ToNewExpression();

            return 10;
        }

        [Test]
        public void Check_ArrayAccess_Assign_InAction()
        {
            var a = Parameter(typeof(int[]), "a");
            var e = Lambda<Action<int[]>>(
                Block(
                    Assign(ArrayAccess(a, Constant(2)), Constant(33))
                ),
                a
            );
            e.PrintCSharp();
            var @cs = (Action<int[]>)((int[] a) =>
            {
                a[2] = 33;
            });
            var a1 = new[] { 1, 2, 9 };
            @cs(a1);
            Assert.AreEqual(33, a1[2]);

            var fs = e.CompileSys();
            fs.PrintIL();

            var a2 = new[] { 1, 2, 9 };
            fs(a2);
            Assert.AreEqual(33, a2[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            var a3 = new[] { 1, 2, 9 };
            ff(a3);
            Assert.AreEqual(33, a3[2]);
        }

        [Test]
        public void Check_ArrayAccess_AddAssign_InAction()
        {
            var a = Parameter(typeof(int[]), "a");
            var e = Lambda<Action<int[]>>(
                Block(AddAssign(ArrayAccess(a, Constant(2)), Constant(33))),
                a
            );
            e.PrintCSharp();
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
        public void Check_ArrayAccess_AddAssign_ReturnResultInFunction()
        {
            var a = Parameter(typeof(int[]), "a");
            var e = Lambda<Func<int[], int>>(
                Block(typeof(int),
                    AddAssign(ArrayAccess(a, Constant(2)), Constant(33))
                ),
                a
            );
            e.PrintCSharp();
            var @cs = (Func<int[], int>)((int[] a) =>
            {
                return a[2] += 33;
            });
            var a1 = new[] { 1, 2, 9 };
            @cs(a1);
            Assert.AreEqual(42, a1[2]);

            var fs = e.CompileSys();
            fs.PrintIL();

            var a2 = new[] { 1, 2, 9 };
            var res = fs(a2);
            Assert.AreEqual(42, res);
            Assert.AreEqual(res, a2[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            res = ff(a2);
            Assert.AreEqual(75, res);
            Assert.AreEqual(res, a2[2]);
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
            e.PrintCSharp();
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
        public void Check_ArrayAccess_PreIncrement_Nullable()
        {
            var a = Parameter(typeof(int[]), "a");
            var e = Lambda<Action<int[]>>(
                Block(typeof(void),
                    PreIncrementAssign(ArrayAccess(a, Constant(2)))
                ),
                a
            );
            e.PrintCSharp();
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
                Block(typeof(void),
                    Assign(ArrayAccess(a, Constant(1)), Add(ArrayAccess(a, Constant(1)), Constant(33)))
                ),
                a
            );
            e.PrintCSharp();
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
            public int? NullableValue;

            public Box() { }

            public static int CtorCalls = 0;
            public Box(int value)
            {
                ++CtorCalls;
                Value = value;
            }
        }

        [Test]
        public void Check_MemberAccess_AddAssign()
        {
            var b = Parameter(typeof(Box), "b");
            var bValueField = typeof(Box).GetField(nameof(Box.Value));
            var e = Lambda<Action<Box>>(
                Block(typeof(void),
                    AddAssign(Field(b, bValueField), Constant(33))
                ),
                b
            );
            e.PrintCSharp();
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
        public void Check_MemberAccess_AddAssign_ToNewExpression()
        {
            Box.CtorCalls = 0;
            var bCtor = typeof(Box).GetConstructor(new[] { typeof(int) });
            var bValueField = typeof(Box).GetField(nameof(Box.Value));

            var e = Lambda<Func<int>>(
                Block(
                    AddAssign(Field(New(bCtor, Constant(42)), bValueField), Constant(33))
                )
            );
            e.PrintCSharp();
            var @cs = (Func<int>)(() =>
            {
                return new Box(42).Value += 33;
            });
            var a = @cs();
            Assert.AreEqual(42 + 33, a);

            var fs = e.CompileSys();
            fs.PrintIL();

            var x = fs();
            Assert.AreEqual(42 + 33, x);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            var y = ff();
            Assert.AreEqual(42 + 33, y);
            // Assert.AreEqual(3, Box.CtorCalls); // todo: @wip @fixme
        }

        [Test]
        public void Check_MemberAccess_PreIncrementAssign()
        {
            var b = Parameter(typeof(Box), "b");
            var bValueField = typeof(Box).GetField(nameof(Box.Value));
            var e = Lambda<Action<Box>>(
                Block(typeof(void),
                    PreIncrementAssign(Field(b, bValueField))
                ),
                b
            );
            e.PrintCSharp();
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

        [Test]
        public void Check_MemberAccess_PreIncrementAssign_Nullable()
        {
            var b = Parameter(typeof(Box), "b");
            var bValueField = typeof(Box).GetField(nameof(Box.NullableValue));
            var e = Lambda<Action<Box>>(
                Block(typeof(void),
                    PreIncrementAssign(Field(b, bValueField))
                ),
                b
            );
            e.PrintCSharp();
            var @cs = (Action<Box>)((Box b) =>
            {
                ++b.NullableValue;
            });
            var b1 = new Box { NullableValue = null };
            var b2 = new Box { NullableValue = 41 };
            @cs(b1);
            @cs(b2);
            Assert.AreEqual(null, b1.NullableValue);
            Assert.AreEqual(42, b2.NullableValue);

            var fs = e.CompileSys();
            fs.PrintIL();
            /*
            0    ldarg.1 <-- load the Box argument on stack
            1    stloc.0
            2    ldloc.0 <-- on stack
            3    ldloc.0 <-- todo: @perf twice on stack or do Dup instead?
            4    ldfld Box.NullableValue
            9    stloc.1 <-- store and pop, so the ldloc.0 is again on top
            10   ldloca.s 1 <-- load the address of field variable, not the address of field
            12   call Nullable`1.get_HasValue <-- if does not have value, then jump to 35
            17   brfalse.s 35
            19   ldloca.s 1 <-- load the address of field variable
            21   call Nullable`1.GetValueOrDefault <-- get it value on stack
            26   ldc.i4.1 <-- load 1 on stack
            27   add <-- add 1 to value
            28   newobj Nullable`1..ctor <-- create new Nullable with the added value
            33   br.s 36 <-- jump to 36 storing the value in the field
            35   ldloc.1 <-- todo: @perf load the field variable not the address and store it in NullableValue, why if we may just jump to return?
            36   stfld Box.NullableValue
            41   ret
            */

            b1 = new Box { NullableValue = null };
            b2 = new Box { NullableValue = 41 };
            fs(b1);
            fs(b2);
            Assert.AreEqual(null, b1.NullableValue);
            Assert.AreEqual(42, b2.NullableValue);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            b1 = new Box { NullableValue = null };
            b2 = new Box { NullableValue = 41 };
            ff(b1);
            ff(b2);
            Assert.AreEqual(null, b1.NullableValue);
            Assert.AreEqual(42, b2.NullableValue);
        }

        [Test]
        public void Check_MemberAccess_PreDecrementAssign_ToNewExpression()
        {
            Box.CtorCalls = 0; // assuming that the tests are not running in parallel
            var bCtor = typeof(Box).GetConstructor(new[] { typeof(int) });
            var bValueField = typeof(Box).GetField(nameof(Box.Value));

            var e = Lambda<Func<int>>(
                Block(
                    PreDecrementAssign(Field(New(bCtor, Constant(42)), bValueField))
                )
            );
            e.PrintCSharp();
            var @cs = (Func<int>)(() =>
            {
                return --new Box(42).Value;
            });
            var a = @cs();
            Assert.AreEqual(41, a);

            var fs = e.CompileSys();
            fs.PrintIL();

            var x = fs();
            Assert.AreEqual(41, x);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            var y = ff();
            Assert.AreEqual(41, y);
            Assert.AreEqual(3, Box.CtorCalls);
        }
    }
}
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
            Check_MemberAccess_PlusOneAssign();
            Check_MemberAccess_AddAssign();

            // Check_MemberAccess_AddAssign_ToNewExpression(); // todo: @wip @fixme

            Check_Ref_ValueType_MemberAccess_PostIncrementAssign_Nullable_ReturningNullable();
            Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable_ReturningNullable();
            Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable();

            Check_Ref_ValueType_MemberAccess_PostIncrementAssign_Returning();
            Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Returning();
            Check_Ref_ValueType_MemberAccess_PreIncrementAssign();

            Check_MemberAccess_PreIncrementAssign();

            Check_MemberAccess_PreIncrementAssign_Returning();
            Check_MemberAccess_PostIncrementAssign_Returning();

            Check_MemberAccess_PreDecrementAssign_ToNewExpression();

            Check_MemberAccess_PreIncrementAssign_Nullable();
            Check_MemberAccess_PreIncrementAssign_Nullable_ReturningNullable();
            Check_MemberAccess_PostIncrementAssign_Nullable_ReturningNullable();

            Check_ArrayAccess_Assign_InAction();
            Check_ArrayAccess_AddAssign_InAction();
            Check_ArrayAccess_AddAssign_ReturnResultInFunction();

            Check_ArrayAccess_PreIncrement();
            Check_ArrayAccess_Add();

            return 19;
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

        struct Val
        {
            public int Value;
            public int? NullableValue;

            public Val() { }

            public static int CtorCalls = 0;
            public Val(int value)
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
                Block(AddAssign(Field(b, bValueField), Constant(33))),
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
                Block(PreIncrementAssign(Field(b, bValueField))),
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
            /*
                // for comparison how FEC may optimize it:
                OpCodes.Ldarg_1,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Ldfld,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stfld,
                OpCodes.Ret
            */
            b1 = new Box { Value = 9 };
            fs(b1);
            Assert.AreEqual(10, b1.Value);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Dup,
                OpCodes.Ldfld,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stfld,
                OpCodes.Ret
            );

            b1 = new Box { Value = 9 };
            ff(b1);
            Assert.AreEqual(10, b1.Value);
        }

        [Test]
        public void Check_MemberAccess_PlusOneAssign()
        {
            var b = Parameter(typeof(Box), "b");
            var bValueField = typeof(Box).GetField(nameof(Box.Value));
            var e = Lambda<Action<Box>>(
                Block(AddAssign(Field(b, bValueField), Constant(1))),
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

            b1 = new Box { Value = 9 };
            fs(b1);
            Assert.AreEqual(10, b1.Value);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Dup,
                OpCodes.Ldfld,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stfld,
                OpCodes.Ret
            );

            b1 = new Box { Value = 9 };
            ff(b1);
            Assert.AreEqual(10, b1.Value);
        }

        [Test]
        public void Check_MemberAccess_PreIncrementAssign_Returning()
        {
            var b = Parameter(typeof(Box), "b");
            var bValueField = typeof(Box).GetField(nameof(Box.Value));
            var e = Lambda<Func<Box, int>>(
                Block(PreIncrementAssign(Field(b, bValueField))),
                b
            );
            e.PrintCSharp();
            var @cs = (Func<Box, int>)((Box b) =>
            {
                return ++b.Value;
            });

            var b1 = new Box { Value = 9 };
            var x1 = @cs(b1);
            Assert.AreEqual(10, b1.Value);
            Assert.AreEqual(10, x1);

            var fs = e.CompileSys();
            fs.PrintIL();

            b1 = new Box { Value = 9 };
            x1 = fs(b1);
            Assert.AreEqual(10, b1.Value);
            Assert.AreEqual(10, x1);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            b1 = new Box { Value = 9 };
            x1 = ff(b1);
            Assert.AreEqual(10, b1.Value);
            Assert.AreEqual(10, x1);
        }

        [Test]
        public void Check_MemberAccess_PostIncrementAssign_Returning()
        {
            var b = Parameter(typeof(Box), "b");
            var bValueField = typeof(Box).GetField(nameof(Box.Value));
            var e = Lambda<Func<Box, int>>(
                Block(PostIncrementAssign(Field(b, bValueField))),
                b
            );
            e.PrintCSharp();

            var @cs = (Func<Box, int>)((Box b) =>
            {
                return b.Value++;
            });

            var b1 = new Box { Value = 9 };
            var x1 = @cs(b1);
            Assert.AreEqual(10, b1.Value);
            Assert.AreEqual(9, x1);

            var fs = e.CompileSys();
            fs.PrintIL();

            b1 = new Box { Value = 9 };
            x1 = fs(b1);
            Assert.AreEqual(10, b1.Value);
            Assert.AreEqual(9, x1);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            b1 = new Box { Value = 9 };
            x1 = ff(b1);
            Assert.AreEqual(10, b1.Value);
            Assert.AreEqual(9, x1);
        }

        delegate void RefVal(ref Val v);

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PreIncrementAssign()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vValueField = typeof(Val).GetField(nameof(Val.Value));
            var e = Lambda<RefVal>(
                Block(PreIncrementAssign(Field(v, vValueField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefVal)((ref Val v) =>
            {
                ++v.Value;
            });

            var v1 = new Val { Value = 9 };
            @cs(ref v1);
            Assert.AreEqual(10, v1.Value);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { Value = 9 };
            fs(ref v1);
            // Assert.AreEqual(10, v1.Value); // todo: @note that System.Compile does not work with ref ValueType.Member Increment/Decrement operations
            Assert.AreEqual(9, v1.Value); // todo: @note that System.Compile does not work with ref ValueType.Member Increment/Decrement operations

            var ff = e.CompileFast(true);
            ff.PrintIL();

            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldflda,
                OpCodes.Dup,
                OpCodes.Ldind_I4,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stind_I4,
                OpCodes.Ret
            );

            v1 = new Val { Value = 9 };
            ff(ref v1);
            Assert.AreEqual(10, v1.Value);
        }

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vValueField = typeof(Val).GetField(nameof(Val.NullableValue));
            var e = Lambda<RefVal>(
                Block(PreIncrementAssign(Field(v, vValueField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefVal)((ref Val v) =>
            {
                ++v.NullableValue;
            });

            var v1 = new Val { NullableValue = null };
            var v2 = new Val { NullableValue = 9 };
            @cs(ref v1);
            @cs(ref v2);
            Assert.AreEqual(null, v1.NullableValue);
            Assert.AreEqual(10, v2.NullableValue);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { NullableValue = null };
            v2 = new Val { NullableValue = 9 };
            fs(ref v1);
            fs(ref v2);
            Assert.AreEqual(null, v1.NullableValue);
            Assert.AreEqual(9, v2.NullableValue); // todo: @note that System.Compile does not work with ref ValueType.Member Increment/Decrement operations

            var ff = e.CompileFast(true);
            ff.PrintIL();

            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldflda,
                OpCodes.Dup,
                OpCodes.Ldobj,
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Dup,
                OpCodes.Call,       // System.Nullable`1<int32>::get_HasValue()
                OpCodes.Brfalse,    // jump to Pop(s) before the Ret op-code
                OpCodes.Call,       // System.Nullable`1<int32>::GetValueOrDefault()
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Stobj,      // Stores the nullable value to the address of the field
                OpCodes.Br_S,       // jump to Ret op-code
                OpCodes.Pop,        // Pops the Ldloca_S Dup-ped
                OpCodes.Pop,        // Pops the Ldflda Dup-ped
                OpCodes.Ret
            );

            v1 = new Val { NullableValue = null };
            v2 = new Val { NullableValue = 9 };
            ff(ref v1);
            ff(ref v2);
            Assert.AreEqual(null, v1.NullableValue);
            Assert.AreEqual(10, v2.NullableValue);
        }

        delegate int? RefValReturningNullable(ref Val v);

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable_ReturningNullable()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vValueField = typeof(Val).GetField(nameof(Val.NullableValue));
            var e = Lambda<RefValReturningNullable>(
                Block(PreIncrementAssign(Field(v, vValueField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefValReturningNullable)((ref Val v) =>
            {
                return ++v.NullableValue;
            });

            var v1 = new Val { NullableValue = null };
            var v2 = new Val { NullableValue = 9 };
            var x1 = @cs(ref v1);
            var x2 = @cs(ref v2);
            Assert.AreEqual(null, v1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(10, v2.NullableValue);
            Assert.AreEqual(10, x2);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { NullableValue = null };
            v2 = new Val { NullableValue = 9 };
            x1 = fs(ref v1);
            x2 = fs(ref v2);
            Assert.AreEqual(null, v1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(9, v2.NullableValue); // todo: @note that System.Compile does not work with ref ValueType.Member Increment/Decrement operations
            Assert.AreEqual(10, x2);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldflda,
                OpCodes.Dup,
                OpCodes.Ldobj,
                OpCodes.Stloc_0,    // we are using a single variable to store the field and to store the result
                OpCodes.Ldloca_S,
                OpCodes.Dup,
                OpCodes.Call,       // System.Nullable`1<int32>::get_HasValue()
                OpCodes.Brfalse,    // jump to Pop(s) before the Ret op-code
                OpCodes.Call,       // System.Nullable`1<int32>::GetValueOrDefault()
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Stobj,      // Stores the nullable value to the address of the field
                OpCodes.Br_S,       // jump to Ret op-code
                OpCodes.Pop,        // Pops the Ldloca_S Dup-ped
                OpCodes.Pop,        // Pops the Ldflda Dup-ped
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            v1 = new Val { NullableValue = null };
            v2 = new Val { NullableValue = 9 };
            x1 = ff(ref v1);
            x2 = ff(ref v2);
            Assert.AreEqual(null, v1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(10, v2.NullableValue);
            Assert.AreEqual(10, x2);
        }

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PostIncrementAssign_Nullable_ReturningNullable()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vValueField = typeof(Val).GetField(nameof(Val.NullableValue));
            var e = Lambda<RefValReturningNullable>(
                Block(PostIncrementAssign(Field(v, vValueField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefValReturningNullable)((ref Val v) =>
            {
                return v.NullableValue++;
            });

            var v1 = new Val { NullableValue = null };
            var v2 = new Val { NullableValue = 9 };
            var x1 = @cs(ref v1);
            var x2 = @cs(ref v2);
            Assert.AreEqual(null, v1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(10, v2.NullableValue);
            Assert.AreEqual(9, x2);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { NullableValue = null };
            v2 = new Val { NullableValue = 9 };
            x1 = fs(ref v1);
            x2 = fs(ref v2);
            Assert.AreEqual(null, v1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(9, v2.NullableValue); // todo: @note that System.Compile does not work with ref ValueType.Member Increment/Decrement operations
            Assert.AreEqual(9, x2);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldflda,
                OpCodes.Dup,
                OpCodes.Ldobj,
                OpCodes.Stloc_0,    // we are using a single variable to store the field and to store the result
                OpCodes.Ldloca_S,
                OpCodes.Dup,
                OpCodes.Call,       // System.Nullable`1<int32>::get_HasValue()
                OpCodes.Brfalse,    // jump to Pop(s) before the Ret op-code
                OpCodes.Call,       // System.Nullable`1<int32>::GetValueOrDefault()
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Stobj,      // Stores the nullable value to the address of the field
                OpCodes.Br_S,       // jump to Ret op-code
                OpCodes.Pop,        // Pops the Ldloca_S Dup-ped
                OpCodes.Pop,        // Pops the Ldflda Dup-ped
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            v1 = new Val { NullableValue = null };
            v2 = new Val { NullableValue = 9 };
            x1 = ff(ref v1);
            x2 = ff(ref v2);
            Assert.AreEqual(null, v1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(10, v2.NullableValue);
            Assert.AreEqual(9, x2);
        }

        delegate int RefValReturning(ref Val v);

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Returning()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vValueField = typeof(Val).GetField(nameof(Val.Value));
            var e = Lambda<RefValReturning>(
                Block(PreIncrementAssign(Field(v, vValueField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefValReturning)((ref Val v) =>
            {
                return ++v.Value;
            });

            var v1 = new Val { Value = 9 };
            var x1 = @cs(ref v1);
            Assert.AreEqual(10, v1.Value);
            Assert.AreEqual(10, x1);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { Value = 9 };
            x1 = fs(ref v1);
            Assert.AreEqual(9, v1.Value); // todo: @note that System.Compile does not work with ref ValueType.Member Increment/Decrement operations
            Assert.AreEqual(10, x1);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldflda,
                OpCodes.Dup,
                OpCodes.Ldind_I4,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Stind_I4,
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            v1 = new Val { Value = 9 };
            x1 = ff(ref v1);
            Assert.AreEqual(10, v1.Value);
            Assert.AreEqual(10, x1);
        }

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PostIncrementAssign_Returning()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vValueField = typeof(Val).GetField(nameof(Val.Value));
            var e = Lambda<RefValReturning>(
                Block(PostIncrementAssign(Field(v, vValueField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefValReturning)((ref Val v) =>
            {
                return v.Value++;
            });

            var v1 = new Val { Value = 9 };
            var x1 = @cs(ref v1);
            Assert.AreEqual(10, v1.Value);
            Assert.AreEqual(9, x1);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { Value = 9 };
            x1 = fs(ref v1);
            Assert.AreEqual(9, v1.Value); // todo: @note that System.Compile does not work with ref ValueType.Member Increment/Decrement operations
            Assert.AreEqual(9, x1);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldflda,
                OpCodes.Dup,
                OpCodes.Ldind_I4,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stind_I4,
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            v1 = new Val { Value = 9 };
            x1 = ff(ref v1);
            Assert.AreEqual(10, v1.Value);
            Assert.AreEqual(9, x1);
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
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Dup,
                OpCodes.Ldfld,
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Dup,
                OpCodes.Call,
                OpCodes.Brfalse,
                OpCodes.Call,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Stfld,
                OpCodes.Br_S,
                OpCodes.Pop,
                OpCodes.Pop,
                OpCodes.Ret
            );

            b1 = new Box { NullableValue = null };
            b2 = new Box { NullableValue = 41 };
            ff(b1);
            ff(b2);
            Assert.AreEqual(null, b1.NullableValue);
            Assert.AreEqual(42, b2.NullableValue);
        }

        [Test]
        public void Check_MemberAccess_PreIncrementAssign_Nullable_ReturningNullable()
        {
            var b = Parameter(typeof(Box), "b");
            var bValueField = typeof(Box).GetField(nameof(Box.NullableValue));
            var e = Lambda<Func<Box, int?>>(
                Block(PreIncrementAssign(Field(b, bValueField))),
                b
            );
            e.PrintCSharp();
            var @cs = (Func<Box, int?>)((Box b) =>
            {
                return ++b.NullableValue;
            });
            var b1 = new Box { NullableValue = null };
            var b2 = new Box { NullableValue = 41 };
            var x1 = @cs(b1);
            var x2 = @cs(b2);
            Assert.AreEqual(null, b1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableValue);
            Assert.AreEqual(42, x2);

            var fs = e.CompileSys();
            fs.PrintIL();
            /*
            */

            b1 = new Box { NullableValue = null };
            b2 = new Box { NullableValue = 41 };
            x1 = fs(b1);
            x2 = fs(b2);
            Assert.AreEqual(null, b1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableValue);
            Assert.AreEqual(42, x2);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Dup,
                OpCodes.Ldfld,
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Dup,
                OpCodes.Call,
                OpCodes.Brfalse,
                OpCodes.Call,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Stfld,
                OpCodes.Br_S,
                OpCodes.Pop,
                OpCodes.Pop,
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            b1 = new Box { NullableValue = null };
            b2 = new Box { NullableValue = 41 };
            x1 = ff(b1);
            x2 = ff(b2);
            Assert.AreEqual(null, b1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableValue);
            Assert.AreEqual(42, x2);
        }

        [Test]
        public void Check_MemberAccess_PostIncrementAssign_Nullable_ReturningNullable()
        {
            var b = Parameter(typeof(Box), "b");
            var bValueField = typeof(Box).GetField(nameof(Box.NullableValue));
            var e = Lambda<Func<Box, int?>>(
                Block(PostIncrementAssign(Field(b, bValueField))),
                b
            );
            e.PrintCSharp();
            var @cs = (Func<Box, int?>)((Box b) =>
            {
                return b.NullableValue++;
            });
            var b1 = new Box { NullableValue = null };
            var b2 = new Box { NullableValue = 41 };
            var x1 = @cs(b1);
            var x2 = @cs(b2);
            Assert.AreEqual(null, b1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableValue);
            Assert.AreEqual(41, x2);

            var fs = e.CompileSys();
            fs.PrintIL();
            /*
            */

            b1 = new Box { NullableValue = null };
            b2 = new Box { NullableValue = 41 };
            x1 = fs(b1);
            x2 = fs(b2);
            Assert.AreEqual(null, b1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableValue);
            Assert.AreEqual(41, x2);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            b1 = new Box { NullableValue = null };
            b2 = new Box { NullableValue = 41 };
            x1 = ff(b1);
            x2 = ff(b2);
            Assert.AreEqual(null, b1.NullableValue);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableValue);
            Assert.AreEqual(41, x2);
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
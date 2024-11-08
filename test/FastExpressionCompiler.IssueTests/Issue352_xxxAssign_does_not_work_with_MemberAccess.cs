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
            Check_ArrayAccess_Add();
            Check_ArrayAccess_AddAssign_PlusOne();
            Check_Val_IndexerAccess_Assign_InAction();
            Check_Val_Ref_IndexerAccess_AddAssign_PlusOne_InAction();
            Check_Val_IndexerAccess_AddAssign_PlusOne_InAction();
            Check_Val_Ref_IndexerAccess_Assign_InAction();
            Check_ArrayAccess_Assign_ParameterByRef_InAction();

            Check_MultiArrayAccess_AddAssign_PlusOne();
            Check_IndexerAccess_AddAssign_PlusOne_InAction();
            Check_ArrayAccess_AddAssign_NullablePlusNullable();
            Check_Ref_ArrayAccess_AddAssign_PlusOne();
            Check_Val_Ref_NoIndexer_AddAssign_PlusOne();

            Check_ArrayAccess_PreIncrement();
            Check_ArrayAccess_AddAssign_InAction();
            Check_ArrayAccess_AddAssign_ReturnResultInFunction();

            Check_MultiArrayAccess_Assign_InAction();
            Check_IndexerAccess_Assign_InAction();
            Check_ArrayAccess_Assign_InAction();

            Check_MemberAccess_AddAssign_ToNewExpression();
            Check_MemberAccess_AddAssign_StaticMember();
            Check_MemberAccess_AddAssign_StaticProp();
            Check_MemberAccess_AddAssign();
            Check_MemberAccess_PlusOneAssign();
            Check_MemberAccess_AddAssign_NullablePlusNullable();
            Check_MemberAccess_AddAssign_NullablePlusNullable_Prop();

            Check_Ref_ValueType_MemberAccess_PostIncrementAssign_Nullable_ReturningNullable();
            Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable_ReturningNullable();
            Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable_ReturningNullable_Prop();
            Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable();
            Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable_Prop();
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

            return 40;
        }

        [Test]
        public void Check_ArrayAccess_Assign_InAction()
        {
            var a = Parameter(typeof(int[]), "a");
            var e = Lambda<Action<int[]>>(
                Block(Assign(ArrayAccess(a, Constant(2)), Constant(33))),
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
            a1 = new[] { 1, 2, 9 };
            fs(a1);
            Assert.AreEqual(33, a1[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldc_I4_2,
                OpCodes.Ldc_I4_S, // 33
                OpCodes.Stelem_I4,
                OpCodes.Ret
            );
            a1 = new[] { 1, 2, 9 };
            ff(a1);
            Assert.AreEqual(33, a1[2]);
        }

        public delegate void ArrAndRefParam(int[] a, ref int b);

        [Test]
        public void Check_ArrayAccess_Assign_ParameterByRef_InAction()
        {
            var a = Parameter(typeof(int[]), "a");
            var b = Parameter(typeof(int).MakeByRefType(), "b");
            var e = Lambda<ArrAndRefParam>(
                Block(Assign(ArrayAccess(a, Constant(2)), b)),
                a, b
            );
            e.PrintCSharp();
            var @cs = (ArrAndRefParam)((
                int[] a,
                ref int b) =>
            {
                a[2] = b;
            });
            var a1 = new[] { 1, 2, 9 };
            var b1 = 33;
            @cs(a1, ref b1);
            Assert.AreEqual(33, a1[2]);

            var fs = e.CompileSys();
            fs.PrintIL();
            a1 = new[] { 1, 2, 9 };
            fs(a1, ref b1);
            Assert.AreEqual(33, a1[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldc_I4_2,
                OpCodes.Ldarg_2,
                OpCodes.Ldind_I4,
                OpCodes.Stelem_I4,
                OpCodes.Ret
            );
            a1 = new[] { 1, 2, 9 };
            ff(a1, ref b1);
            Assert.AreEqual(33, a1[2]);
        }

        [Test]
        public void Check_MultiArrayAccess_Assign_InAction()
        {
            var a = Parameter(typeof(int[,]), "a");
            var e = Lambda<Action<int[,]>>(
                Block(Assign(ArrayAccess(a, Constant(1), Constant(2)), Constant(33))),
                a
            );
            e.PrintCSharp();
            var @cs = (Action<int[,]>)((int[,] a) =>
            {
                a[1, 2] = 33;
            });
            var a1 = new[,] { { 1, 2, 9 }, { 3, 4, 5 } };
            @cs(a1);
            Assert.AreEqual(33, a1[1, 2]);

            var fs = e.CompileSys();
            fs.PrintIL();
            a1 = new[,] { { 1, 2, 9 }, { 3, 4, 5 } };
            fs(a1);
            Assert.AreEqual(33, a1[1, 2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldc_I4_1,
                OpCodes.Ldc_I4_2,
                OpCodes.Ldc_I4_S, // 33
                OpCodes.Call,     // Array.Set
                OpCodes.Ret
            );
            a1 = new[,] { { 1, 2, 9 }, { 3, 4, 5 } };
            ff(a1);
            Assert.AreEqual(33, a1[1, 2]);
        }

        public class Arr
        {
            public int Elem;
            public int this[string s, int i]
            {
                get => s == "a" ? i < 0 ? -1 : 1 : i < 0 ? -2 : 2;
                set
                {
                    if (s == "a")
                        Elem = value;
                    else
                        Elem = -value;
                }
            }
        }

        public struct ArrVal
        {
            public int Elem;
            public int this[string s, int i]
            {
                get => s == "a" ? i < 0 ? -1 : 1 : i < 0 ? -2 : 2;
                set
                {
                    if (s == "a")
                        Elem = value;
                    else
                        Elem = -value;
                }
            }
        }

        public struct ArrValNoIndexer
        {
            public int Elem;
            public int GetItem(string s, int i) =>
                s == "a" ? i < 0 ? -1 : 1 : i < 0 ? -2 : 2;
            public void SetItem(string s, int i, int value)
            {
                if (s == "a")
                    Elem = value;
                else
                    Elem = -value;
            }
        }

        [Test]
        public void Check_IndexerAccess_Assign_InAction()
        {
            var a = Parameter(typeof(Arr), "a");
            var e = Lambda<Action<Arr>>(
                Block(Assign(Property(a, "Item", Constant("b"), Constant(2)), Constant(33))),
                a);

            e.PrintCSharp();
            var @cs = (Action<Arr>)((Arr a) =>
            {
                a["b", 2] = 33;
            });
            var a1 = new Arr { Elem = 9 };
            @cs(a1);
            Assert.AreEqual(-33, a1.Elem);

            var fs = e.CompileSys();
            fs.PrintIL();
            a1 = new Arr { Elem = 9 };
            fs(a1);
            Assert.AreEqual(-33, a1.Elem);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldstr,      // "b"
                OpCodes.Ldc_I4_2,
                OpCodes.Ldc_I4_S,   // 33
                OpCodes.Call,       // Arr.set_Item
                OpCodes.Ret
            );
            a1 = new Arr { Elem = 9 };
            ff(a1);
            Assert.AreEqual(-33, a1.Elem);
        }

        [Test]
        public void Check_Val_IndexerAccess_Assign_InAction()
        {
            var a = Parameter(typeof(ArrVal), "a");
            var e = Lambda<Action<ArrVal>>(
                Block(Assign(Property(a, "Item", Constant("b"), Constant(2)), Constant(33))),
                a);

            e.PrintCSharp();
            var @cs = (Action<ArrVal>)((ArrVal a) =>
            {
                a["b", 2] = 33;
            });
            var a1 = new ArrVal { Elem = 9 };
            @cs(a1);
            Assert.AreEqual(9, a1.Elem); // does not change because passed-by-value

            var fs = e.CompileSys();
            fs.PrintIL();
            a1 = new ArrVal { Elem = 9 };
            fs(a1);
            Assert.AreEqual(9, a1.Elem);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarga_S,
                OpCodes.Ldstr,      // "b"
                OpCodes.Ldc_I4_2,
                OpCodes.Ldc_I4_S,   // 33
                OpCodes.Call,       // Arr.set_Item
                OpCodes.Ret
            );
            a1 = new ArrVal { Elem = 9 };
            ff(a1);
            Assert.AreEqual(9, a1.Elem);
        }

        delegate void RefArrVal(ref ArrVal a);

        [Test]
        public void Check_Val_Ref_IndexerAccess_Assign_InAction()
        {
            var a = Parameter(typeof(ArrVal).MakeByRefType(), "a");
            var e = Lambda<RefArrVal>(
                Block(Assign(Property(a, "Item", Constant("b"), Constant(2)), Constant(33))),
                a);

            e.PrintCSharp();
            var @cs = (RefArrVal)((ref ArrVal a) =>
            {
                a["b", 2] = 33;
            });
            var a1 = new ArrVal { Elem = 9 };
            @cs(ref a1);
            Assert.AreEqual(-33, a1.Elem);

            var fs = e.CompileSys();
            fs.PrintIL();
            a1 = new ArrVal { Elem = 9 };
            fs(ref a1);
            Assert.AreEqual(-33, a1.Elem);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldstr,      // "b"
                OpCodes.Ldc_I4_2,
                OpCodes.Ldc_I4_S,   // 33
                OpCodes.Call,       // Arr.set_Item
                OpCodes.Ret
            );
            a1 = new ArrVal { Elem = 9 };
            ff(ref a1);
            Assert.AreEqual(-33, a1.Elem);
        }

        [Test]
        public void Check_IndexerAccess_AddAssign_PlusOne_InAction()
        {
            var a = Parameter(typeof(Arr), "a");
            var e = Lambda<Action<Arr>>(
                Block(AddAssign(Property(a, "Item", Constant("b"), Constant(2)), Constant(1))),
                a);

            e.PrintCSharp();
            var @cs = (Action<Arr>)((Arr a) =>
            {
                a["b", 2] += 1;
            });
            var a1 = new Arr { Elem = 9 };
            @cs(a1);
            Assert.AreEqual(-3, a1.Elem);

            var fs = e.CompileSys();
            fs.PrintIL();

            a1 = new Arr { Elem = 9 };
            fs(a1);
            Assert.AreEqual(-3, a1.Elem);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            a1 = new Arr { Elem = 9 };
            ff(a1);
            Assert.AreEqual(-3, a1.Elem);
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
            var res = @cs(a1);
            Assert.AreEqual(42, a1[2]);
            Assert.AreEqual(res, a1[2]);

            var fs = e.CompileSys();
            fs.PrintIL();

            a1 = new[] { 1, 2, 9 };
            res = fs(a1);
            Assert.AreEqual(42, res);
            Assert.AreEqual(res, a1[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            a1 = new[] { 1, 2, 9 };
            res = ff(a1);
            Assert.AreEqual(42, res);
            Assert.AreEqual(res, a1[2]);
        }

        [Test]
        public void Check_ArrayAccess_PreIncrement()
        {
            var a = Parameter(typeof(int[]), "a");
            var e = Lambda<Action<int[]>>(
                Block(PreIncrementAssign(ArrayAccess(a, Constant(2)))),
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

            a1 = new[] { 1, 2, 9 };
            fs(a1);
            Assert.AreEqual(10, a1[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            a1 = new[] { 1, 2, 9 };
            ff(a1);
            Assert.AreEqual(10, a1[2]);
        }

        [Test]
        public void Check_ArrayAccess_AddAssign_PlusOne()
        {
            var a = Parameter(typeof(int[]), "a");
            var e = Lambda<Action<int[]>>(
                Block(AddAssign(ArrayAccess(a, Constant(2)), Constant(1))),
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

            a1 = new[] { 1, 2, 9 };
            fs(a1);
            Assert.AreEqual(10, a1[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            a1 = new[] { 1, 2, 9 };
            ff(a1);
            Assert.AreEqual(10, a1[2]);
        }

        [Test]
        public void Check_MultiArrayAccess_AddAssign_PlusOne()
        {
            var a = Parameter(typeof(int[,]), "a");
            var e = Lambda<Action<int[,]>>(
                Block(AddAssign(ArrayAccess(a, Constant(1), Constant(2)), Constant(1))),
                a
            );
            e.PrintCSharp();
            var @cs = (Action<int[,]>)((int[,] a) =>
            {
                ++a[1, 2];
            });
            var a1 = new[,] { { 1, 2, 9 }, { 3, 4, 5 } };
            @cs(a1);
            Assert.AreEqual(6, a1[1, 2]);

            var fs = e.CompileSys();
            fs.PrintIL();

            a1 = new[,] { { 1, 2, 9 }, { 3, 4, 5 } };
            fs(a1);
            Assert.AreEqual(6, a1[1, 2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            a1 = new[,] { { 1, 2, 9 }, { 3, 4, 5 } };
            ff(a1);
            Assert.AreEqual(6, a1[1, 2]);
        }

        delegate void RefArr(ref int[] a);

        [Test]
        public void Check_Ref_ArrayAccess_AddAssign_PlusOne()
        {
            var a = Parameter(typeof(int[]).MakeByRefType(), "a");
            var e = Lambda<RefArr>(
                Block(AddAssign(ArrayAccess(a, Constant(2)), Constant(1))),
                a
            );
            e.PrintCSharp();
            var @cs = (RefArr)((ref int[] a) =>
            {
                a[2] += 1;
            });
            var a1 = new[] { 1, 2, 9 };
            @cs(ref a1);
            Assert.AreEqual(10, a1[2]);

            var fs = e.CompileSys();
            fs.PrintIL();

            a1 = new[] { 1, 2, 9 };
            fs(ref a1);
            Assert.AreEqual(10, a1[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldind_Ref,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Ldc_I4_2,
                OpCodes.Stloc_1,
                OpCodes.Ldloc_1,
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_1,
                OpCodes.Ldelem_I4,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stelem_I4,
                OpCodes.Ret
            );

            a1 = new[] { 1, 2, 9 };
            ff(ref a1);
            Assert.AreEqual(10, a1[2]);
        }

        [Test]
        public void Check_Val_IndexerAccess_AddAssign_PlusOne_InAction()
        {
            var a = Parameter(typeof(ArrVal), "a");
            var e = Lambda<Action<ArrVal>>(
                Block(AddAssign(Property(a, "Item", Constant("b"), Constant(2)), Constant(1))),
                a);

            e.PrintCSharp();
            var @cs = (Action<ArrVal>)((ArrVal a) =>
            {
                a["b", 2] += 1;
            });
            var a1 = new ArrVal { Elem = 9 };
            @cs(a1);
            Assert.AreEqual(9, a1.Elem); // NOTE!!! it does not change because passed-by-value

            var fs = e.CompileSys();
            fs.PrintIL();
            a1 = new ArrVal { Elem = 9 };
            fs(a1);
            Assert.AreEqual(9, a1.Elem);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,   // 0
                OpCodes.Ldstr,      // "b"
                OpCodes.Stloc_1,
                OpCodes.Ldloc_1,
                OpCodes.Ldc_I4_2,
                OpCodes.Stloc_2,
                OpCodes.Ldloc_2,
                OpCodes.Ldloca_S,   // 0
                OpCodes.Ldloc_1,
                OpCodes.Ldloc_2,
                OpCodes.Call,       // ArrVal.get_Item
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Call,       // ArrVal.set_Item
                OpCodes.Ret
            );
            a1 = new ArrVal { Elem = 9 };
            ff(a1);
            Assert.AreEqual(9, a1.Elem);
        }

        [Test]
        public void Check_Val_Ref_IndexerAccess_AddAssign_PlusOne_InAction()
        {
            var a = Parameter(typeof(ArrVal).MakeByRefType(), "a");
            var e = Lambda<RefArrVal>(
                Block(AddAssign(Property(a, "Item", Constant("b"), Constant(2)), Constant(1))),
                a);

            e.PrintCSharp();
            var @cs = (RefArrVal)((ref ArrVal a) =>
            {
                a["b", 2] += 1;
            });
            var a1 = new ArrVal { Elem = 9 };
            @cs(ref a1);
            Assert.AreEqual(-3, a1.Elem);

            var fs = e.CompileSys();
            fs.PrintIL();
            a1 = new ArrVal { Elem = 9 };
            fs(ref a1);
            Assert.AreEqual(9, a1.Elem); // todo: @sys does not work, or is there bug in converting to the Expression?

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Ldstr,      // "b"
                OpCodes.Stloc_1,
                OpCodes.Ldloc_1,
                OpCodes.Ldc_I4_2,
                OpCodes.Stloc_2,
                OpCodes.Ldloc_2,
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_1,
                OpCodes.Ldloc_2,
                OpCodes.Call,       // ArrVal.get_Item
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Call,       // ArrVal.set_Item
                OpCodes.Ret
            );
            a1 = new ArrVal { Elem = 9 };
            ff(ref a1);
            Assert.AreEqual(-3, a1.Elem);
        }

        delegate void RefArrValNoIndexer(ref ArrValNoIndexer a);

        [Test]
        public void Check_Val_Ref_NoIndexer_AddAssign_PlusOne()
        {
            var a = Parameter(typeof(ArrValNoIndexer).MakeByRefType(), "a");
            var e = Lambda<RefArrValNoIndexer>(
                Call(a, typeof(ArrValNoIndexer).GetMethod(nameof(ArrValNoIndexer.SetItem)), Constant("b"), Constant(2),
                    Add(Call(a, typeof(ArrValNoIndexer).GetMethod(nameof(ArrValNoIndexer.GetItem)), Constant("b"), Constant(2)),
                        Constant(1))
                    ),
                a);

            e.PrintCSharp();
            var @cs = (RefArrValNoIndexer)((ref ArrValNoIndexer a) =>
            {
                a.SetItem(
                    "b",
                    2,
                    (a.GetItem(
                        "b",
                        2) + 1));
            });

            var a1 = new ArrValNoIndexer { Elem = 9 };
            @cs(ref a1);
            Assert.AreEqual(-3, a1.Elem);

            var fs = e.CompileSys();
            fs.PrintIL();
            a1 = new ArrValNoIndexer { Elem = 9 };
            fs(ref a1);
            Assert.AreEqual(-3, a1.Elem); // todo: @sys does not work, or is there bug in converting to the Expression?

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldstr,  // "b"
                OpCodes.Ldc_I4_2,
                OpCodes.Ldarg_1,
                OpCodes.Ldstr,  // "b"
                OpCodes.Ldc_I4_2,
                OpCodes.Call,   // ArrVal.get_Item
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Call,   // ArrVal.set_Item
                OpCodes.Ret
            );
            a1 = new ArrValNoIndexer { Elem = 9 };
            ff(ref a1);
            Assert.AreEqual(-3, a1.Elem);
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

            a1 = new[] { 1, 2, 9 };
            fs(a1);
            Assert.AreEqual(10, a1[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            a1 = new[] { 1, 2, 9 };
            ff(a1);
            Assert.AreEqual(10, a1[2]);
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

            a1 = new[] { 1, 9 };
            fs(a1);
            Assert.AreEqual(42, a1[1]);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            a1 = new[] { 1, 9 };
            ff(a1);
            Assert.AreEqual(42, a1[1]);
        }

        class Box
        {
            public static int StaticField;
            public static int StaticProp { get; set; }
            public int Field;
            public int Prop { get; set; }
            public int? NullableField;
            public int? NullableProp { get; set; }

            public Box() { }

            public static int CtorCalls = 0;
            public Box(int value)
            {
                ++CtorCalls;
                Field = value;
            }
        }

        struct Val
        {
            public int Field;
            public int? NullableField;
            public int? NullableProp { get; set; }

            public Val() { }

            public static int CtorCalls = 0;
            public Val(int value)
            {
                ++CtorCalls;
                Field = value;
            }
        }

        [Test]
        public void Check_MemberAccess_AddAssign_StaticMember()
        {
            var bField = typeof(Box).GetField(nameof(Box.StaticField));
            var e = Lambda<Action>(
                Block(AddAssign(Field(null, bField), Constant(33)))
            );
            e.PrintCSharp();
            var @cs = (Action)(() =>
            {
                Box.StaticField += 33;
            });
            Box.StaticField = 0;
            @cs();
            Assert.AreEqual(33, Box.StaticField);

            var fs = e.CompileSys();
            fs.PrintIL();

            Box.StaticField = 0;
            fs();
            Assert.AreEqual(33, Box.StaticField);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldsfld,
                OpCodes.Ldc_I4_S, // 33
                OpCodes.Add,
                OpCodes.Stsfld,
                OpCodes.Ret
            );

            Box.StaticField = 0;
            ff();
            Assert.AreEqual(33, Box.StaticField);
        }

        [Test]
        public void Check_MemberAccess_AddAssign_StaticProp()
        {
            var bField = typeof(Box).GetProperty(nameof(Box.StaticProp));
            var e = Lambda<Action>(
                Block(AddAssign(Property(null, bField), Constant(33)))
            );
            e.PrintCSharp();
            var @cs = (Action)(() =>
            {
                Box.StaticProp += 33;
            });
            Box.StaticProp = 0;
            @cs();
            Assert.AreEqual(33, Box.StaticProp);

            var fs = e.CompileSys();
            fs.PrintIL();

            Box.StaticProp = 0;
            fs();
            Assert.AreEqual(33, Box.StaticProp);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Call,       // Box.get_StaticProp
                OpCodes.Ldc_I4_S,   // 33
                OpCodes.Add,
                OpCodes.Call,       // Box.set_StaticProp
                OpCodes.Ret
            );

            Box.StaticProp = 0;
            ff();
            Assert.AreEqual(33, Box.StaticProp);
        }

        [Test]
        public void Check_MemberAccess_AddAssign()
        {
            var b = Parameter(typeof(Box), "b");
            var bField = typeof(Box).GetField(nameof(Box.Field));
            var e = Lambda<Action<Box>>(
                Block(AddAssign(Field(b, bField), Constant(33))),
                b
            );
            e.PrintCSharp();
            var @cs = (Action<Box>)((Box b) =>
            {
                b.Field += 33;
            });
            var b1 = new Box { Field = 9 };
            @cs(b1);
            Assert.AreEqual(42, b1.Field);

            var fs = e.CompileSys();
            fs.PrintIL();

            b1 = new Box { Field = 9 };
            fs(b1);
            Assert.AreEqual(42, b1.Field);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Dup,
                OpCodes.Ldfld,
                OpCodes.Ldc_I4_S,   // 33
                OpCodes.Add,
                OpCodes.Stfld,
                OpCodes.Ret
            );

            b1 = new Box { Field = 9 };
            ff(b1);
            Assert.AreEqual(42, b1.Field);
        }

        [Test]
        public void Check_MemberAccess_AddAssign_NullablePlusNullable()
        {
            var b = Parameter(typeof(Box), "b");
            var bField = typeof(Box).GetField(nameof(Box.NullableField));
            var e = Lambda<Action<Box>>(
                Block(AddAssign(Field(b, bField), Constant((int?)33, typeof(int?)))),
                b
            );
            e.PrintCSharp();
            var @cs = (Action<Box>)((Box b) =>
            {
                b.NullableField += (int?)33;
            });

            var b1 = new Box { NullableField = null };
            var b2 = new Box { NullableField = 9 };
            @cs(b1);
            @cs(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(42, b2.NullableField);

            var fs = e.CompileSys();
            fs.PrintIL();

            b1 = new Box { NullableField = null };
            b2 = new Box { NullableField = 9 };
            fs(b1);
            fs(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(42, b2.NullableField);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Dup,
                OpCodes.Ldfld,
                OpCodes.Stloc_0,
                OpCodes.Ldc_I4_S,   // 33
                OpCodes.Newobj,     // Nullable`1..ctor
                OpCodes.Stloc_1,
                OpCodes.Ldloca_S,   // 0
                OpCodes.Call,       // Nullable`1.get_HasValue
                OpCodes.Ldloca_S,   // 1
                OpCodes.Call,       // Nullable`1.get_HasValue
                OpCodes.And,
                OpCodes.Brfalse,    // --> Pop
                OpCodes.Ldloca_S,   // 0
                OpCodes.Ldfld,      // Nullable`1.GetValueOrDefault
                OpCodes.Ldloca_S,   // 1
                OpCodes.Ldfld,      // Nullable`1.GetValueOrDefault
                OpCodes.Add,
                OpCodes.Newobj,     // Nullable`1..ctor
                OpCodes.Stfld,      // Box.NullableValue
                OpCodes.Br_S,       // --> Ret
                OpCodes.Pop,
                OpCodes.Ret
            );

            b1 = new Box { NullableField = null };
            b2 = new Box { NullableField = 9 };
            ff(b1);
            ff(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(42, b2.NullableField);
        }

        [Test]
        public void Check_ArrayAccess_AddAssign_NullablePlusNullable()
        {
            var a = Parameter(typeof(int?[]), "a");
            var e = Lambda<Action<int?[]>>(
                Block(AddAssign(ArrayAccess(a, Constant(2)), Constant((int?)33, typeof(int?)))),
                a
            );
            e.PrintCSharp();
            var @cs = (Action<int?[]>)((int?[] a) =>
            {
                a[2] += (int?)33;
            });

            var a1 = new int?[] { 1, 2, null, 4 };
            var a2 = new int?[] { 1, 2, 9, 4 };
            @cs(a1);
            @cs(a2);
            Assert.AreEqual(null, a1[2]);
            Assert.AreEqual(42, a2[2]);

            var fs = e.CompileSys();
            fs.PrintIL();

            a1 = new int?[] { 1, 2, null, 4 };
            a2 = new int?[] { 1, 2, 9, 4 };
            fs(a1);
            fs(a2);
            Assert.AreEqual(null, a1[2]);
            Assert.AreEqual(42, a2[2]);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            // ff.AssertOpCodes(
            //     OpCodes.Ldarg_1,
            //     OpCodes.Dup,
            //     OpCodes.Ldfld,
            //     OpCodes.Stloc_0,
            //     OpCodes.Ldc_I4_S,   // 33
            //     OpCodes.Newobj,     // Nullable`1..ctor
            //     OpCodes.Stloc_1,
            //     OpCodes.Ldloca_S,   // 0
            //     OpCodes.Call,       // Nullable`1.get_HasValue
            //     OpCodes.Ldloca_S,   // 1
            //     OpCodes.Call,       // Nullable`1.get_HasValue
            //     OpCodes.And,
            //     OpCodes.Brfalse,    // --> Pop
            //     OpCodes.Ldloca_S,   // 0
            //     OpCodes.Ldfld,      // Nullable`1.GetValueOrDefault
            //     OpCodes.Ldloca_S,   // 1
            //     OpCodes.Ldfld,      // Nullable`1.GetValueOrDefault
            //     OpCodes.Add,
            //     OpCodes.Newobj,     // Nullable`1..ctor
            //     OpCodes.Stfld,      // Box.NullableValue
            //     OpCodes.Br_S,       // --> Ret
            //     OpCodes.Pop,
            //     OpCodes.Ret
            // );

            a1 = new int?[] { 1, 2, null, 4 };
            a2 = new int?[] { 1, 2, 9, 4 };
            fs(a1);
            fs(a2);
            Assert.AreEqual(null, a1[2]);
            Assert.AreEqual(42, a2[2]);
        }

        [Test]
        public void Check_MemberAccess_AddAssign_NullablePlusNullable_Prop()
        {
            var b = Parameter(typeof(Box), "b");
            var bProp = typeof(Box).GetProperty(nameof(Box.NullableProp));
            var e = Lambda<Action<Box>>(
                Block(AddAssign(Property(b, bProp), Constant((int?)33, typeof(int?)))),
                b
            );
            e.PrintCSharp();
            var @cs = (Action<Box>)((Box b) =>
            {
                b.NullableProp += (int?)33;
            });

            var b1 = new Box { NullableProp = null };
            var b2 = new Box { NullableProp = 9 };
            @cs(b1);
            @cs(b2);
            Assert.AreEqual(null, b1.NullableProp);
            Assert.AreEqual(42, b2.NullableProp);

            var fs = e.CompileSys();
            fs.PrintIL();

            b1 = new Box { NullableProp = null };
            b2 = new Box { NullableProp = 9 };
            fs(b1);
            fs(b2);
            Assert.AreEqual(null, b1.NullableProp);
            Assert.AreEqual(42, b2.NullableProp);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Dup,
                OpCodes.Call,       // Box.get_NullableProp
                OpCodes.Stloc_0,
                OpCodes.Ldc_I4_S,   // 33
                OpCodes.Newobj,     // Nullable`1..ctor
                OpCodes.Stloc_1,
                OpCodes.Ldloca_S,   // 0
                OpCodes.Call,       // Nullable`1.get_HasValue
                OpCodes.Ldloca_S,   // 1
                OpCodes.Call,       // Nullable`1.get_HasValue
                OpCodes.And,
                OpCodes.Brfalse,    // --> Pop
                OpCodes.Ldloca_S,   // 0
                OpCodes.Ldfld,      // Nullable`1.GetValueOrDefault
                OpCodes.Ldloca_S,   // 1
                OpCodes.Ldfld,      // Nullable`1.GetValueOrDefault
                OpCodes.Add,
                OpCodes.Newobj,     // Nullable`1..ctor
                OpCodes.Call,       // Box.set_NullableProp
                OpCodes.Br_S,       // --> Ret
                OpCodes.Pop,
                OpCodes.Ret
            );

            b1 = new Box { NullableProp = null };
            b2 = new Box { NullableProp = 9 };
            ff(b1);
            ff(b2);
            Assert.AreEqual(null, b1.NullableProp);
            Assert.AreEqual(42, b2.NullableProp);
        }

        [Test]
        public void Check_MemberAccess_AddAssign_ToNewExpression()
        {
            Box.CtorCalls = 0;
            var bCtor = typeof(Box).GetConstructor(new[] { typeof(int) });
            var bField = typeof(Box).GetField(nameof(Box.Field));

            var e = Lambda<Func<int>>(
                Block(AddAssign(Field(New(bCtor, Constant(42)), bField), Constant(33)))
            );
            e.PrintCSharp();
            Box.CtorCalls = 0;
            var @cs = (Func<int>)(() =>
            {
                return new Box(42).Field += 33;
            });
            var a = @cs();
            Assert.AreEqual(42 + 33, a);
            Assert.AreEqual(1, Box.CtorCalls);

            var fs = e.CompileSys();
            fs.PrintIL();

            Box.CtorCalls = 0;
            var x = fs();
            Assert.AreEqual(42 + 33, x);
            Assert.AreEqual(1, Box.CtorCalls);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            Box.CtorCalls = 0;
            var y = ff();
            Assert.AreEqual(42 + 33, y);
            Assert.AreEqual(1, Box.CtorCalls);
        }

        [Test]
        public void Check_MemberAccess_PreIncrementAssign()
        {
            var b = Parameter(typeof(Box), "b");
            var bField = typeof(Box).GetField(nameof(Box.Field));
            var e = Lambda<Action<Box>>(
                Block(PreIncrementAssign(Field(b, bField))),
                b
            );
            e.PrintCSharp();
            var @cs = (Action<Box>)((Box b) =>
            {
                ++b.Field;
            });
            var b1 = new Box { Field = 9 };
            @cs(b1);
            Assert.AreEqual(10, b1.Field);

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
            b1 = new Box { Field = 9 };
            fs(b1);
            Assert.AreEqual(10, b1.Field);

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

            b1 = new Box { Field = 9 };
            ff(b1);
            Assert.AreEqual(10, b1.Field);
        }

        [Test]
        public void Check_MemberAccess_PlusOneAssign()
        {
            var b = Parameter(typeof(Box), "b");
            var bField = typeof(Box).GetField(nameof(Box.Field));
            var e = Lambda<Action<Box>>(
                Block(AddAssign(Field(b, bField), Constant(1))),
                b
            );
            e.PrintCSharp();
            var @cs = (Action<Box>)((Box b) =>
            {
                ++b.Field;
            });
            var b1 = new Box { Field = 9 };
            @cs(b1);
            Assert.AreEqual(10, b1.Field);

            var fs = e.CompileSys();
            fs.PrintIL();

            b1 = new Box { Field = 9 };
            fs(b1);
            Assert.AreEqual(10, b1.Field);

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

            b1 = new Box { Field = 9 };
            ff(b1);
            Assert.AreEqual(10, b1.Field);
        }

        [Test]
        public void Check_MemberAccess_PreIncrementAssign_Returning()
        {
            var b = Parameter(typeof(Box), "b");
            var bField = typeof(Box).GetField(nameof(Box.Field));
            var e = Lambda<Func<Box, int>>(
                Block(PreIncrementAssign(Field(b, bField))),
                b
            );
            e.PrintCSharp();
            var @cs = (Func<Box, int>)((Box b) =>
            {
                return ++b.Field;
            });

            var b1 = new Box { Field = 9 };
            var x1 = @cs(b1);
            Assert.AreEqual(10, b1.Field);
            Assert.AreEqual(10, x1);

            var fs = e.CompileSys();
            fs.PrintIL();

            b1 = new Box { Field = 9 };
            x1 = fs(b1);
            Assert.AreEqual(10, b1.Field);
            Assert.AreEqual(10, x1);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            b1 = new Box { Field = 9 };
            x1 = ff(b1);
            Assert.AreEqual(10, b1.Field);
            Assert.AreEqual(10, x1);
        }

        [Test]
        public void Check_MemberAccess_PostIncrementAssign_Returning()
        {
            var b = Parameter(typeof(Box), "b");
            var bField = typeof(Box).GetField(nameof(Box.Field));
            var e = Lambda<Func<Box, int>>(
                Block(PostIncrementAssign(Field(b, bField))),
                b
            );
            e.PrintCSharp();

            var @cs = (Func<Box, int>)((Box b) =>
            {
                return b.Field++;
            });

            var b1 = new Box { Field = 9 };
            var x1 = @cs(b1);
            Assert.AreEqual(10, b1.Field);
            Assert.AreEqual(9, x1);

            var fs = e.CompileSys();
            fs.PrintIL();

            b1 = new Box { Field = 9 };
            x1 = fs(b1);
            Assert.AreEqual(10, b1.Field);
            Assert.AreEqual(9, x1);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            b1 = new Box { Field = 9 };
            x1 = ff(b1);
            Assert.AreEqual(10, b1.Field);
            Assert.AreEqual(9, x1);
        }

        delegate void RefVal(ref Val v);

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PreIncrementAssign()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vField = typeof(Val).GetField(nameof(Val.Field));
            var e = Lambda<RefVal>(
                Block(PreIncrementAssign(Field(v, vField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefVal)((ref Val v) =>
            {
                ++v.Field;
            });

            var v1 = new Val { Field = 9 };
            @cs(ref v1);
            Assert.AreEqual(10, v1.Field);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { Field = 9 };
            fs(ref v1);
            // Assert.AreEqual(10, v1.Value); // todo: @sys that System.Compile does not work with ref ValueType.Member Increment/Decrement operations
            Assert.AreEqual(9, v1.Field); // todo: @sys that System.Compile does not work with ref ValueType.Member Increment/Decrement operations

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

            v1 = new Val { Field = 9 };
            ff(ref v1);
            Assert.AreEqual(10, v1.Field);
        }

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vField = typeof(Val).GetField(nameof(Val.NullableField));
            var e = Lambda<RefVal>(
                Block(PreIncrementAssign(Field(v, vField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefVal)((ref Val v) =>
            {
                ++v.NullableField;
            });

            var v1 = new Val { NullableField = null };
            var v2 = new Val { NullableField = 9 };
            @cs(ref v1);
            @cs(ref v2);
            Assert.AreEqual(null, v1.NullableField);
            Assert.AreEqual(10, v2.NullableField);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { NullableField = null };
            v2 = new Val { NullableField = 9 };
            fs(ref v1);
            fs(ref v2);
            Assert.AreEqual(null, v1.NullableField);
            Assert.AreEqual(9, v2.NullableField); // todo: @sys that System.Compile does not work with ref ValueType.Member Increment/Decrement operations

            var ff = e.CompileFast(true);
            ff.PrintIL();

            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldflda,
                OpCodes.Dup,
                OpCodes.Ldobj,
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Call,       // System.Nullable`1<int32>::get_HasValue()
                OpCodes.Brfalse,    // jump to Pop(s) before the Ret op-code
                OpCodes.Ldloca_S,
                OpCodes.Ldfld,      // System.Nullable`1<int32>::GetValueOrDefault()
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Stobj,      // Stores the nullable value to the address of the field
                OpCodes.Br_S,       // jump to Ret op-code
                OpCodes.Pop,        // Pops the Ldflda Dup-ped
                OpCodes.Ret
            );

            v1 = new Val { NullableField = null };
            v2 = new Val { NullableField = 9 };
            ff(ref v1);
            ff(ref v2);
            Assert.AreEqual(null, v1.NullableField);
            Assert.AreEqual(10, v2.NullableField);
        }

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable_Prop()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vProp = typeof(Val).GetProperty(nameof(Val.NullableProp));
            var e = Lambda<RefVal>(
                Block(PreIncrementAssign(Property(v, vProp))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefVal)((ref Val v) =>
            {
                ++v.NullableProp;
            });

            var v1 = new Val { NullableProp = null };
            var v2 = new Val { NullableProp = 9 };
            @cs(ref v1);
            @cs(ref v2);
            Assert.AreEqual(null, v1.NullableProp);
            Assert.AreEqual(10, v2.NullableProp);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { NullableProp = null };
            v2 = new Val { NullableProp = 9 };
            fs(ref v1);
            fs(ref v2);
            Assert.AreEqual(null, v1.NullableProp);
            Assert.AreEqual(9, v2.NullableProp); // todo: @sys that System.Compile does not work with ref ValueType.Member Increment/Decrement operations

            var ff = e.CompileFast(true);
            ff.PrintIL();

            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Dup,
                OpCodes.Call,       // Val.get_NullableProp
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Call,       // System.Nullable`1<int32>::get_HasValue()
                OpCodes.Brfalse,    // jump to Pop(s) before the Ret op-code
                OpCodes.Ldloca_S,
                OpCodes.Ldfld,      // System.Nullable`1<int32>::GetValueOrDefault()
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Call,       // Val.set_NullableProp
                OpCodes.Br_S,       // jump to Ret op-code
                OpCodes.Pop,        // Pops the Dup-ped Val argument
                OpCodes.Ret
            );

            v1 = new Val { NullableProp = null };
            v2 = new Val { NullableProp = 9 };
            ff(ref v1);
            ff(ref v2);
            Assert.AreEqual(null, v1.NullableProp);
            Assert.AreEqual(10, v2.NullableProp);
        }

        delegate int? RefValReturningNullable(ref Val v);

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable_ReturningNullable()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vField = typeof(Val).GetField(nameof(Val.NullableField));
            var e = Lambda<RefValReturningNullable>(
                Block(PreIncrementAssign(Field(v, vField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefValReturningNullable)((ref Val v) =>
            {
                return ++v.NullableField;
            });

            var v1 = new Val { NullableField = null };
            var v2 = new Val { NullableField = 9 };
            var x1 = @cs(ref v1);
            var x2 = @cs(ref v2);
            Assert.AreEqual(null, v1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(10, v2.NullableField);
            Assert.AreEqual(10, x2);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { NullableField = null };
            v2 = new Val { NullableField = 9 };
            x1 = fs(ref v1);
            x2 = fs(ref v2);
            Assert.AreEqual(null, v1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(9, v2.NullableField); // todo: @sys that System.Compile does not work with ref ValueType.Member Increment/Decrement operations
            Assert.AreEqual(10, x2);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldflda,
                OpCodes.Dup,
                OpCodes.Ldobj,
                OpCodes.Stloc_0,    // we are using a single variable to store the field and to store the result
                OpCodes.Ldloca_S,   // 0
                OpCodes.Call,       // System.Nullable`1<int32>::get_HasValue()
                OpCodes.Brfalse,    // jump to Pop(s) before the Ret op-code
                OpCodes.Ldloca_S,   // 0
                OpCodes.Ldfld,      // System.Nullable`1<int32>::GetValueOrDefault()
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Stobj,      // Stores the nullable value to the address of the field
                OpCodes.Br_S,       // jump to Ret op-code
                OpCodes.Pop,        // Pops the Ldflda Dup-ped
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            v1 = new Val { NullableField = null };
            v2 = new Val { NullableField = 9 };
            x1 = ff(ref v1);
            x2 = ff(ref v2);
            Assert.AreEqual(null, v1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(10, v2.NullableField);
            Assert.AreEqual(10, x2);
        }

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Nullable_ReturningNullable_Prop()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vProp = typeof(Val).GetProperty(nameof(Val.NullableProp));
            var e = Lambda<RefValReturningNullable>(
                Block(PreIncrementAssign(Property(v, vProp))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefValReturningNullable)((ref Val v) =>
            {
                return ++v.NullableProp;
            });

            var v1 = new Val { NullableProp = null };
            var v2 = new Val { NullableProp = 9 };
            var x1 = @cs(ref v1);
            var x2 = @cs(ref v2);
            Assert.AreEqual(null, v1.NullableProp);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(10, v2.NullableProp);
            Assert.AreEqual(10, x2);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { NullableProp = null };
            v2 = new Val { NullableProp = 9 };
            x1 = fs(ref v1);
            x2 = fs(ref v2);
            Assert.AreEqual(null, v1.NullableProp);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(9, v2.NullableProp); // todo: @sys that System.Compile does not work with ref ValueType.Member Increment/Decrement operations
            Assert.AreEqual(10, x2);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Dup,
                OpCodes.Call,       // Val.get_NullableProp
                OpCodes.Stloc_0,    // we are using a single variable to store the field and to store the result
                OpCodes.Ldloca_S,   // 0
                OpCodes.Call,       // System.Nullable`1<int32>::get_HasValue()
                OpCodes.Brfalse,    // jump to Pop(s) before the Ret op-code
                OpCodes.Ldloca_S,   // 0
                OpCodes.Ldfld,      // System.Nullable`1<int32>::GetValueOrDefault()
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Call,       // Val.set_NullableProp
                OpCodes.Br_S,       // jump to Ret op-code
                OpCodes.Pop,        // Pops the Val arg Dupped
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            v1 = new Val { NullableProp = null };
            v2 = new Val { NullableProp = 9 };
            x1 = ff(ref v1);
            x2 = ff(ref v2);
            Assert.AreEqual(null, v1.NullableProp);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(10, v2.NullableProp);
            Assert.AreEqual(10, x2);
        }

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PostIncrementAssign_Nullable_ReturningNullable()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vField = typeof(Val).GetField(nameof(Val.NullableField));
            var e = Lambda<RefValReturningNullable>(
                Block(PostIncrementAssign(Field(v, vField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefValReturningNullable)((ref Val v) =>
            {
                return v.NullableField++;
            });

            var v1 = new Val { NullableField = null };
            var v2 = new Val { NullableField = 9 };
            var x1 = @cs(ref v1);
            var x2 = @cs(ref v2);
            Assert.AreEqual(null, v1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(10, v2.NullableField);
            Assert.AreEqual(9, x2);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { NullableField = null };
            v2 = new Val { NullableField = 9 };
            x1 = fs(ref v1);
            x2 = fs(ref v2);
            Assert.AreEqual(null, v1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(9, v2.NullableField); // todo: @sys that System.Compile does not work with ref ValueType.Member Increment/Decrement operations
            Assert.AreEqual(9, x2);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldflda,
                OpCodes.Dup,
                OpCodes.Ldobj,
                OpCodes.Stloc_0,    // we are using a single variable to store the field and to store the result
                OpCodes.Ldloca_S,   // 0
                OpCodes.Call,       // System.Nullable`1<int32>::get_HasValue()
                OpCodes.Brfalse,    // jump to Pop(s) before the Ret op-code
                OpCodes.Ldloca_S,   // 0
                OpCodes.Ldfld,      // System.Nullable`1<int32>::GetValueOrDefault()
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Stobj,      // Stores the nullable value to the address of the field
                OpCodes.Br_S,       // jump to Ret op-code
                OpCodes.Pop,        // Pops the Ldflda Dup-ped
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            v1 = new Val { NullableField = null };
            v2 = new Val { NullableField = 9 };
            x1 = ff(ref v1);
            x2 = ff(ref v2);
            Assert.AreEqual(null, v1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(10, v2.NullableField);
            Assert.AreEqual(9, x2);
        }

        delegate int RefValReturning(ref Val v);

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PreIncrementAssign_Returning()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vField = typeof(Val).GetField(nameof(Val.Field));
            var e = Lambda<RefValReturning>(
                Block(PreIncrementAssign(Field(v, vField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefValReturning)((ref Val v) =>
            {
                return ++v.Field;
            });

            var v1 = new Val { Field = 9 };
            var x1 = @cs(ref v1);
            Assert.AreEqual(10, v1.Field);
            Assert.AreEqual(10, x1);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { Field = 9 };
            x1 = fs(ref v1);
            Assert.AreEqual(9, v1.Field); // todo: @sys that System.Compile does not work with ref ValueType.Member Increment/Decrement operations
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

            v1 = new Val { Field = 9 };
            x1 = ff(ref v1);
            Assert.AreEqual(10, v1.Field);
            Assert.AreEqual(10, x1);
        }

        [Test]
        public void Check_Ref_ValueType_MemberAccess_PostIncrementAssign_Returning()
        {
            var v = Parameter(typeof(Val).MakeByRefType(), "v");
            var vField = typeof(Val).GetField(nameof(Val.Field));
            var e = Lambda<RefValReturning>(
                Block(PostIncrementAssign(Field(v, vField))),
                v
            );
            e.PrintCSharp();
            var @cs = (RefValReturning)((ref Val v) =>
            {
                return v.Field++;
            });

            var v1 = new Val { Field = 9 };
            var x1 = @cs(ref v1);
            Assert.AreEqual(10, v1.Field);
            Assert.AreEqual(9, x1);

            var fs = e.CompileSys();
            fs.PrintIL();

            v1 = new Val { Field = 9 };
            x1 = fs(ref v1);
            Assert.AreEqual(9, v1.Field); // todo: @sys that System.Compile does not work with ref ValueType.Member Increment/Decrement operations
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

            v1 = new Val { Field = 9 };
            x1 = ff(ref v1);
            Assert.AreEqual(10, v1.Field);
            Assert.AreEqual(9, x1);
        }

        [Test]
        public void Check_MemberAccess_PreIncrementAssign_Nullable()
        {
            var b = Parameter(typeof(Box), "b");
            var bField = typeof(Box).GetField(nameof(Box.NullableField));
            var e = Lambda<Action<Box>>(
                Block(typeof(void),
                    PreIncrementAssign(Field(b, bField))
                ),
                b
            );
            e.PrintCSharp();
            var @cs = (Action<Box>)((Box b) =>
            {
                ++b.NullableField;
            });
            var b1 = new Box { NullableField = null };
            var b2 = new Box { NullableField = 41 };
            @cs(b1);
            @cs(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(42, b2.NullableField);

            var fs = e.CompileSys();
            fs.PrintIL();

            b1 = new Box { NullableField = null };
            b2 = new Box { NullableField = 41 };
            fs(b1);
            fs(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(42, b2.NullableField);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Dup,
                OpCodes.Ldfld,
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Call,
                OpCodes.Brfalse,
                OpCodes.Ldloca_S,
                OpCodes.Ldfld,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Stfld,
                OpCodes.Br_S,
                OpCodes.Pop,
                OpCodes.Ret
            );

            b1 = new Box { NullableField = null };
            b2 = new Box { NullableField = 41 };
            ff(b1);
            ff(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(42, b2.NullableField);
        }

        [Test]
        public void Check_MemberAccess_PreIncrementAssign_Nullable_ReturningNullable()
        {
            var b = Parameter(typeof(Box), "b");
            var bField = typeof(Box).GetField(nameof(Box.NullableField));
            var e = Lambda<Func<Box, int?>>(
                Block(PreIncrementAssign(Field(b, bField))),
                b
            );
            e.PrintCSharp();
            var @cs = (Func<Box, int?>)((Box b) =>
            {
                return ++b.NullableField;
            });
            var b1 = new Box { NullableField = null };
            var b2 = new Box { NullableField = 41 };
            var x1 = @cs(b1);
            var x2 = @cs(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableField);
            Assert.AreEqual(42, x2);

            var fs = e.CompileSys();
            fs.PrintIL();
            /*
            */

            b1 = new Box { NullableField = null };
            b2 = new Box { NullableField = 41 };
            x1 = fs(b1);
            x2 = fs(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableField);
            Assert.AreEqual(42, x2);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Dup,
                OpCodes.Ldfld,
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Call,
                OpCodes.Brfalse,
                OpCodes.Ldloca_S,
                OpCodes.Ldfld,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Newobj,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Stfld,
                OpCodes.Br_S, // <-- jump to Ldloc_0 op-code
                OpCodes.Pop,
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            b1 = new Box { NullableField = null };
            b2 = new Box { NullableField = 41 };
            x1 = ff(b1);
            x2 = ff(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableField);
            Assert.AreEqual(42, x2);
        }

        [Test]
        public void Check_MemberAccess_PostIncrementAssign_Nullable_ReturningNullable()
        {
            var b = Parameter(typeof(Box), "b");
            var bField = typeof(Box).GetField(nameof(Box.NullableField));
            var e = Lambda<Func<Box, int?>>(
                Block(PostIncrementAssign(Field(b, bField))),
                b
            );
            e.PrintCSharp();
            var @cs = (Func<Box, int?>)((Box b) =>
            {
                return b.NullableField++;
            });
            var b1 = new Box { NullableField = null };
            var b2 = new Box { NullableField = 41 };
            var x1 = @cs(b1);
            var x2 = @cs(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableField);
            Assert.AreEqual(41, x2);

            var fs = e.CompileSys();
            fs.PrintIL();

            b1 = new Box { NullableField = null };
            b2 = new Box { NullableField = 41 };
            x1 = fs(b1);
            x2 = fs(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableField);
            Assert.AreEqual(41, x2);

            var ff = e.CompileFast(true);
            ff.PrintIL();

            b1 = new Box { NullableField = null };
            b2 = new Box { NullableField = 41 };
            x1 = ff(b1);
            x2 = ff(b2);
            Assert.AreEqual(null, b1.NullableField);
            Assert.AreEqual(null, x1);
            Assert.AreEqual(42, b2.NullableField);
            Assert.AreEqual(41, x2);
        }

        [Test]
        public void Check_MemberAccess_PreDecrementAssign_ToNewExpression()
        {
            Box.CtorCalls = 0; // assuming that the tests are not running in parallel
            var bCtor = typeof(Box).GetConstructor(new[] { typeof(int) });
            var bField = typeof(Box).GetField(nameof(Box.Field));

            var e = Lambda<Func<int>>(
                Block(
                    PreDecrementAssign(Field(New(bCtor, Constant(42)), bField))
                )
            );
            e.PrintCSharp();
            var @cs = (Func<int>)(() =>
            {
                return --(new Box(42)).Field;
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
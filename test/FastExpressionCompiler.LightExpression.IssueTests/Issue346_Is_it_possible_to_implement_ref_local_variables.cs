using System;
using NUnit.Framework;
using System.Reflection.Emit;
using static FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.LightExpression.IssueTests
{

    [TestFixture]
    public class Issue346_Is_it_possible_to_implement_ref_local_variables : ITest
    {
        public int Run()
        {
            Check_assignment_to_by_ref_float_parameter_PostIncrement_Returning();

            Check_assignment_to_by_ref_int_parameter_PostIncrement_Returning();
            Check_assignment_to_by_ref_int_parameter_PostIncrement_Void();
            Get_array_element_ref_and_member_change_and_Pre_increment_it();
            Get_array_element_ref_and_member_change_and_Post_increment_it();

            Real_world_test_ref_array_element();
            Get_array_element_ref_and_member_change_and_increment_it_then_method_call_on_ref_value_elem();
            Get_array_element_ref_and_member_change_and_increment_it();
            Get_array_element_ref_and_increment_it();
            Check_assignment_to_by_ref_int_parameter_PlusOne();

            return 10;
        }

        delegate void IncRefInt(ref int x);

        [Test]
        public void Check_assignment_to_by_ref_int_parameter_PlusOne()
        {
            var p = Parameter(typeof(int).MakeByRefType(), "x");
            var e = Lambda<IncRefInt>(
                Block(AddAssign(p, Constant(1))),
                p
            );
            e.PrintCSharp();
            var @cs = (IncRefInt)((ref int x) =>
            {
                x += (int)1;
            });

            var s = e.CompileSys();
            s.PrintIL();

            var x = 1;
            s(ref x);
            Asserts.AreEqual(2, x);

            var f = e.CompileFast(true);
            f.PrintIL();
            f.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_1,
                OpCodes.Ldind_I4,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stind_I4,
                OpCodes.Ret
            );

            var y = 1;
            f(ref y);
            Asserts.AreEqual(2, y);
        }

        [Test]
        public void Check_assignment_to_by_ref_int_parameter_PostIncrement_Void()
        {
            var p = Parameter(typeof(int).MakeByRefType(), "x");
            var e = Lambda<IncRefInt>(
                Block(PostIncrementAssign(p)),
                p);

            e.PrintCSharp();
            var @cs = (IncRefInt)((ref int x) =>
            {
                x++;
            });

            var s = e.CompileSys();
            s.PrintIL();

            var x = 1;
            s(ref x);
            Asserts.AreEqual(2, x);

            var f = e.CompileFast(true);
            f.PrintIL();
            f.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldind_I4,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stloc_0,
                OpCodes.Ldarg_1,
                OpCodes.Ldloc_0,
                OpCodes.Stind_I4,
                OpCodes.Ret
            );

            var y = 1;
            f(ref y);
            Asserts.AreEqual(2, y);
        }

        delegate float IncRefFloatReturning(ref float x);
        delegate int IncRefintReturning(ref int x);

        [Test]
        public void Check_assignment_to_by_ref_float_parameter_PostIncrement_Returning()
        {
            var p = Parameter(typeof(float).MakeByRefType(), "x");
            var e = Lambda<IncRefFloatReturning>(
                Block(PostIncrementAssign(p)),
                p);

            e.PrintCSharp();
            var @cs = (IncRefFloatReturning)((ref float x) =>
            {
                return x++;
            });

            var s = e.CompileSys();
            s.PrintIL();
            s.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldind_R4,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Ldc_R4,
                OpCodes.Add,
                OpCodes.Stloc_1,
                OpCodes.Ldarg_1,
                OpCodes.Ldloc_1,
                OpCodes.Stind_R4,
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            var x = 1.0f;
            var y = s(ref x);
            Asserts.AreEqual(2.0f, x);
            Asserts.AreEqual(1.0f, y);

            var f = e.CompileFast(true);
            f.PrintIL();
            // todo: @wip the IL codes is the same for the System Compile but the expected values are different
            // f.AssertOpCodes(
            // IL_0000: ldarg.1
            // IL_0001: ldarg.1
            // IL_0002: ldind.r4
            // IL_0003: stloc.0
            // IL_0004: ldloc.0
            // IL_0005: ldc.r4 1
            // IL_000a: add
            // IL_000b: stind.r4
            // IL_000c: ldloc.0
            // IL_000d: ret
            // );

            x = 1.0f;
            y = f(ref x);
            Asserts.AreEqual(1.0f, y);
            // Asserts.AreEqual(2.0f, x); // @wip inconsistent: 2.0f in Debug, and 1.0f in Release, but why?
        }

        [Test]
        public void Check_assignment_to_by_ref_int_parameter_PostIncrement_Returning()
        {
            var p = Parameter(typeof(int).MakeByRefType(), "x");
            var e = Lambda<IncRefintReturning>(
                Block(PostIncrementAssign(p)),
                p);

            e.PrintCSharp();
            var @cs = (IncRefintReturning)((ref int x) =>
            {
                return x++;
            });

            var s = e.CompileSys();
            s.PrintIL();

            var x = 1;
            var y = s(ref x);
            Asserts.AreEqual(2, x);
            Asserts.AreEqual(1, y);

            var f = e.CompileFast(true);
            f.PrintIL();
            f.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldind_I4,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stloc_1,
                OpCodes.Ldarg_1,
                OpCodes.Ldloc_1,
                OpCodes.Stind_I4,
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            x = 1;
            y = f(ref x);
            Asserts.AreEqual(1, y);
            Asserts.AreEqual(2, x);
        }

        [Test]
        public void Get_array_element_ref_and_increment_it()
        {
            var a = Parameter(typeof(int[]), "a");
            var n = Variable(typeof(int).MakeByRefType(), "n");
            var e = Lambda<Action<int[]>>(
                Block(new[] { n },
                    Assign(n, ArrayAccess(a, Constant(0))),
                    AddAssign(n, Constant(1))
                ),
                a
            );

            e.PrintCSharp();
            var @cs = (Action<int[]>)((int[] a) => //void
            {
                int n__discard_init_by_ref = default; ref var n = ref n__discard_init_by_ref;
                n = ref a[0];
                n += 1;
            });

            var array = new[] { 42 };
            @cs(array);
            Asserts.AreEqual(43, array[0]);

            var fs = e.CompileFast(true);
            fs.PrintIL();
            fs.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldc_I4_0,
                OpCodes.Ldelema,
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Ldind_I4,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stind_I4,
                OpCodes.Ret
            );

            array = new[] { 42 };
            fs(array);
            Asserts.AreEqual(43, array[0]);
        }

        [Test]
        public void Get_array_element_ref_and_member_change_and_increment_it()
        {
            var a = Variable(typeof(Vector3[]), "a");
            var i = Variable(typeof(int), "i");
            var vRef = Variable(typeof(Vector3).MakeByRefType(), "v");
            var bField = typeof(Vector3).GetField(nameof(Vector3.x));
            var e = Lambda<Func<Vector3[]>>(
                Block(
                    new[] { a, i, vRef },
                    Assign(a, NewArrayBounds(typeof(Vector3), ConstantInt(10))),
                    Assign(i, ConstantInt(0)),
                    Assign(vRef, ArrayAccess(a, i)),
                    AddAssign(Field(vRef, bField), Constant(12)),
                    a
                ));

            e.PrintCSharp();
            var @cs = (Func<Vector3[]>)(() =>
            {
                Vector3[] a = null;
                int i = default;
                Vector3 v__discard_init_by_ref = default; ref var v = ref v__discard_init_by_ref;
                a = new Vector3[10];
                i = 0;
                v = ref a[i];
                v.x += 12;
                return a;
            });
            // @cs.PrintIL();
            var vs = @cs();
            Asserts.AreEqual(12, vs[0].x);

            var fs = e.CompileFast(true);
            fs.PrintIL();
            fs.AssertOpCodes(
                OpCodes.Ldc_I4_S,// 10
                OpCodes.Newarr,  // C/Vector3
                OpCodes.Stloc_0,
                OpCodes.Ldc_I4_0,
                OpCodes.Stloc_1,
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_1,
                OpCodes.Ldelema, // C/Vector3
                OpCodes.Stloc_2,
                OpCodes.Ldloc_2,
                OpCodes.Ldflda,  // int64 C/Vector3::x
                OpCodes.Dup,
                OpCodes.Ldind_I4,
                OpCodes.Ldc_I4_S,// 12
                OpCodes.Add,
                OpCodes.Stind_I4,
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            vs = fs();
            Asserts.AreEqual(12, vs[0].x);
        }

        [Test]
        public void Get_array_element_ref_and_member_change_and_Post_increment_it()
        {
            var aPar = Parameter(typeof(Vector3[]), "aPar");
            var aVar = Variable(typeof(Vector3[]), "aVar");
            var i = Variable(typeof(int), "i");
            var vRef = Variable(typeof(Vector3).MakeByRefType(), "v");
            var bField = typeof(Vector3).GetField(nameof(Vector3.x));
            var e = Lambda<Func<Vector3[], int>>(
                Block(
                    new[] { aVar, i, vRef },
                    Assign(aVar, aPar),
                    Assign(i, ConstantInt(9)),
                    Assign(vRef, ArrayAccess(aVar, i)),
                    PostIncrementAssign(Field(vRef, bField))
                ),
                aPar);

            e.PrintCSharp();
            var @cs = (Func<Vector3[], int>)((Vector3[] aPar) =>
            {
                Vector3[] aVar = null;
                int i = default;
                Vector3 v__discard_init_by_ref = default; ref var v = ref v__discard_init_by_ref;
                aVar = aPar;
                i = 9;
                v = ref aVar[i];
                return v.x++;
            });
            var a = new Vector3[10];
            var x = @cs(a);
            Asserts.AreEqual(0, x);
            Asserts.AreEqual(1, a[9].x);

            var fs = e.CompileFast(true);
            fs.PrintIL();
            fs.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Stloc_0,
                OpCodes.Ldc_I4_S,// 9
                OpCodes.Stloc_1,
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_1,
                OpCodes.Ldelema,// Vector3
                OpCodes.Stloc_2,
                OpCodes.Ldloc_2,
                OpCodes.Ldflda, // Vector3.x
                OpCodes.Dup,
                OpCodes.Ldind_I4,
                OpCodes.Stloc_3,
                OpCodes.Ldloc_3,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stind_I4,
                OpCodes.Ldloc_3,
                OpCodes.Ret
            );
            a = new Vector3[10];
            x = fs(a);
            Asserts.AreEqual(0, x);
            Asserts.AreEqual(1, a[9].x);
        }

        [Test]
        public void Get_array_element_ref_and_member_change_and_Pre_increment_it()
        {
            var aPar = Parameter(typeof(Vector3[]), "aPar");
            var aVar = Variable(typeof(Vector3[]), "aVar");
            var i = Variable(typeof(int), "i");
            var vRef = Variable(typeof(Vector3).MakeByRefType(), "v");
            var bField = typeof(Vector3).GetField(nameof(Vector3.x));
            var e = Lambda<Func<Vector3[], int>>(
                Block(
                    new[] { aVar, i, vRef },
                    Assign(aVar, aPar),
                    Assign(i, ConstantInt(9)),
                    Assign(vRef, ArrayAccess(aVar, i)),
                    PreIncrementAssign(Field(vRef, bField))
                ),
                aPar);

            e.PrintCSharp();
            var @cs = (Func<Vector3[], int>)((Vector3[] aPar) =>
            {
                Vector3[] aVar = null;
                int i = default;
                Vector3 v__discard_init_by_ref = default; ref var v = ref v__discard_init_by_ref;
                aVar = aPar;
                i = 9;
                v = ref aVar[i];
                return ++v.x;
            });
            var a = new Vector3[10];
            var x = @cs(a);
            Asserts.AreEqual(1, x);
            Asserts.AreEqual(1, a[9].x);

            var fs = e.CompileFast(true);
            fs.PrintIL();
            fs.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Stloc_0,
                OpCodes.Ldc_I4_S,// 9
                OpCodes.Stloc_1,
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_1,
                OpCodes.Ldelema,// Vector3
                OpCodes.Stloc_2,
                OpCodes.Ldloc_2,
                OpCodes.Ldflda, // Vector3.x
                OpCodes.Dup,
                OpCodes.Ldind_I4,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stloc_3,
                OpCodes.Ldloc_3,
                OpCodes.Stind_I4,
                OpCodes.Ldloc_3,
                OpCodes.Ret
            );
            a = new Vector3[10];
            x = fs(a);
            Asserts.AreEqual(1, x);
            Asserts.AreEqual(1, a[9].x);
        }

        [Test]
        public void Get_array_element_ref_and_member_change_and_increment_it_then_method_call_on_ref_value_elem()
        {
            var a = Variable(typeof(Vector3[]), "a");
            var i = Variable(typeof(int), "i");
            var vRef = Variable(typeof(Vector3).MakeByRefType(), "v");
            var bField = typeof(Vector3).GetField(nameof(Vector3.x));
            var normalizeMethod = typeof(Vector3).GetMethod(nameof(Vector3.Normalize));
            var e = Lambda<Func<Vector3[]>>(
                Block(
                    new[] { a, i, vRef },
                    Assign(a, NewArrayBounds(typeof(Vector3), ConstantInt(10))),
                    Assign(i, ConstantInt(0)),
                    Assign(vRef, ArrayAccess(a, i)),
                    AddAssign(Field(vRef, bField), Constant(12)),
                    Call(vRef, normalizeMethod),
                    a
                ));

            e.PrintCSharp();
            var @cs = (Func<Vector3[]>)(() =>
            {
                Vector3[] a = null;
                int i = default;
                Vector3 v__discard_init_by_ref = default; ref var v = ref v__discard_init_by_ref;
                a = new Vector3[10];
                i = 0;
                v = ref a[i];
                v.x += 12;
                v.Normalize();
                return a;
            });
            // @cs.PrintIL();
            var vs = @cs();
            Asserts.AreEqual(53, vs[0].x);

            var fs = e.CompileFast(true);
            fs.PrintIL();
            fs.AssertOpCodes(
                OpCodes.Ldc_I4_S,// 10
                OpCodes.Newarr,  // C/Vector3
                OpCodes.Stloc_0,
                OpCodes.Ldc_I4_0,
                OpCodes.Stloc_1,
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_1,
                OpCodes.Ldelema, // C/Vector3
                OpCodes.Stloc_2,
                OpCodes.Ldloc_2,
                OpCodes.Ldflda,  // int64 C/Vector3::x
                OpCodes.Dup,
                OpCodes.Ldind_I4,
                OpCodes.Ldc_I4_S,// 12
                OpCodes.Add,
                OpCodes.Stind_I4,
                OpCodes.Ldloc_2,
                OpCodes.Call, // call Vector3.Normalize
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            vs = fs();
            Asserts.AreEqual(53, vs[0].x);
        }

        [Test]
        public void Real_world_test_ref_array_element()
        {
            // Vector3[] array = new Vector3[100]; // struct btw
            // for(int i = 0; i < array.Length; i++) {
            //     ref Vector3 v = ref array[i];
            //     // do stuff with v and have the array[i] value updated (because its a reference)
            //     v.x += 12;
            //     v.Normalize();
            // }

            var array = Variable(typeof(Vector3[]), "array");
            var index = Variable(typeof(int), "i");
            var refV = Variable(typeof(Vector3).MakeByRefType(), "v");

            var xField = typeof(Vector3).GetField("x");
            var normalizeMethod = typeof(Vector3).GetMethod("Normalize");

            var loopBreak = Label();

            var e = Lambda<Func<Vector3[]>>(
                Block(
                    new[] { array, index },
                    Assign(array, NewArrayBounds(typeof(Vector3), ConstantInt(100))),
                    Assign(index, ConstantInt(0)),
                    Loop(
                        IfThenElse(
                            LessThan(index, ArrayLength(array)),
                            Block(
                                new[] { refV },
                                Assign(refV, ArrayAccess(array, index)),
                                AddAssign(Field(refV, xField), ConstantInt(12)),
                                Call(refV, normalizeMethod),
                                PreIncrementAssign(index)
                            ),
                            Break(loopBreak)
                        ),
                        loopBreak
                    ),
                    array
                )
            );

            e.PrintCSharp();
            // verify the printed code is compiled:
            var @cs = (Func<Vector3[]>)(() =>
            {
                Vector3[] array = null;
                int i = default;
                array = new Vector3[100];
                i = 0;

                while (true)
                {
                    if (i < array.Length)
                    {
                        Vector3 v__discard_init_by_ref = default; ref var v = ref v__discard_init_by_ref;
                        v = ref array[i];
                        v.x += 12;
                        v.Normalize();
                        ++i;
                    }
                    else
                    {
                        goto void__54267293;
                    }
                }
            void__54267293:;

                return array;
            });
            var a = @cs();
            Asserts.AreEqual(100, a.Length);

            var f = e.CompileFast(true);
            f.PrintIL();
            f.AssertOpCodes(
                OpCodes.Ldc_I4_S,// 100
                OpCodes.Newarr, // Vector3
                OpCodes.Stloc_0,
                OpCodes.Ldc_I4_0,
                OpCodes.Stloc_1,
                OpCodes.Ldloc_1,
                OpCodes.Ldloc_0,
                OpCodes.Ldlen,
                OpCodes.Clt,
                OpCodes.Brfalse,// 55
                OpCodes.Ldloc_0,
                OpCodes.Ldloc_1,
                OpCodes.Ldelema,// Vector3
                OpCodes.Stloc_2,
                OpCodes.Ldloc_2,
                OpCodes.Ldflda,// Vector3.x
                OpCodes.Dup,
                OpCodes.Ldind_I4,
                OpCodes.Ldc_I4_S,// 12
                OpCodes.Add,
                OpCodes.Stind_I4,
                OpCodes.Ldloc_2,
                OpCodes.Call, //Vector3.Normalize
                OpCodes.Ldloc_1,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stloc_1,
                OpCodes.Br, //60
                OpCodes.Br, //65
                OpCodes.Br, //10
                OpCodes.Ldloc_0,
                OpCodes.Ret
            );

            a = f();
            Asserts.AreEqual(100, a.Length);
            Asserts.AreEqual(a[0].x, 53);
            Asserts.AreEqual(a[99].x, 53);
        }

        public struct Vector3
        {
            public int x, y, z;
            public Vector3(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
            public void Normalize() { x += 41; y += 42; z += 42; }
        }
    }
}

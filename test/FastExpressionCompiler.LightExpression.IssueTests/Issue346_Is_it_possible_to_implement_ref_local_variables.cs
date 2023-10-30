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
            Get_array_element_ref_and_member_change_and_increment_it();
            Get_array_element_ref_and_increment_it();
            // Real_world_test_ref_array_element();
            Check_assignment_to_by_ref_float_parameter_Increment();
            Check_assignment_to_by_ref_float_parameter_PlusOne();
            return 3;
        }

        delegate void IncRefFloat(ref float x);

        [Test]
        public void Check_assignment_to_by_ref_float_parameter_PlusOne()
        {
            var p = Parameter(typeof(float).MakeByRefType(), "x");
            var e = Lambda<IncRefFloat>(
                Block(AddAssign(p, Constant(1.0f))),
                p
            );
            e.PrintCSharp();
            var @cs = (IncRefFloat)((ref float x) =>
            {
                x += (float)1;
            });

            var s = e.CompileSys();
            s.PrintIL();

            var x = 1.0f;
            s(ref x);
            Assert.AreEqual(2, (int)x);

            var f = e.CompileFast(true);
            f.PrintIL();
            f.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_1,
                OpCodes.Ldind_R4,
                OpCodes.Ldc_R4,
                OpCodes.Add,
                OpCodes.Stind_R4,
                OpCodes.Ret
            );

            var y = 1.0f;
            f(ref y);
            Assert.AreEqual(2, (int)y);
        }

        [Test]
        public void Check_assignment_to_by_ref_float_parameter_Increment()
        {
            var p = Parameter(typeof(float).MakeByRefType(), "x");
            var e = Lambda<IncRefFloat>(
                Block(PostIncrementAssign(p)),
                p
            );
            e.PrintCSharp();
            var @cs = (IncRefFloat)((ref float x) =>
            {
                x++;
            });

            var s = e.CompileSys();
            s.PrintIL();

            var x = 1.0f;
            s(ref x);
            Assert.AreEqual(2, (int)x);

            var f = e.CompileFast(true);
            f.PrintIL();
            // f.AssertOpCodes(
            //     OpCodes.Ldarg_1,
            //     OpCodes.Ldarg_1,
            //     OpCodes.Ldind_R4,
            //     OpCodes.Ldc_I4_1,
            //     OpCodes.Add,
            //     OpCodes.Stind_R4,
            //     OpCodes.Ret
            // );

            var y = 1.0f;
            f(ref y);
            Assert.AreEqual(2, (int)y);
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
            var @cs = (Action<int[]>)((int[] a) =>
            {
                ref int n = ref a[0];
                n += 1;
            });
            var array = new[] { 42 };
            @cs(array);
            Assert.AreEqual(43, array[0]);

            var fs = e.CompileFast(true);
            fs.PrintIL();
            fs.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldc_I4_0,
                OpCodes.Ldelema,
                OpCodes.Dup,
                OpCodes.Ldind_I4,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stind_I4,
                OpCodes.Ret
            );

            array = new[] { 42 };
            fs(array);
            Assert.AreEqual(43, array[0]);
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
            Assert.AreEqual(12, vs[0].x);

            var fs = e.CompileFast(true);
            fs.PrintIL();
            // fs.AssertOpCodes(
                // OpCodes.Ldc_I4_0,
                // OpCodes.Stloc_0,
                // OpCodes.Ldloca_S,// 1
                // OpCodes.Initobj, // C/Vector3
                // OpCodes.Ldc_I4_S,// 10
                // OpCodes.Newarr,  // C/Vector3
                // OpCodes.Ldc_I4_0,
                // OpCodes.Stloc_0,
                // OpCodes.Dup,
                // OpCodes.Ldloc_0,
                // OpCodes.Ldelema, // C/Vector3
                // OpCodes.Ldflda,  // float64 C/Vector3::x
                // OpCodes.Dup,
                // OpCodes.Ldind_R8,
                // OpCodes.Ldc_R8,  // 12
                // OpCodes.Add,
                // OpCodes.Stind_R8,
                // OpCodes.Ret
            // );

            // vs = fs();
            // Assert.AreEqual(43, vs[0].x);
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
                        ref Vector3 v = ref array[i];
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
            Assert.AreEqual(100, a.Length);

            var f = e.CompileFast(true);
            f.PrintIL();
            a = f();
            Assert.AreEqual(100, a.Length);
        }

        public struct Vector3
        {
            public double x, y, z;
            public Vector3(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
            public void Normalize() { x += 41; y += 42; z += 42; }
        }
    }
}

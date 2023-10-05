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
            Check_assignment_to_by_ref_float_parameter_Increment();
            Check_assignment_to_by_ref_float_parameter_PlusOne();
            // SimpleTest();
            // Test();
            return 2;
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
            f.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldind_R4,
                OpCodes.Ldc_I4_1,
                OpCodes.Add,
                OpCodes.Stloc_0,
                OpCodes.Ldarg_1,
                OpCodes.Ldloc_0,
                OpCodes.Stind_R4,
                OpCodes.Ret
            );

            var y = 1.0f;
            f(ref y);
            Assert.AreEqual(2, (int)y);
        }

        [Test]
        public void SimpleTest()
        {
            // ref var n = ref array[0];
            var a = Parameter(typeof(int[]), "a");
            var n = Variable(typeof(int).MakeByRefType(), "n");
            var e = Lambda<Action<int[]>>(
                Block(new[] { n },
                    Assign(n, ArrayAccess(a, Constant(0))),
                    // PreIncrementAssign(n)
                    AddAssign(n, Constant(1))
                ),
                a
            );

            e.PrintCSharp();
            // var @cs = (Action<int[]>)((int[] a) =>
            // {
            //     ref int n = ref a[0];
            //     n += 1;
            // });

            var f = e.CompileFast(true);
            f.PrintIL();
            f.AssertOpCodes(
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

            var array = new[] { 42 };
            f(array);

            Assert.AreEqual(43, array[0]);
        }

        [Test]
        public void Test()
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
            // var ff = (Func<Issue346_Is_it_possible_to_implement_ref_local_variables.Vector3[]>)(() =>
            // {
            //     Issue346_Is_it_possible_to_implement_ref_local_variables.Vector3[] array;
            //     int i;
            //     array = new Issue346_Is_it_possible_to_implement_ref_local_variables.Vector3[100];
            //     i = 0;

            //     while (true)
            //     {
            //         if (i < array.Length)
            //         {
            //             ref Issue346_Is_it_possible_to_implement_ref_local_variables.Vector3 v = ref array[i];
            //             v.x += 12;
            //             v.Normalize();
            //             ++i;
            //         }
            //         else
            //         {
            //             goto void__43495525;
            //         }
            //     }
            //     void__43495525:;

            //     return array;
            // });

            var f = e.CompileFast(true);
            var vs = f();

            Assert.AreEqual(100, vs.Length);
        }

        public struct Vector3
        {
            public double x, y, z;
            public Vector3(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
            public void Normalize() { x += 41; y += 42; z += 42; }
        }
    }
}

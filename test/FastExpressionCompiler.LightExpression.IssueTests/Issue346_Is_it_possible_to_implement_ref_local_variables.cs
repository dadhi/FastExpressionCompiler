using System;
using NUnit.Framework;
using static FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.LightExpression.IssueTests
{

    [TestFixture]
    public class Issue346_Is_it_possible_to_implement_ref_local_variables : ITest
    {
        public int Run()
        {
            Test();
            return 1;
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

            // verify the code:
            // var ff = (Func<Issue346_Is_it_possible_to_implement_ref_local_variables.Vector3[]>)(() =>
            // {
            //     Issue346_Is_it_possible_to_implement_ref_local_variables.Vector3[] array;
            //     int i;
            //     array = new Issue346_Is_it_possible_to_implement_ref_local_variables.Vector3[100];
            //     i = 0; while (true)
            //     {
            //         if ((i < array.Length))
            //         {
            //             Issue346_Is_it_possible_to_implement_ref_local_variables.Vector3 v;
            //             v = array[i];
            //             v.x += 12;
            //             v.Normalize();
            //             (++i); // todo: @wip - this does not compile
            //         }
            //         else
            //         {
            //             goto void__54267293;
            //         }
            //     }
            // void__54267293:
            //     return array;
            // });

            var f = e.CompileFast(true);
            var vs = f();

            Assert.AreEqual(100, vs.Length);
        }

        public struct Vector3
        {
            public double x, y, z;
            Vector3(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
            public void Normalize() { x += 41; y += 42; z += 42; }
        }
    }
}

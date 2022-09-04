using System;
using NUnit.Framework;

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
            var newExample = Expression.New(typeof(Example));

            var e = Expression.Lambda<Func<Example>>(newExample);

            // todo: @wip convert to expression, ref assignment?

            // Vector3[] array = new Vector3[100]; // struct btw
            // for(int i = 0; i < array.Length; i++) {
            //     ref Vector3 v = ref array[i];
            //     // do stuff with v and have the array[i] value updated (because its a reference)
            //     v.x += 12;
            //     v.Normalize();
            // }

            var f = e.CompileFast(true);

            Assert.IsInstanceOf<Example>(f());
        }

        struct Example { }
    }
}

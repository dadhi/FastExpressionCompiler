using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using NUnit.Framework;
#pragma warning disable CS0164, CS0649

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue281_Index_Out_of_Range : ITest
    {
        public int Run()
        {
            Index_Out_of_Range();
            return 1;
        }

        [Test]
        public void Index_Out_of_Range()
        {
            var input = Parameter(typeof(List<string>));
            var idx = Parameter(typeof(int));

            var listIdxProp = typeof(List<string>).GetProperties().FirstOrDefault(x => x.GetIndexParameters().Any());
            var printMethod = typeof(object).GetMethod(nameof(ToString));

            var e = Lambda<Func<List<string>, int, string>>(
                Call(MakeIndex(input, listIdxProp, new[] { idx }), printMethod),
                input, idx);

            e.PrintCSharp();
            // var @cs = (Func<List<string>, int, string>)((
            //     List<string> list_string___32854180,
            //     int int__27252167) => //string
            //     list_string___32854180[int__27252167].ToString());

            var fs = e.CompileSys();
            fs.PrintIL();

            var m = new List<string> { "a" };
            Asserts.AreEqual("a", fs(m, 0));

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Callvirt, // List`1.get_Item
                OpCodes.Callvirt, // Object.ToString
                OpCodes.Ret
            );

            Asserts.AreEqual("a", ff(m, 0));
        }
    }
}
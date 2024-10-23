using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using System.Text;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue333_AccessViolationException_and_other_suspicious_behavior_on_invoking_result_of_CompileFast : ITest
    {
        public int Run()
        {
            Test();
            return 1;
        }

        private readonly S _foo = "foo";

        [Test]
        public void Test()
        {
            System.Linq.Expressions.Expression<Func<S>> sHoistedExpr = () => new Widget(null, null, null, _foo, null).Dodgy;
            var hoistedExpr = sHoistedExpr.FromSysExpression();

            var manualExpr = hoistedExpr.ToExpressionString();
            // var expr = Lambda<System.Func<S>>( //$
            // e[0]=Field(
            //     e[1]=New( // 5 args
            //     typeof(Widget).GetTypeInfo().DeclaredConstructors.ToArray()[0],
            //     e[2]=Constant(null, typeof(I)), 
            //     e[3]=Constant(null, typeof(I)), 
            //     e[4]=Constant(null, typeof(I)), 
            //     e[5]=Field(
            //         e[6]=Constant(default(Issue333_AccessViolationException_and_other_suspicious_behavior_on_invoking_result_of_CompileFast)/* (!) Please provide the non-default value for the constant */),
            //         typeof(Issue333_AccessViolationException_and_other_suspicious_behavior_on_invoking_result_of_CompileFast).GetTypeInfo().GetDeclaredField("_foo")), 
            //     e[7]=Convert(
            //         e[8]=Constant(null, typeof(string)),
            //         typeof(S),
            //         typeof(S).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "op_Implicit" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string) })))),
            //     typeof(Widget).GetTypeInfo().GetDeclaredField("Dodgy")), new ParameterExpression[0]);

            var code = hoistedExpr.ToCSharpString();
            // var f = (Func<S>)(() => //$
            //     new Widget(
            //         null,
            //         null,
            //         null,
            //         default(Issue333_AccessViolationException_and_other_suspicious_behavior_on_invoking_result_of_CompileFast)/* (!) Please provide the non-default value for the constant */._foo,
            //         ((S)null)).Dodgy);

            var fSys = hoistedExpr.CompileSys();
            fSys.PrintIL("sys");
            var x = fSys();
            Assert.AreEqual("foo", x.Value);

            var fFast = hoistedExpr.CompileFast(true);
            fFast.PrintIL("fast");
            var y = fFast();
            Assert.AreEqual("foo", y.Value);
        }

        private sealed class Widget
        {
            public readonly S Dodgy;

            public Widget(I a, I b, I c, S dodgy, S e)
            {
                Dodgy = dodgy; // throws
            }
        }

        private interface I { }

        private readonly struct S
        {
            public readonly bool HasValue;
            public readonly string Value;

            private S(string value)
            {
                HasValue = value is not null;
                Value = value;
            }

            public static implicit operator S(string s) => new(s);
        }
    }
}
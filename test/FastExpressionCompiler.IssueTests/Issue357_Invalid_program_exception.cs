#if !LIGHT_EXPRESSION
using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue357_Invalid_program_exception : ITest
    {
        public int Run()
        {
            Test1();
            return 1;
        }

        static Expression<Func<ActionItem, bool>> Predicate(Id<AccountManager>? value) => 
            x => x.AccountManagerId == value;

        [Test]
        public void Test1()
        {
            var e = Predicate(null);
            e.PrintCSharp();
            // var @cs = (Func<ActionItem, bool>)((ActionItem x) =>
            // x.AccountManagerId == ((Int64?)default(c__DisplayClass1_0)/*Please provide the non-default value for the constant!*/.value));

            e.PrintExpression();
            // var p = new ParameterExpression[1]; // the parameter expressions
            // var e = new Expression[5]; // the unique expressions
            // var expr = Lambda<Func<ActionItem, bool>>(
            //   e[0]=MakeBinary(ExpressionType.Equal,
            //       e[1]=Property(
            //           p[0]=Parameter(typeof(ActionItem), "x"),
            //           typeof(ActionItem).GetTypeInfo().GetDeclaredProperty("AccountManagerId")),
            //       e[2]=Convert(
            //           e[3]=Field(
            //               e[4]=Constant(default(c__DisplayClass1_0)/*Please provide the non-default value for the constant!*/),
            //               typeof(c__DisplayClass1_0).GetTypeInfo().GetDeclaredField("value")),
            //           typeof(Int64?),
            //           typeof(Id<AccountManager>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "op_Implicit" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Id<AccountManager>) })))),
            // p[0 // (ActionItem x)
            //       ]);

            var convertMethod = typeof(Id<AccountManager>).GetMethods()
                .Single(x => !x.IsGenericMethod && x.Name == "op_Implicit" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Id<AccountManager>) }));

            var actionItem = new ActionItem();

            var fs = e.CompileSys();
            fs.PrintIL();

            var xs = fs(actionItem);
            Assert.IsTrue(xs);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            var xf = ff(actionItem);
            Assert.IsTrue(xf);
        }

        public class AccountManager
        {
        }

        public class ActionItem
        {
            public long? AccountManagerId { get; set; }
        }

        public readonly struct Id<T>
        {
            private readonly long _value;

            public Id(long value)
            {
                _value = value;
            }

            public static implicit operator long?(Id<T> d) => d._value;
        }
    }
}
#endif

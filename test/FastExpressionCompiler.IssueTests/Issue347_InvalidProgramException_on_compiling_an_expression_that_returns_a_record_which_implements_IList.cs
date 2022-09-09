using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

#if LIGHT_EXPRESSION
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
    public class Issue347_InvalidProgramException_on_compiling_an_expression_that_returns_a_record_which_implements_IList : ITest
    {
        public int Run()
        {
            Test();
            return 1;
        }

        [Test]
        public void Test()
        {
            // var e = new Expression[3]; // the unique expressions
            // var expr = Lambda<Func<MyService_NullableIntArgWithIntValue>>( //$
            //   e[0] = New( // 2 args
            //     typeof(MyService_NullableIntArgWithIntValue).GetTypeInfo().DeclaredConstructors.ToArray()[0],
            //     e[1] = New( // 0 args
            //       typeof(MyOtherDependency).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]),
            //     e[2] = Constant(15, typeof(int?))));

            // var fSys = expr.CompileSys();
            // fSys.PrintIL("sys");
            // var x = fSys();
            // Assert.AreEqual(15, x.OptionalArgument);

            // var fFast = expr.CompileFast();
            // fFast.PrintIL("fast");
            // var y = fFast();
            // Assert.AreEqual(15, y.OptionalArgument);
        }

        // internal interface IReadOnlyListRecord<T> 
        // {
        //     int Count { get; }
        //     T this[int index] { get; }
        // }

        internal sealed record ReadOnlyListRecord<T> : IReadOnlyList<T>
        {
            public ReadOnlyListRecord(params T[] items) => Items = items.ToList().AsReadOnly();

            /// <summary>Gets or initializes the items in this collection.</summary>
            public IReadOnlyList<T> Items { get; }

            /// <inheritdoc />
            public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <inheritdoc />
            public int Count => Items.Count;

            /// <inheritdoc />
            public T this[int index] => Items[index];

            /// <inheritdoc />
            public override int GetHashCode()
            {
                int someHashValue = -234897289;
                foreach (var item in Items)
                    someHashValue = someHashValue ^ item.GetHashCode();
                return someHashValue;
            }

            /// <inheritdoc />
            public bool Equals(ReadOnlyListRecord<T> other)
            {
                // create a proper equality method...
                if (other == null || other.Count != Count)
                    return false;
                for (int i = 0; i < Count; i++)
                    if (!other[i].Equals(this[i]))
                        return false;
                return true;
            }
        }

        internal static class Extensions
        {
            public static IReadOnlyList<T> ToReadOnlyRecord<T>(IEnumerable<T> items) => new ReadOnlyListRecord<T>(items.ToArray());
        }
    }
}
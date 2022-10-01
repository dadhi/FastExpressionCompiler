using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
            // Expression<Func<NotifyContainer?, IReadOnlyListRecord<NotifyModel>>> e =
            //     x => x.Value.collectionA.Where(i => i.Number1 % 2 == 0 || x.Value.model.Number2 == 0).ToReadOnlyRecord();

            var p = new ParameterExpression[2]; // the parameter expressions
            var e = new Expression[16]; // the unique expressions
            var expr = Lambda<Func<NotifyContainer?, IReadOnlyListRecord<NotifyModel>>>(
            e[0]=Call(
                null,
                typeof(Extensions).GetMethods().Where(x => x.IsGenericMethod && x.Name == "ToReadOnlyRecord" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(NotifyModel)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(IEnumerable<NotifyModel>) })),
                e[1]=Call(
                    null,
                    typeof(Enumerable).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Where" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(NotifyModel)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(IEnumerable<NotifyModel>), typeof(Func<NotifyModel, bool>) })),
                    e[2]=Property(
                        e[3]=Property(
                            p[0]=Parameter(typeof(NotifyContainer?), "x"),
                            typeof(NotifyContainer?).GetTypeInfo().GetDeclaredProperty("Value")),
                        typeof(NotifyContainer).GetTypeInfo().GetDeclaredProperty("collectionA")),
                    e[4]=Lambda<Func<NotifyModel, bool>>(
                        e[5]=MakeBinary(ExpressionType.OrElse,
                            e[6]=MakeBinary(ExpressionType.Equal,
                                e[7]=MakeBinary(ExpressionType.Modulo,
                                    e[8]=Property(
                                        p[1]=Parameter(typeof(NotifyModel), "i"),
                                        typeof(NotifyModel).GetTypeInfo().GetDeclaredProperty("Number1")),
                                    e[9]=Constant(2)),
                                e[10]=Constant(0)),
                            e[11]=MakeBinary(ExpressionType.Equal,
                                e[12]=Property(
                                    e[13]=Property(
                                        e[14]=Property(
                                            p[0 // ([struct] NotifyContainer? x)
                                                ],
                                            typeof(NotifyContainer?).GetTypeInfo().GetDeclaredProperty("Value")),
                                        typeof(NotifyContainer).GetTypeInfo().GetDeclaredProperty("model")),
                                    typeof(NotifyModel).GetTypeInfo().GetDeclaredProperty("Number2")),
                                e[15]=Constant(0))),
                        p[1 // ([struct] NotifyModel i)
                            ]))),
            p[0 // ([struct] NotifyContainer? x)
                ]);

            // expr.PrintExpression();
            // expr.PrintCSharp();

            var func = (Func<NotifyContainer?, IReadOnlyListRecord<NotifyModel>>)((NotifyContainer? x) =>
            Extensions.ToReadOnlyRecord<NotifyModel>(Enumerable.Where<NotifyModel>(
                x.Value.collectionA,
                (Func<NotifyModel, bool>)((NotifyModel i) =>
                    (((i.Number1 % 2) == 0) || (x.Value.model.Number2 == 0))))));

            var model = new NotifyModel(42, 3);
            var container = new NotifyContainer(new List<NotifyModel> { model }, model);

            var fs = expr.CompileSys();
            var x = fs(container);
            Assert.AreEqual(1, x.Count);

            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
            var y = f(container);
            Assert.AreEqual(1, y.Count);
        }

        public record struct NotifyContainer(List<NotifyModel> collectionA, NotifyModel model);

        public record struct NotifyModel(int Number1, int Number2);
    }

    internal interface IReadOnlyListRecord<T> : IEnumerable<T>
    {
        int Count { get; }
        T this[int index] { get; }
    }

    internal sealed record ReadOnlyListRecord<T> : IReadOnlyListRecord<T>
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
        public static IReadOnlyListRecord<T> ToReadOnlyRecord<T>(this IEnumerable<T> items) => new ReadOnlyListRecord<T>(items.ToArray());
    }
}
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

#if !LIGHT_EXPRESSION
// using FastExpressionCompiler.LightExpression;
// using static FastExpressionCompiler.LightExpression.Expression;
// namespace FastExpressionCompiler.LightExpression.IssueTests
// #else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
// #endif
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
            Expression<Func<NotifyContainer?, IReadOnlyListRecord<NotifyModel>>> e =
                x => x.Value.collectionA.Where(i => i.Number1 % 2 == 0 || x.Value.model.Number2 == 0).ToReadOnlyRecord();

            e.PrintExpression();
            e.PrintCSharp();

            var model = new NotifyModel(42, 3);
            var container = new NotifyContainer(new List<NotifyModel> { model }, model);

            var fs = e.CompileSys();
            var x = fs(container);
            Assert.AreEqual(1, x.Count);

            var f = e.CompileFast(true);
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
#endif
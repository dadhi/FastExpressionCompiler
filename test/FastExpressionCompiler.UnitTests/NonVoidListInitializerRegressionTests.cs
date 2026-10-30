using System;
using System.Collections;
using System.Collections.Generic;
#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
using ElementInitializer = FastExpressionCompiler.LightExpression.ElementInit;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
using ElementInitializer = System.Linq.Expressions.ElementInit;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct NonVoidListInitializerRegressionTests : ITest
{
    public int Run()
    {
        Hashset_initializer_discards_boolean_add_results();
        Struct_initializer_discards_integer_add_results();
        Ignored_initializers_preserve_add_effects();
        Struct_initializer_result_supports_member_access();
        Alternating_add_overloads_keep_their_own_signatures();
        return 5;
    }
    public void Hashset_initializer_discards_boolean_add_results()
    {
        var expression = Expr.Lambda<Func<HashSet<int>>>(CreateInitializer(typeof(HashSet<int>), 1, 2, 4, 16, 42, 2));
        var fast = expression.CompileFast(true);
        Asserts.IsNotNull(fast);
        var result = fast();
        Asserts.AreEqual(5, result.Count);
        foreach (var value in new[] { 1, 2, 4, 16, 42 }) Asserts.IsTrue(result.Contains(value));
    }
    public void Struct_initializer_discards_integer_add_results()
    {
        var expression = Expr.Lambda<Func<ValueList>>(CreateInitializer(typeof(ValueList), 5, 6, 7, 8));
        var fast = expression.CompileFast(true);
        Asserts.IsNotNull(fast);
        Trace = "";
        var result = fast();
        Asserts.AreEqual("5;6;7;8;", Trace);
        Asserts.AreEqual(4, result.Count);
        Asserts.AreEqual(5, result[0]); Asserts.AreEqual(6, result[1]); Asserts.AreEqual(7, result[2]); Asserts.AreEqual(8, result[3]);
    }
    public void Ignored_initializers_preserve_add_effects()
    {
        foreach (var type in new[] { typeof(ReferenceList), typeof(ValueList) })
        {
            var body = CreateInitializer(type, 5, 6);
            var fast = Expr.Lambda<Action>(Expr.Block(body, Expr.Empty())).CompileFast(true);
            Asserts.IsNotNull(fast);
            Trace = "";
            fast();
            Asserts.AreEqual("5;6;", Trace);
        }
    }
    public void Struct_initializer_result_supports_member_access()
    {
        var body = CreateInitializer(typeof(ValueList), 5, 6);
        var fast = Expr.Lambda<Func<int>>(Expr.Property(body, nameof(ValueList.Count))).CompileFast(true);
        Asserts.IsNotNull(fast);
        Asserts.AreEqual(2, fast());
    }
    public void Alternating_add_overloads_keep_their_own_signatures()
    {
        var addInt = typeof(OverloadedList).GetMethod("Add", new[] { typeof(int) });
        var addString = typeof(OverloadedList).GetMethod("Add", new[] { typeof(string) });
        var body = Expr.ListInit(Expr.New(typeof(OverloadedList)),
            Expr.ElementInit(addInt, Expr.Constant(1)),
            Expr.ElementInit(addString, Expr.Constant("x")),
            Expr.ElementInit(addInt, Expr.Constant(2)));
        var fast = Expr.Lambda<Func<OverloadedList>>(body).CompileFast(true);
        Asserts.IsNotNull(fast);
        Trace = "";
        fast();
        Asserts.AreEqual("1;x;2;", Trace);
    }
    public class OverloadedList : IEnumerable<int>
    {
        public bool Add(int value) { Trace += value + ";"; return true; }
        public void Add(string value) { Trace += value + ";"; }
        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)Array.Empty<int>()).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private static Expr CreateInitializer(Type type, params int[] values)
    {
        var add = type.GetMethod("Add", new[] { typeof(int) });
        var initializers = new ElementInitializer[values.Length];
        for (var i = 0; i != values.Length; ++i) initializers[i] = Expr.ElementInit(add, Expr.Constant(values[i]));
        return Expr.ListInit(Expr.New(type), initializers);
    }
    public static string Trace;
    public struct ValueList : IEnumerable<int>
    {
        private List<int> _values;
        public int Add(int value) { Trace += value + ";"; (_values ??= new List<int>()).Add(value); return value; }
        public int Count => _values?.Count ?? 0;
        public int this[int index] => _values[index];
        public IEnumerator<int> GetEnumerator() => (_values ??= new List<int>()).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class ReferenceList : IEnumerable<int>
    {
        private readonly List<int> _values = new List<int>();
        public int Add(int value) { Trace += value + ";"; _values.Add(value); return value; }
        public IEnumerator<int> GetEnumerator() => _values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

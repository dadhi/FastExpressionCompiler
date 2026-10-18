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
public struct NonVoidListInitializerRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Hashset_initializer_discards_boolean_add_results(t);
        Struct_initializer_discards_integer_add_results(t);
        Ignored_initializers_preserve_add_effects(t);
        Struct_initializer_result_supports_member_access(t);
    }
    public void Hashset_initializer_discards_boolean_add_results(TestContext t)
    {
        var expression = Expr.Lambda<Func<HashSet<int>>>(CreateInitializer(typeof(HashSet<int>), 1, 2, 4, 16, 42, 2));
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var result = fast();
        t.AreEqual(5, result.Count);
        foreach (var value in new[] { 1, 2, 4, 16, 42 }) t.IsTrue(result.Contains(value));
    }
    public void Struct_initializer_discards_integer_add_results(TestContext t)
    {
        var expression = Expr.Lambda<Func<ValueList>>(CreateInitializer(typeof(ValueList), 5, 6, 7, 8));
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Trace = "";
        var result = fast();
        t.AreEqual("5;6;7;8;", Trace);
        t.AreEqual(4, result.Count);
        t.AreEqual(5, result[0]); t.AreEqual(6, result[1]); t.AreEqual(7, result[2]); t.AreEqual(8, result[3]);
    }
    public void Ignored_initializers_preserve_add_effects(TestContext t)
    {
        foreach (var type in new[] { typeof(ReferenceList), typeof(ValueList) })
        {
            var body = CreateInitializer(type, 5, 6);
            var fast = Expr.Lambda<Action>(Expr.Block(body, Expr.Empty())).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Trace = "";
            fast();
            t.AreEqual("5;6;", Trace);
        }
    }
    public void Struct_initializer_result_supports_member_access(TestContext t)
    {
        var body = CreateInitializer(typeof(ValueList), 5, 6);
        var fast = Expr.Lambda<Func<int>>(Expr.Property(body, nameof(ValueList.Count))).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(2, fast());
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
using Binding = FastExpressionCompiler.LightExpression.MemberBinding;
using Init = FastExpressionCompiler.LightExpression.ElementInit;
using Lambda = FastExpressionCompiler.LightExpression.LambdaExpression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
using Binding = System.Linq.Expressions.MemberBinding;
using Init = System.Linq.Expressions.ElementInit;
using Lambda = System.Linq.Expressions.LambdaExpression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct NestedBindingNullReceiversTests : ITestX
{
    public void Run(TestRun t)
    {
        Null_nested_receivers_evaluate_arguments_before_throwing(t);
    }

    public void Null_nested_receivers_evaluate_arguments_before_throwing(TestContext t)
    {
        Compare(t, Make(Expr.MemberBind(Member(typeof(Host), "NullNode"), Set(typeof(Node), "Y", 1))));
        Compare(t, Make(List(typeof(Host), "NullList", typeof(List<int>), false, 1, 2)));
    }

    public static int PropertyWrites;
    public struct PropertyValue { public int X; }
    public class PropertyOwner
    {
        private PropertyValue _value;
        public PropertyValue Value { get => _value; set { ++PropertyWrites; _value = value; } }
    }
    public struct PropertyStructOwner
    {
        private PropertyValue _value;
        public PropertyValue Value { get => _value; set { ++PropertyWrites; _value = value; } }
    }

    private static Binding Set(Type owner, string name, int value, bool spill = false) => Expr.Bind(Member(owner, name), Value(value, spill));
    private static Expr Value(int value, bool spill)
    {
        Expr body = Expr.Call(typeof(NestedBindingNullReceiversTests).GetMethod(nameof(Record)), Expr.Constant(value));
        return spill ? Expr.TryFinally(body, Expr.Call(typeof(NestedBindingNullReceiversTests).GetMethod(nameof(Finally)), Expr.Constant(value))) : body;
    }
    private static Binding List(Type owner, string name, Type collection, bool spill, params int[] values)
    {
        var add = collection.GetMethod("Add", new[] { typeof(int) });
        var initializers = new Init[values.Length];
        for (var i = 0; i != values.Length; ++i) initializers[i] = Expr.ElementInit(add, Value(values[i], spill));
        return Expr.ListBind(Member(owner, name), initializers);
    }
    private static MemberInfo Member(Type type, string name) => type.GetMember(name)[0];
    private static Lambda Make(params Binding[] bindings) => Expr.Lambda<Func<Host>>(Expr.MemberInit(Expr.New(typeof(Host)), bindings));
    private static System.Linq.Expressions.LambdaExpression ToSystem(Lambda expression)
    {
#if LIGHT_EXPRESSION
        return expression.ToLambdaExpression();
#else
        return expression;
#endif
    }
    private static void Compare(TestContext t, Lambda expression)
    {
        var system = ToSystem(expression).Compile();
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Trace = "";
        var expected = Invoke(system);
        var expectedTrace = Trace;
        Trace = "";
        var actual = Invoke(fast);
        t.AreEqual(expected, actual);
        t.AreEqual(expectedTrace, Trace);
    }
    private static object Invoke(Delegate function)
    {
        try
        {
            var result = function.DynamicInvoke();
            return result is Host host ? host.Snapshot() : result;
        }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }
    public static string Trace;
    public static int Record(int value) { Trace += "R" + value + ";"; return value; }
    public static void Finally(int value) => Trace += "F" + value + ";";
    public class Node
    {
        public int X;
        private int _y;
        public int Y { get => _y; set { Trace += "Y" + value + ";"; _y = value; } }
        public Node Child;
        public readonly List<int> Items = new List<int>();
        public string Snapshot() => X + ":" + Y + ":" + string.Join(",", Items) + ":" + Child?.Snapshot();
    }
    public struct StructNode
    {
        public int A;
        public Node Reference;
        public StructNode(bool initialize) { A = 0; Reference = new Node(); }
        public string Snapshot() => A + ":" + Reference.Snapshot();
    }
    public struct StructCollection : IEnumerable<int>
    {
        private List<int> _items;
        public int Add(int value) { Trace += "A" + value + ";"; (_items ??= new List<int>()).Add(value); return value; }
        public string Snapshot() => _items == null ? "" : string.Join(",", _items);
        public IEnumerator<int> GetEnumerator() => (_items ??= new List<int>()).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class Host
    {
        public int Value;
        public Node Field = new Node { Child = new Node() };
        public readonly Node ReadonlyField = new Node();
        public Node LastProperty;
        public Node P { get { Trace += "P;"; return LastProperty = new Node(); } set => throw new InvalidOperationException("setter must not run"); }
        public Node RO { get { Trace += "RO;"; return ReadonlyField; } }
        public Node NullNode { get { Trace += "NULL;"; return null; } }
        public List<int> ListField = new List<int>();
        public readonly List<int> ReadonlyList = new List<int>();
        public List<int> LastList;
        public List<int> ListProperty { get { Trace += "LP;"; return LastList = new List<int>(); } }
        public List<int> NullList { get { Trace += "NL;"; return null; } }
        public readonly HashSet<int> Set = new HashSet<int>();
        public StructNode Struct = new StructNode(true);
        public readonly StructNode ReadonlyStruct = new StructNode(true);
        public StructNode ValueProperty { get { Trace += "VP;"; return Struct; } }
        public StructCollection ValueList;
        public readonly StructCollection ReadonlyValueList;
        public StructCollection ValueListProperty { get { Trace += "VLP;"; return ValueList; } }
        public static Node StaticField = new Node();
        public static Node StaticPropertyTarget = new Node();
        public static Node StaticProperty { get { Trace += "SP;"; return StaticPropertyTarget; } }
        public static readonly Node StaticReadonlyField = new Node();
        public static Node StaticReadonlyProperty { get { Trace += "SRP;"; return StaticReadonlyField; } }
        public static readonly List<int> StaticList = new List<int>();
        public static void ResetStatic() { StaticField = new Node(); StaticPropertyTarget = new Node(); StaticReadonlyField.X = 0; StaticReadonlyField.Y = 0; }
        public Host() { Trace += "C;"; }
        public string Snapshot() => Value + "|" + Field.Snapshot() + "|" + ReadonlyField.Snapshot() + "|" + LastProperty?.Snapshot() + "|" + string.Join(",", ListField)
            + "|" + string.Join(",", ReadonlyList) + "|" + (LastList == null ? "" : string.Join(",", LastList)) + "|" + string.Join(",", Set)
            + "|" + Struct.Snapshot() + "|" + ReadonlyStruct.Snapshot() + "|" + ValueList.Snapshot() + "|" + ReadonlyValueList.Snapshot();
    }
}

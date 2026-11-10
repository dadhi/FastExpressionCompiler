using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct PropertyGetterReceiverRegressionTests : ITestX
{
    public void Run(TestRun t) { Interface_getters_use_struct_receiver_addresses(t); Discarded_getters_pop_results_and_keep_effects(t); Null_reference_getters_throw(t); Method_calls_share_receiver_dispatch(t); Indexers_share_receiver_dispatch(t); Collection_initializers_share_receiver_dispatch(t); }
    public interface IValue { int Value { get; } }
    public struct Cell : IValue { public int Number; public int Value { get { return ++Number; } } }
    public sealed class Owner { public int Calls; public int Value { get { ++Calls; return 17; } } public int Constant => 17; }
    public void Interface_getters_use_struct_receiver_addresses(TestContext t)
    {
        var value = S.Variable(typeof(Cell));
        var property = S.Property(value, typeof(IValue).GetProperty(nameof(IValue.Value)));
        var tree = S.Lambda<Func<int>>(S.Block(new[] { value }, S.Assign(value, S.Default(typeof(Cell))),
            S.Add(S.Multiply(property, S.Constant(10)), S.Field(value, nameof(Cell.Number)))));
        var expected = tree.Compile()();
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
        { var fast = tree.FromSysExpression().CompileFast(true, flags); if (t.IsNotNull(fast)) t.AreEqual(expected, fast()); }
    }
    public void Discarded_getters_pop_results_and_keep_effects(TestContext t)
    {
        var owner = S.Parameter(typeof(Owner));
        var tree = S.Lambda<Func<Owner, int>>(S.Block(S.Property(owner, nameof(Owner.Value)), S.Field(owner, nameof(Owner.Calls))), owner);
        var fast = tree.FromSysExpression().CompileFast(true); if (!t.IsNotNull(fast)) return;
        t.AreEqual(tree.Compile()(new Owner()), fast(new Owner()));
    }
    public void Null_reference_getters_throw(TestContext t)
    {
        var owner = S.Parameter(typeof(Owner));
        var tree = S.Lambda<Func<Owner, int>>(S.Property(owner, nameof(Owner.Constant)), owner);
        t.Throws<NullReferenceException>(() => tree.Compile()(null));
        var fast = tree.FromSysExpression().CompileFast(true); if (t.IsNotNull(fast)) t.Throws<NullReferenceException>(() => fast(null));
    }

    public interface IIndexed { int this[int index] { get; set; } }
    public interface IAdder { void Add(int value); }
    public struct Collection : IIndexed, IAdder, System.Collections.IEnumerable
    {
        public int Number;
        public int this[int index] { get { return ++Number + index; } set { Number = value + index; } }
        public void Add(int value) { Number += value; }
        public System.Collections.IEnumerator GetEnumerator() => new int[0].GetEnumerator();
        public override string ToString() => "collection";
    }
    public sealed class Bag : System.Collections.IEnumerable
    {
        public void Add(int value) { }
        public int this[int index] { get => 17; set { } }
        public System.Collections.IEnumerator GetEnumerator() => new int[0].GetEnumerator();
    }
    public sealed class BagOwner { public Bag Items => null; }
    public void Method_calls_share_receiver_dispatch(TestContext t)
    {
        var value = S.Variable(typeof(Collection));
        var add = S.Call(value, typeof(IAdder).GetMethod(nameof(IAdder.Add)), S.Constant(7));
        var tree = S.Lambda<Func<int>>(S.Block(new[] { value }, S.Assign(value, S.Default(typeof(Collection))), add, S.Field(value, nameof(Collection.Number))));
        var fast = tree.FromSysExpression().CompileFast(true);
        if (t.IsNotNull(fast)) t.AreEqual(tree.Compile()(), fast());
        var text = S.Lambda<Func<string>>(S.Call(S.Default(typeof(Collection)), typeof(object).GetMethod(nameof(ToString))));
        var fastText = text.FromSysExpression().CompileFast(true);
        if (t.IsNotNull(fastText)) t.AreEqual(text.Compile()(), fastText());
        var bag = S.Parameter(typeof(Bag));
        var nullCall = S.Lambda<Action<Bag>>(S.Call(bag, typeof(Bag).GetMethod(nameof(Bag.Add)), S.Constant(1)), bag);
        t.Throws<NullReferenceException>(() => nullCall.Compile()(null));
        var fastNull = nullCall.FromSysExpression().CompileFast(true);
        if (t.IsNotNull(fastNull)) t.Throws<NullReferenceException>(() => fastNull(null));
    }
    public void Indexers_share_receiver_dispatch(TestContext t)
    {
        var value = S.Variable(typeof(Collection));
        var index = S.MakeIndex(value, typeof(IIndexed).GetProperty("Item"), new[] { S.Constant(2) });
        var read = S.Lambda<Func<int>>(S.Block(new[] { value }, S.Assign(value, S.Default(typeof(Collection))),
            S.Add(S.Multiply(index, S.Constant(10)), S.Field(value, nameof(Collection.Number)))));
        var fastRead = read.FromSysExpression().CompileFast(true);
        if (t.IsNotNull(fastRead)) t.AreEqual(read.Compile()(), fastRead());
        var discarded = S.Lambda<Func<int>>(S.Block(new[] { value }, S.Assign(value, S.Default(typeof(Collection))), index, S.Field(value, nameof(Collection.Number))));
        var fastDiscarded = discarded.FromSysExpression().CompileFast(true);
        if (t.IsNotNull(fastDiscarded)) t.AreEqual(discarded.Compile()(), fastDiscarded());
        var bag = S.Parameter(typeof(Bag));
        var referenceIndex = S.MakeIndex(bag, typeof(Bag).GetProperty("Item"), new[] { S.Constant(2) });
        var tree = S.Lambda<Func<Bag, int>>(referenceIndex, bag);
        t.Throws<NullReferenceException>(() => tree.Compile()(null));
        var fast = tree.FromSysExpression().CompileFast(true);
        if (t.IsNotNull(fast)) t.Throws<NullReferenceException>(() => fast(null));
    }
    public void Collection_initializers_share_receiver_dispatch(TestContext t)
    {
        var initializer = S.ListInit(S.New(typeof(Collection)), S.ElementInit(typeof(IAdder).GetMethod(nameof(IAdder.Add)), S.Constant(7)));
        var tree = S.Lambda<Func<int>>(S.Field(initializer, nameof(Collection.Number)));
        var nativeInitializer = E.ListInit(E.New(typeof(Collection)), E.ElementInit(typeof(IAdder).GetMethod(nameof(IAdder.Add)), E.Constant(7)));
        var fast = E.Lambda<Func<int>>(E.Field(nativeInitializer, nameof(Collection.Number))).CompileFast(true);
        if (t.IsNotNull(fast)) t.AreEqual(tree.Compile()(), fast());
        var nullItems = S.Lambda<Func<BagOwner>>(S.MemberInit(S.New(typeof(BagOwner)),
            S.ListBind(typeof(BagOwner).GetProperty(nameof(BagOwner.Items)), S.ElementInit(typeof(Bag).GetMethod(nameof(Bag.Add)), S.Constant(1)))));
        t.Throws<NullReferenceException>(() => nullItems.Compile()());
        var fastNull = E.Lambda<Func<BagOwner>>(E.MemberInit(E.New(typeof(BagOwner)),
            E.ListBind(typeof(BagOwner).GetProperty(nameof(BagOwner.Items)), E.ElementInit(typeof(Bag).GetMethod(nameof(Bag.Add)), E.Constant(1))))).CompileFast(true);
        if (t.IsNotNull(fastNull)) t.Throws<NullReferenceException>(() => fastNull());
    }

}

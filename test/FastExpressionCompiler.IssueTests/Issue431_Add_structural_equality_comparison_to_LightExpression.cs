using System;
using System.Linq;
using System.Reflection;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
using FastExpressionCompiler.LightExpression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif


#if LIGHT_EXPRESSION
public struct Issue431_Add_structural_equality_comparison_to_LightExpression : ITestX
{
    public static readonly ConstructorInfo CtorOfA = typeof(A).GetTypeInfo().DeclaredConstructors.First();
    public static readonly ConstructorInfo CtorOfB = typeof(B).GetTypeInfo().DeclaredConstructors.First();
    public static readonly PropertyInfo PropAProp = typeof(A).GetTypeInfo().DeclaredProperties.First(p => p.Name == "Prop");

    public void Run(TestRun t)
    {
        Eq_simple_lambda(t);
        Eq_lambda_with_parameters(t);
        Eq_constants(t);
        Eq_member_access(t);
        Eq_method_call(t);
        Eq_new_expression(t);
        Eq_member_init(t);
        Eq_new_array(t);
        Eq_conditional(t);
        Eq_block_with_variables(t);
        Eq_try_catch(t);
        Eq_loop_with_labels(t);
        Eq_switch(t);
        Eq_complex_lambda_round_trip(t);
        NotEq_different_constants(t);
        NotEq_different_types(t);
        NotEq_different_parameters(t);
    }

    public void Eq_simple_lambda(TestContext t)
    {
        var e1 = Lambda<Func<int>>(Constant(42));
        var e2 = Lambda<Func<int>>(Constant(42));
        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_lambda_with_parameters(TestContext t)
    {
        var p1a = Parameter(typeof(int), "x");
        var p1b = Parameter(typeof(int), "y");
        var e1 = Lambda<Func<int, int, int>>(Add(p1a, p1b), p1a, p1b);

        var p2a = Parameter(typeof(int), "x");
        var p2b = Parameter(typeof(int), "y");
        var e2 = Lambda<Func<int, int, int>>(Add(p2a, p2b), p2a, p2b);

        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_constants(TestContext t)
    {
        t.IsTrue(Constant(42).EqualsTo(Constant(42)));
        t.IsTrue(Constant("hello").EqualsTo(Constant("hello")));
        t.IsTrue(Constant(null, typeof(string)).EqualsTo(Constant(null, typeof(string))));
    }

    public void Eq_member_access(TestContext t)
    {
        var prop = typeof(string).GetProperty(nameof(string.Length));
        var p1 = Parameter(typeof(string), "s");
        var p2 = Parameter(typeof(string), "s");
        var e1 = Lambda<Func<string, int>>(Property(p1, prop), p1);
        var e2 = Lambda<Func<string, int>>(Property(p2, prop), p2);
        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_method_call(TestContext t)
    {
        var method = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) });
        var p1 = Parameter(typeof(string), "a");
        var p2 = Parameter(typeof(string), "b");
        var pa = Parameter(typeof(string), "a");
        var pb = Parameter(typeof(string), "b");
        var e1 = Lambda<Func<string, string, string>>(Call(method, p1, p2), p1, p2);
        var e2 = Lambda<Func<string, string, string>>(Call(method, pa, pb), pa, pb);
        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_new_expression(TestContext t)
    {
        var ctor = typeof(B).GetConstructor(Type.EmptyTypes);
        var e1 = New(ctor);
        var e2 = New(ctor);
        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_member_init(TestContext t)
    {
        var e1 = MemberInit(New(CtorOfA, New(CtorOfB)), Bind(PropAProp, New(CtorOfB)));
        var e2 = MemberInit(New(CtorOfA, New(CtorOfB)), Bind(PropAProp, New(CtorOfB)));
        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_new_array(TestContext t)
    {
        var e1 = NewArrayInit(typeof(int), Constant(1), Constant(2), Constant(3));
        var e2 = NewArrayInit(typeof(int), Constant(1), Constant(2), Constant(3));
        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_conditional(TestContext t)
    {
        var p1 = Parameter(typeof(int), "x");
        var p2 = Parameter(typeof(int), "x");
        var e1 = Lambda<Func<int, int>>(Condition(Equal(p1, Constant(0)), Constant(1), p1), p1);
        var e2 = Lambda<Func<int, int>>(Condition(Equal(p2, Constant(0)), Constant(1), p2), p2);
        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_block_with_variables(TestContext t)
    {
        var v1 = Variable(typeof(int), "i");
        var v2 = Variable(typeof(int), "i");
        var e1 = Block(new[] { v1 }, Assign(v1, Constant(5)), v1);
        var e2 = Block(new[] { v2 }, Assign(v2, Constant(5)), v2);
        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_try_catch(TestContext t)
    {
        var ex1 = Parameter(typeof(Exception), "ex");
        var ex2 = Parameter(typeof(Exception), "ex");
        var e1 = TryCatch(Constant(1), Catch(ex1, Constant(2)));
        var e2 = TryCatch(Constant(1), Catch(ex2, Constant(2)));
        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_loop_with_labels(TestContext t)
    {
        var brk1 = Label(typeof(void), "break");
        var cnt1 = Label(typeof(void), "continue");
        var brk2 = Label(typeof(void), "break");
        var cnt2 = Label(typeof(void), "continue");
        var e1 = Loop(Block(Break(brk1), Continue(cnt1)), brk1, cnt1);
        var e2 = Loop(Block(Break(brk2), Continue(cnt2)), brk2, cnt2);
        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_switch(TestContext t)
    {
        var p1 = Parameter(typeof(int), "x");
        var p2 = Parameter(typeof(int), "x");
        var e1 = Lambda<Func<int, int>>(
            Switch(p1, Constant(-1), SwitchCase(Constant(10), Constant(1)), SwitchCase(Constant(20), Constant(2))),
            p1);
        var e2 = Lambda<Func<int, int>>(
            Switch(p2, Constant(-1), SwitchCase(Constant(10), Constant(1)), SwitchCase(Constant(20), Constant(2))),
            p2);
        t.IsTrue(e1.EqualsTo(e2));
    }

    public void Eq_complex_lambda_round_trip(TestContext t)
    {
        var expr = Lambda<Func<object[], object>>(
            MemberInit(
                New(CtorOfA, New(CtorOfB)),
                Bind(PropAProp, New(CtorOfB))),
            ParameterOf<object[]>("p"));

        var sysExpr = expr.ToLambdaExpression();
        var restoredExpr = sysExpr.ToLightExpression<Func<object[], object>>();

        t.IsTrue(expr.EqualsTo(restoredExpr));
    }

    public void NotEq_different_constants(TestContext t)
    {
        t.IsFalse(Constant(42).EqualsTo(Constant(43)));
        t.IsFalse(Constant("a").EqualsTo(Constant("b")));
    }

    public void NotEq_different_types(TestContext t)
    {
        t.IsFalse(Constant(42).EqualsTo(Constant(42L)));
        t.IsFalse(Default(typeof(int)).EqualsTo(Default(typeof(long))));
    }

    public void NotEq_different_parameters(TestContext t)
    {
        var p1 = Parameter(typeof(int), "x");
        var p2 = Parameter(typeof(int), "y");
        var e1 = Lambda<Func<int, int>>(p1, p1);
        var e2 = Lambda<Func<int, int>>(p2, p2);
        // When mapped by position in a lambda, different-named params ARE equal structurally (same position)
        t.IsTrue(e1.EqualsTo(e2));
        // But accessing a param outside its lambda context uses name comparison
        t.IsFalse(p1.EqualsTo(p2));
    }

    public class A
    {
        public B Prop { get; set; }
        public A(B b) { Prop = b; }
    }

    public class B { }
}
#endif

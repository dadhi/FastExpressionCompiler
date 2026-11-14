using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct UnaryParameterAssignmentTests : ITestX
{
    public struct Number
    {
        public int Value;
        public static Number operator ++(Number n)=>new Number{Value=n.Value+1};
        public static Number operator --(Number n)=>new Number{Value=n.Value-1};
    }
    public static Number Step(Number n)=>new Number{Value=n.Value+10};
    public static long AddWide(int left, int right) => left + right;
    public static int? NullableStep(int? n)=>(n??7)+10;
    public delegate Number RefUpdate(ref Number value);
    public void Run(TestRun t) { Parameter_and_local_results_match_system(t); Lifted_and_nonlifted_nullable_methods(t); Ref_and_captured_parameters_keep_existing_storage(t); Binary_compound_assignment_retains_rhs_and_conversion(t); }
    public void Parameter_and_local_results_match_system(TestContext t)
    {
        foreach(var local in new[]{false,true})
        foreach(var post in new[]{false,true})
        foreach(var decrement in new[]{false,true})
        foreach(var discard in new[]{false,true})
        foreach(var selected in new[]{false,true})
        {
            var parameter=S.Parameter(typeof(Number));var value=local?S.Variable(typeof(Number)):parameter;
            var op=decrement?(post?System.Linq.Expressions.ExpressionType.PostDecrementAssign:System.Linq.Expressions.ExpressionType.PreDecrementAssign)
                :(post?System.Linq.Expressions.ExpressionType.PostIncrementAssign:System.Linq.Expressions.ExpressionType.PreIncrementAssign);
            var assignment=S.MakeUnary(op,value,null,selected?typeof(UnaryParameterAssignmentTests).GetMethod(nameof(Step)):null);
            var body=discard?(System.Linq.Expressions.Expression)S.Block(assignment,S.Field(value,nameof(Number.Value))):S.Field(assignment,nameof(Number.Value));
            if(local)body=S.Block(new[]{value},S.Assign(value,parameter),body);
            var tree=S.Lambda<Func<Number,int>>(body,parameter);var fast=tree.FromSysExpression().CompileFast(true);
            if(!t.IsNotNull(fast))return;
            var input=new Number{Value=5};t.AreEqual(tree.Compile()(input),fast(input));
        }
    }
    public void Lifted_and_nonlifted_nullable_methods(TestContext t)
    {
        var number=S.Parameter(typeof(Number?));
        var lifted=S.Lambda<Func<Number?,Number?>>(S.PreIncrementAssign(number),number);
        var fast=lifted.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;
        t.AreEqual((Number?)null,fast(null));t.AreEqual(6,fast(new Number{Value=5}).Value.Value);
        var value=S.Parameter(typeof(int?));
        var explicitMethod=S.Lambda<Func<int?,int?>>(S.PreIncrementAssign(value,typeof(UnaryParameterAssignmentTests).GetMethod(nameof(NullableStep))),value);
        var direct=explicitMethod.FromSysExpression().CompileFast(true);if(!t.IsNotNull(direct))return;
        t.AreEqual(explicitMethod.Compile()(null),direct(null));t.AreEqual((int?)17,direct(null));
    }
    public void Binary_compound_assignment_retains_rhs_and_conversion(TestContext t)
    {
        foreach (var local in new[] { false, true })
        foreach (var discard in new[] { false, true })
        {
            var parameter = S.Parameter(typeof(int));
            var target = local ? S.Variable(typeof(int)) : parameter;
            var trace = "";
            Func<int> right = () => { trace += "rhs;"; return 3; };
            Func<long, int> convert = value => { trace += "convert;"; return (int)value * 2; };
            var converted = S.Parameter(typeof(long));
            var conversion = S.Lambda(S.Invoke(S.Constant(convert), converted), converted);
            // Capture the old LHS before this RHS mutates its storage; then apply the conversion once.
            var rhs = S.Block(S.Assign(target, S.Constant(100)), S.Invoke(S.Constant(right)));
            var assignment = S.AddAssign(target, rhs, typeof(UnaryParameterAssignmentTests).GetMethod(nameof(AddWide)), conversion);
            var body = discard
                ? (System.Linq.Expressions.Expression)S.Block(assignment, target)
                : assignment;
            if (local) body = S.Block(new[] { target }, S.Assign(target, parameter), body);
            var tree = S.Lambda<Func<int, int>>(body, parameter);
            var fast = tree.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var input = 5;
            trace = "";
            var expected = tree.Compile()(input);
            var expectedTrace = trace;
            trace = "";
            t.AreEqual(expected, fast(input));
            t.AreEqual(16, expected);
            t.AreEqual(expectedTrace, trace);
            t.AreEqual("rhs;convert;", trace);
        }
    }
    public void Ref_and_captured_parameters_keep_existing_storage(TestContext t)
    {
        var reference=S.Parameter(typeof(Number).MakeByRefType());
        var tree=S.Lambda<RefUpdate>(S.PostIncrementAssign(reference),reference);
        var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;
        var value=new Number{Value=5};t.AreEqual(5,fast(ref value).Value);t.AreEqual(6,value.Value);
        var captured=S.Parameter(typeof(Number));
        var factory=S.Lambda<Func<Number,Func<Number>>>(S.Lambda<Func<Number>>(S.PreIncrementAssign(captured)),captured).FromSysExpression().CompileFast(true);
        if(!t.IsNotNull(factory))return;
        var next=factory(new Number{Value=5});t.AreEqual(6,next().Value);t.AreEqual(7,next().Value);
    }
}

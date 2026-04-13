// FlatExpression is only in the FastExpressionCompiler assembly, not the LightExpression variant.
#if !LIGHT_EXPRESSION
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using FastExpressionCompiler.FlatExpression;

namespace FastExpressionCompiler.UnitTests;

public class FlatExpressionTests : ITest
{
    public int Run()
    {
        Idx_default_is_nil();
        Idx_of_is_one_based();

        Build_constant_node_inline();
        Build_constant_node_in_closure();
        Build_parameter_node();

        Build_add_two_constants();
        Build_lambda_int_identity();
        Build_lambda_add_two_params();
        Build_new_expression();
        Build_call_static_method();
        Build_conditional();
        Build_block_with_variable();

        Structural_equality_same_trees();
        Structural_equality_different_trees();

        Convert_to_system_expression_constant_lambda();
        Convert_to_system_expression_add_lambda();
        Convert_to_system_expression_new_lambda();

        Dump_does_not_throw();

        Roundtrip_lambda_identity_compile_and_invoke();
        Roundtrip_lambda_add_compile_and_invoke();

        // Closure constants can be swapped after tree construction without rebuilding.
        Closure_constant_is_mutable_after_build();

        return 22;
    }

    public void Idx_default_is_nil()
    {
        var idx = default(Idx);
        Asserts.IsTrue(idx.IsNil);
        Asserts.AreEqual(0, idx.It);
        Asserts.AreEqual(Idx.Nil, idx);
    }

    public void Idx_of_is_one_based()
    {
        var idx = Idx.Of(3);
        Asserts.IsFalse(idx.IsNil);
        Asserts.AreEqual(3, idx.It);
    }

    public void Build_constant_node_inline()
    {
        var tree = default(ExpressionTree);
        var ci = tree.Constant(42);

        Asserts.AreEqual(1, tree.NodeCount);
        Asserts.IsFalse(ci.IsNil);

        ref var node = ref tree.NodeAt(ci);
        Asserts.AreEqual(ExpressionType.Constant, node.NodeType);
        Asserts.AreEqual(typeof(int), node.Type);
        Asserts.AreEqual(null, node.Info);
        Asserts.AreEqual(-1, node.ExtraIdx.It);   // inline bits sentinel
        Asserts.AreEqual(42, node.ChildIdx.It);   // inline int32 bits
    }

    public void Build_constant_node_in_closure()
    {
        var tree = default(ExpressionTree);
        var ci = tree.Constant("hello", putIntoClosure: true);

        ref var node = ref tree.NodeAt(ci);
        Asserts.AreEqual(1, node.ExtraIdx.It);   // 1-based closure index
        Asserts.AreEqual(1, tree.ClosureConstants.Count);
        Asserts.AreEqual("hello", (string)tree.ClosureConstants.GetSurePresentRef(0));
    }

    public void Build_parameter_node()
    {
        var tree = default(ExpressionTree);
        var pi = tree.Parameter(typeof(int), "x");

        ref var node = ref tree.NodeAt(pi);
        Asserts.AreEqual(ExpressionType.Parameter, node.NodeType);
        Asserts.AreEqual(typeof(int), node.Type);
        Asserts.AreEqual("x", (string)node.Info);
    }

    public void Build_add_two_constants()
    {
        var tree = default(ExpressionTree);
        var a = tree.Constant(10);
        var b = tree.Constant(20);
        var add = tree.Add(a, b, typeof(int));

        Asserts.AreEqual(3, tree.NodeCount);
        ref var node = ref tree.NodeAt(add);
        Asserts.AreEqual(ExpressionType.Add, node.NodeType);
        Asserts.AreEqual(a, node.ChildIdx);
        Asserts.AreEqual(b, node.ExtraIdx);
    }

    public void Build_lambda_int_identity()
    {
        var tree = default(ExpressionTree);
        var p = tree.Parameter(typeof(int), "x");
        var lambdaIdx = tree.Lambda(typeof(Func<int, int>), body: p, parameters: [p]);

        Asserts.AreEqual(2, tree.NodeCount);
        Asserts.AreEqual(lambdaIdx, tree.RootIdx);

        ref var lambda = ref tree.NodeAt(lambdaIdx);
        Asserts.AreEqual(ExpressionType.Lambda, lambda.NodeType);
        Asserts.AreEqual(p, lambda.ChildIdx);

        // params stored as Idx[] in Info — not chained via NextIdx (see Lambda factory)
        var parms = (Idx[])lambda.Info;
        Asserts.AreEqual(1, parms.Length);
        Asserts.AreEqual(p, parms[0]);
    }

    public void Build_lambda_add_two_params()
    {
        var tree = default(ExpressionTree);
        var px = tree.Parameter(typeof(int), "x");
        var py = tree.Parameter(typeof(int), "y");
        var add = tree.Add(px, py, typeof(int));
        var lambda = tree.Lambda(typeof(Func<int, int, int>), body: add, parameters: [px, py]);

        Asserts.AreEqual(4, tree.NodeCount);

        ref var lambdaNode = ref tree.NodeAt(lambda);
        Asserts.AreEqual(add, lambdaNode.ChildIdx);

        var parms = (Idx[])lambdaNode.Info;
        Asserts.AreEqual(2, parms.Length);
        Asserts.AreEqual(px, parms[0]);
        Asserts.AreEqual(py, parms[1]);

        // NextIdx is NOT touched — a param can still appear as a New/Call argument alongside being a lambda param
        ref var pxNode = ref tree.NodeAt(px);
        Asserts.IsTrue(pxNode.NextIdx.IsNil);
    }

    public void Build_new_expression()
    {
        var ctor = typeof(Tuple<int, string>).GetConstructor([typeof(int), typeof(string)]);
        var tree = default(ExpressionTree);
        var arg1 = tree.Constant(1);
        var arg2 = tree.Constant("hi");
        var newIdx = tree.New(ctor, arg1, arg2);

        ref var newNode = ref tree.NodeAt(newIdx);
        Asserts.AreEqual(ExpressionType.New, newNode.NodeType);
        Asserts.AreEqual(typeof(Tuple<int, string>), newNode.Type);
        Asserts.AreEqual(ctor, (ConstructorInfo)newNode.Info);

        var siblings = tree.Siblings(newNode.ChildIdx).ToArray();
        Asserts.AreEqual(2, siblings.Length);
        Asserts.AreEqual(arg1, siblings[0]);
        Asserts.AreEqual(arg2, siblings[1]);
    }

    public void Build_call_static_method()
    {
        var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)]);
        var tree = default(ExpressionTree);
        var arg = tree.Parameter(typeof(int), "n");
        var callIdx = tree.Call(method, Idx.Nil, arg);

        ref var callNode = ref tree.NodeAt(callIdx);
        Asserts.AreEqual(ExpressionType.Call, callNode.NodeType);
        Asserts.AreEqual(method, (MethodInfo)callNode.Info);
    }

    public void Build_conditional()
    {
        var tree = default(ExpressionTree);
        var x = tree.Parameter(typeof(int), "x");
        var zero = tree.Constant(0);
        var test = tree.Binary(ExpressionType.GreaterThan, x, zero, typeof(bool));
        var neg = tree.Negate(x, typeof(int));
        var xCopy = tree.Parameter(typeof(int), "x_copy");
        var cond = tree.Conditional(test, xCopy, neg, typeof(int));

        ref var condNode = ref tree.NodeAt(cond);
        Asserts.AreEqual(ExpressionType.Conditional, condNode.NodeType);
        Asserts.AreEqual(test, condNode.ChildIdx);
        Asserts.AreEqual(xCopy, condNode.ExtraIdx);
        // ifFalse is chained as ifTrue.NextIdx
        ref var ifTrueNode = ref tree.NodeAt(xCopy);
        Asserts.AreEqual(neg, ifTrueNode.NextIdx);
    }

    public void Build_block_with_variable()
    {
        var tree = default(ExpressionTree);
        var v = tree.Variable(typeof(int), "v");
        var zero = tree.Constant(0);
        var assign = tree.Assign(v, zero, typeof(int));
        var blockIdx = tree.Block(typeof(int), exprs: [assign, v], variables: [v]);

        ref var blockNode = ref tree.NodeAt(blockIdx);
        Asserts.AreEqual(ExpressionType.Block, blockNode.NodeType);
        Asserts.IsFalse(blockNode.ChildIdx.IsNil);
        Asserts.IsFalse(blockNode.ExtraIdx.IsNil);
    }

    public void Structural_equality_same_trees()
    {
        var t1 = BuildAddTree();
        var t2 = BuildAddTree();
        Asserts.IsTrue(ExpressionTree.StructurallyEqual(ref t1, ref t2));
    }

    public void Structural_equality_different_trees()
    {
        var t1 = BuildAddTree();

        var t2 = default(ExpressionTree);
        var a = t2.Constant(10);
        var b = t2.Constant(99);
        t2.Add(a, b, typeof(int));

        Asserts.IsFalse(ExpressionTree.StructurallyEqual(ref t1, ref t2));
    }

    public void Convert_to_system_expression_constant_lambda()
    {
        var tree = default(ExpressionTree);
        var c = tree.Constant(42);
        tree.Lambda(typeof(Func<int>), body: c);

        var sysExpr = tree.ToSystemExpression();
        Asserts.IsNotNull(sysExpr);
        Asserts.AreEqual(ExpressionType.Lambda, sysExpr.NodeType);
    }

    public void Convert_to_system_expression_add_lambda()
    {
        var tree = default(ExpressionTree);
        var px = tree.Parameter(typeof(int), "x");
        var py = tree.Parameter(typeof(int), "y");
        var add = tree.Add(px, py, typeof(int));
        tree.Lambda(typeof(Func<int, int, int>), body: add, parameters: [px, py]);

        var sysExpr = (LambdaExpression)tree.ToSystemExpression();
        Asserts.AreEqual(2, sysExpr.Parameters.Count);
        Asserts.AreEqual(ExpressionType.Add, sysExpr.Body.NodeType);
    }

    public void Convert_to_system_expression_new_lambda()
    {
        var ctor = typeof(Tuple<int, string>).GetConstructor([typeof(int), typeof(string)]);
        var tree = default(ExpressionTree);
        var n = tree.Parameter(typeof(int), "n");
        var s = tree.Constant("x");
        var newIdx = tree.New(ctor, n, s);
        tree.Lambda(typeof(Func<int, Tuple<int, string>>), body: newIdx, parameters: [n]);

        var sysExpr = (LambdaExpression)tree.ToSystemExpression();
        Asserts.AreEqual(ExpressionType.New, sysExpr.Body.NodeType);
    }

    public void Roundtrip_lambda_identity_compile_and_invoke()
    {
        var tree = default(ExpressionTree);
        var p = tree.Parameter(typeof(int), "x");
        tree.Lambda(typeof(Func<int, int>), body: p, parameters: [p]);

        var fn = ((Expression<Func<int, int>>)tree.ToSystemExpression()).Compile();
        Asserts.AreEqual(7, fn(7));
    }

    public void Roundtrip_lambda_add_compile_and_invoke()
    {
        var tree = default(ExpressionTree);
        var px = tree.Parameter(typeof(int), "x");
        var py = tree.Parameter(typeof(int), "y");
        var add = tree.Add(px, py, typeof(int));
        tree.Lambda(typeof(Func<int, int, int>), body: add, parameters: [px, py]);

        var fn = ((Expression<Func<int, int, int>>)tree.ToSystemExpression()).Compile();
        Asserts.AreEqual(11, fn(4, 7));
    }

    public void Closure_constant_is_mutable_after_build()
    {
        var tree = default(ExpressionTree);
        var c = tree.Constant("initial", putIntoClosure: true);
        tree.Lambda(typeof(Func<string>), body: c);

        // Swap constant in-place; the Idx still points to the same closure slot.
        tree.ClosureConstants.GetSurePresentRef(0) = "updated";

        var fn = ((Expression<Func<string>>)tree.ToSystemExpression()).Compile();
        Asserts.AreEqual("updated", fn());
    }

    public void Dump_does_not_throw()
    {
        var tree = BuildAddTree();
        var dump = tree.Dump();
        Asserts.IsNotNull(dump);
        Asserts.IsTrue(dump.Contains("ExpressionTree"));
    }

    private static ExpressionTree BuildAddTree()
    {
        var tree = default(ExpressionTree);
        var a = tree.Constant(10);
        var b = tree.Constant(20);
        tree.Add(a, b, typeof(int));
        return tree;
    }
}
#endif

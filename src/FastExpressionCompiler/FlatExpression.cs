/*
The MIT License (MIT)

Copyright (c) 2016-2026 Maksim Volkau

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

// FlatExpression.cs — POC for a data-oriented, flat (SOA-flavoured) expression tree.
//
// KEY IDEAS (from issue #512 / comments):
//   • Intrusive linked-list tree: every node has ChildIdx (first child) + NextIdx (next sibling)
//     encoded as 1-based indices into a single flat Nodes array.
//   • 0 (default) == nil, so an uninitialised Idx.It means "absent".
//   • ExpressionNode is a "fat" struct: NodeType, Type, Info, plus two child index slots.
//   • ExpressionTree keeps all nodes + closure constants in SmallList<> wrappers.
//     The SmallList<T, Stack16<T>, NoArrayPool<T>> variant keeps the first 16 nodes
//     directly inside the struct (on the stack when the tree is a local variable).
//   • Factory methods mutate `this` and return the 1-based Idx of the new node.
//
// WINS:
//   ✓ Small expressions fully on stack — zero heap allocation for ≤16 nodes.
//   ✓ Trivially serializable: arrays of plain structs with integer references.
//   ✓ O(1) node access by Idx — no pointer chasing.
//   ✓ Structural equality via a single pass over the two arrays.
//   ✓ Closure constants collected automatically during construction.
//   ✓ Dead-code / liveness bits can be packed into the Idx.It upper bits later.
//
// GAPS / CONS / OBSTACLES:
//   ✗ Not API-compatible with System.Linq.Expressions — requires a conversion adapter.
//   ✗ Mutable struct semantics: accidental copy of ExpressionTree silently forks state.
//   ✗ A node can only belong to one tree (tree, not DAG); re-use across trees requires
//     cloning (but that is a minor cost given how rarely it occurs).
//   ✗ ExpressionNode fat-struct (≈ 40 bytes) × 16 on-stack ≈ 640 bytes per tree on
//     the call-stack — suitable for leaf methods, not for deeply recursive builders.
//   ✗ Parameter identity across nested lambdas must be tracked by the caller through the
//     returned Idx (same index = same parameter).
//   ✗ Info field boxes MethodBase / string — one allocation per Call/New/Parameter node.
//     A future optimisation could use a dedicated MethodBase[] side array.

#nullable disable

namespace FastExpressionCompiler.FlatExpression;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FastExpressionCompiler.ImTools;

using SysExpr = System.Linq.Expressions.Expression;
using SysParam = System.Linq.Expressions.ParameterExpression;

/// <summary>
/// 1-based index into <see cref="ExpressionTree.Nodes"/>.
/// <c>default</c> / <c>It == 0</c> is the nil sentinel.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Idx : IEquatable<Idx>
{
    /// <summary>1-based position in the Nodes array. 0 = nil.</summary>
    public int It;

    /// <summary>True when this index represents "no node".</summary>
    public bool IsNil => It == 0;

    /// <summary>The nil sentinel.</summary>
    public static Idx Nil => default;

    /// <summary>Creates an Idx from a 1-based position.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Idx Of(int oneBasedIndex) => new Idx { It = oneBasedIndex };

    /// <inheritdoc/>
    public bool Equals(Idx other) => It == other.It;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Idx other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => It;

    /// <inheritdoc/>
    public override string ToString() => IsNil ? "nil" : It.ToString();
}

/// <summary>
/// A fat node inside <see cref="ExpressionTree.Nodes"/>. Uses an intrusive linked-list to
/// represent trees without any nested allocation.
///
/// Layout conventions by <see cref="ExpressionType"/>:
/// <list type="table">
///   <item><term>Constant</term><description>Type, Info = boxed value (or null when ConstantIndex ≥ 0)</description></item>
///   <item><term>Parameter/Variable</term><description>Type, Info = name (string or null)</description></item>
///   <item><term>Default</term><description>Type, ChildIdx/ExtraIdx = Nil</description></item>
///   <item><term>Unary</term><description>NodeType, Type, Info = MethodInfo (nullable), ChildIdx = operand</description></item>
///   <item><term>Binary</term><description>NodeType, Type, Info = MethodInfo (nullable), ChildIdx = left, ExtraIdx = right</description></item>
///   <item><term>New</term><description>Type, Info = ConstructorInfo, ChildIdx = first arg (args chained via NextIdx)</description></item>
///   <item><term>Call</term><description>Type, Info = MethodInfo, ChildIdx = instance (or first arg for static), ExtraIdx = first arg for instance calls</description></item>
///   <item><term>Lambda</term><description>Type = delegate type, Info = Idx[] of parameter indices, ChildIdx = body, ExtraIdx = Nil</description></item>
///   <item><term>Block</term><description>Type, ChildIdx = first expr (chained via NextIdx), ExtraIdx = first variable (chained via NextIdx)</description></item>
///   <item><term>Conditional</term><description>Type, ChildIdx = test, ExtraIdx = ifTrue; ifFalse is chained via ExtraIdx.NextIdx</description></item>
/// </list>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ExpressionNode
{
    /// <summary>The kind of this expression node (mirrors <see cref="ExpressionType"/>).</summary>
    public ExpressionType NodeType;

    /// <summary>The CLR type this expression evaluates to.</summary>
    public Type Type;

    /// <summary>
    /// Node-kind-specific metadata:
    /// <list type="bullet">
    ///   <item>Constant → the boxed value (null when using ConstantIndex).</item>
    ///   <item>Parameter → the parameter name (string, may be null).</item>
    ///   <item>Call / Invoke → the <see cref="MethodInfo"/>.</item>
    ///   <item>New → the <see cref="ConstructorInfo"/>.</item>
    ///   <item>Unary / Binary with custom method → the <see cref="MethodInfo"/>.</item>
    ///   <item>Lambda → <see cref="Idx"/>[] of parameter node indices.
    ///     <para>
    ///     Design note: Lambda does NOT chain params via NextIdx because the parameter nodes
    ///     may already have their NextIdx used as argument chains in New/Call.
    ///     Storing params as an Idx[] in Info avoids that conflict at the cost of one small
    ///     heap allocation per lambda node.  A future optimisation could pack them into a
    ///     dedicated ParamsIdx array side-table with a (start, count) slice reference.
    ///     </para>
    ///   </item>
    /// </list>
    /// </summary>
    public object Info;

    /// <summary>
    /// For <see cref="ExpressionType.Constant"/> nodes: 0-based index into
    /// <see cref="ExpressionTree.ClosureConstants"/> if ≥ 0, otherwise the value lives in
    /// <see cref="Info"/> directly and is treated as a compile-time literal (no closure slot).
    /// </summary>
    public int ConstantIndex;

    /// <summary>Next sibling in a linked list (next argument, parameter, or statement).</summary>
    public Idx NextIdx;

    /// <summary>First child node (first arg, operand, body, first statement…).</summary>
    public Idx ChildIdx;

    /// <summary>
    /// Second child slot:
    /// <list type="bullet">
    ///   <item>Binary → right operand.</item>
    ///   <item>Lambda → Nil (parameters are stored as Idx[] in Info instead of NextIdx chain).</item>
    ///   <item>Call (instance) → first argument (ChildIdx is the target).</item>
    ///   <item>Block → first variable declaration.</item>
    ///   <item>Conditional → ifTrue branch (ifFalse is ifTrue.NextIdx).</item>
    /// </list>
    /// </summary>
    public Idx ExtraIdx;
}

/// <summary>
/// Flat expression tree. All nodes live in <see cref="Nodes"/> (a <see cref="SmallList{T}"/>)
/// and closure constants in <see cref="ClosureConstants"/>.
///
/// Nodes are 1-indexed: <c>Idx.It == 1</c> corresponds to <c>Nodes.Items[0]</c>.
/// <c>Idx.It == 0</c> (<see cref="Idx.Nil"/>) means "absent".
///
/// Factory methods mutate <em>this</em> struct and return the <see cref="Idx"/> of the new node.
/// Because this is a mutable struct you should hold it as a local variable (or on the heap via
/// a wrapper) and not pass it by value to helpers — use <c>ref</c> parameters instead.
/// </summary>
public struct ExpressionTree
{
    // -------------------------------------------------------------------------
    // Storage
    // -------------------------------------------------------------------------

    /// <summary>
    /// All expression nodes. First 16 slots are held inside the struct itself via
    /// <see cref="Stack16{T}"/>; overflow spills to the heap array.
    /// </summary>
    public SmallList<ExpressionNode, Stack16<ExpressionNode>, NoArrayPool<ExpressionNode>> Nodes;

    /// <summary>
    /// Closure constants collected during tree construction.
    /// Reference-type values and structs larger than a pointer go here; primitives may
    /// stay in <see cref="ExpressionNode.Info"/> directly (ConstantIndex == -1).
    /// First 4 slots are held inside the struct itself.
    /// </summary>
    public SmallList<object, Stack4<object>, NoArrayPool<object>> ClosureConstants;

    /// <summary>The root node (usually the outermost Lambda). Set by <see cref="Lambda"/>.</summary>
    public Idx RootIdx;

    // -------------------------------------------------------------------------
    // Primitive helpers
    // -------------------------------------------------------------------------

    /// <summary>Gets a reference to the node at <paramref name="idx"/> (1-based, not nil).</summary>
    [UnscopedRef]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ExpressionNode NodeAt(Idx idx)
    {
        Debug.Assert(!idx.IsNil, "Cannot dereference a nil Idx");
        return ref Nodes.GetSurePresentRef(idx.It - 1);
    }

    /// <summary>Total number of nodes added so far.</summary>
    public int NodeCount => Nodes.Count;

    // -------------------------------------------------------------------------
    // Internal: add a node and return its 1-based Idx
    // -------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Idx AddNode(
        ExpressionType nodeType,
        Type type,
        object info = null,
        int constantIndex = -1,
        Idx childIdx = default,
        Idx extraIdx = default)
    {
        ref var n = ref Nodes.AddDefaultAndGetRef();
        n.NodeType = nodeType;
        n.Type = type;
        n.Info = info;
        n.ConstantIndex = constantIndex;
        n.ChildIdx = childIdx;
        n.ExtraIdx = extraIdx;
        n.NextIdx = Idx.Nil;
        return Idx.Of(Nodes.Count); // Count is already incremented by AddDefaultAndGetRef
    }

    // -------------------------------------------------------------------------
    // Factory — Constant
    // -------------------------------------------------------------------------

    /// <summary>
    /// Adds a constant node. Reference types and large structs are added to the closure
    /// constants array so they can be mutated after compilation; plain primitives (int, bool,
    /// string) are stored inline in <see cref="ExpressionNode.Info"/>.
    /// </summary>
    public Idx Constant(object value, bool putIntoClosure = false)
    {
        if (value == null)
            return AddNode(ExpressionType.Constant, typeof(object), null);

        var type = value.GetType();

        if (!putIntoClosure && IsInlineable(type))
            return AddNode(ExpressionType.Constant, type, value, constantIndex: -1);

        // Add to closure constants
        var ci = ClosureConstants.Count;
        ClosureConstants.Add(value);
        return AddNode(ExpressionType.Constant, type, null, constantIndex: ci);
    }

    /// <summary>Typed helper — avoids boxing for common value types.</summary>
    public Idx Constant<T>(T value, bool putIntoClosure = false) =>
        Constant((object)value, putIntoClosure);

    // Primitive types that are cheap to keep inline (no closure slot needed by default).
    private static bool IsInlineable(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(double) || t == typeof(float) ||
        t == typeof(bool) || t == typeof(string) || t == typeof(char) ||
        t == typeof(byte) || t == typeof(short) || t == typeof(decimal) ||
        t == typeof(DateTime) || t == typeof(Guid);

    // -------------------------------------------------------------------------
    // Factory — Parameter / Variable
    // -------------------------------------------------------------------------

    /// <summary>Adds a parameter node. Use the returned <see cref="Idx"/> to refer to the same parameter.</summary>
    public Idx Parameter(Type type, string name = null) =>
        AddNode(ExpressionType.Parameter, type, info: name);

    /// <summary>Alias for <see cref="Parameter"/> (variables are parameters in lambda body).</summary>
    public Idx Variable(Type type, string name = null) =>
        AddNode(ExpressionType.Parameter, type, info: name);

    // -------------------------------------------------------------------------
    // Factory — Default
    // -------------------------------------------------------------------------

    /// <summary>Adds a <c>default(T)</c> node.</summary>
    public Idx Default(Type type) =>
        AddNode(ExpressionType.Default, type);

    // -------------------------------------------------------------------------
    // Factory — Unary
    // -------------------------------------------------------------------------

    /// <summary>Adds a unary expression node.</summary>
    public Idx Unary(ExpressionType nodeType, Idx operand, Type type, MethodInfo method = null) =>
        AddNode(nodeType, type, info: method, childIdx: operand);

    /// <summary>Typed convert.</summary>
    public Idx Convert(Idx operand, Type toType) =>
        Unary(ExpressionType.Convert, operand, toType);

    /// <summary>Logical not.</summary>
    public Idx Not(Idx operand) =>
        Unary(ExpressionType.Not, operand, typeof(bool));

    /// <summary>Negate.</summary>
    public Idx Negate(Idx operand, Type type) =>
        Unary(ExpressionType.Negate, operand, type);

    // -------------------------------------------------------------------------
    // Factory — Binary
    // -------------------------------------------------------------------------

    /// <summary>Adds a binary expression node.</summary>
    public Idx Binary(ExpressionType nodeType, Idx left, Idx right, Type type, MethodInfo method = null) =>
        AddNode(nodeType, type, info: method, childIdx: left, extraIdx: right);

    /// <summary>Addition.</summary>
    public Idx Add(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Add, left, right, type);

    /// <summary>Subtraction.</summary>
    public Idx Subtract(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Subtract, left, right, type);

    /// <summary>Multiply.</summary>
    public Idx Multiply(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Multiply, left, right, type);

    /// <summary>Equal.</summary>
    public Idx Equal(Idx left, Idx right) =>
        Binary(ExpressionType.Equal, left, right, typeof(bool));

    /// <summary>Assign.</summary>
    public Idx Assign(Idx target, Idx value, Type type) =>
        Binary(ExpressionType.Assign, target, value, type);

    // -------------------------------------------------------------------------
    // Factory — New
    // -------------------------------------------------------------------------

    /// <summary>
    /// Adds a <c>new T(args…)</c> node.
    /// Arguments are linked via <see cref="ExpressionNode.NextIdx"/> in the order supplied.
    /// </summary>
    public Idx New(ConstructorInfo ctor, params Idx[] args)
    {
        var firstArgIdx = LinkList(args);
        return AddNode(ExpressionType.New, ctor.DeclaringType, info: ctor, childIdx: firstArgIdx);
    }

    // -------------------------------------------------------------------------
    // Factory — Call
    // -------------------------------------------------------------------------

    /// <summary>Adds a static or instance method call node.</summary>
    public Idx Call(MethodInfo method, Idx instance, params Idx[] args)
    {
        var returnType = method.ReturnType == typeof(void) ? typeof(void) : method.ReturnType;
        if (instance.IsNil)
        {
            // Static call: all args hang from ChildIdx
            var firstArgIdx = LinkList(args);
            return AddNode(ExpressionType.Call, returnType, info: method, childIdx: firstArgIdx);
        }
        else
        {
            // Instance call: target → ChildIdx, args → ExtraIdx
            var firstArgIdx = LinkList(args);
            return AddNode(ExpressionType.Call, returnType, info: method,
                childIdx: instance, extraIdx: firstArgIdx);
        }
    }

    // -------------------------------------------------------------------------
    // Factory — Lambda
    // -------------------------------------------------------------------------

    /// <summary>
    /// Adds a lambda node. <paramref name="body"/> is the body expression;
    /// <paramref name="parameters"/> are the parameter nodes (must already exist in
    /// <see cref="Nodes"/> — reuse the <see cref="Idx"/> values returned by
    /// <see cref="Parameter"/>).
    ///
    /// Parameters are stored in <see cref="ExpressionNode.Info"/> as an <see cref="Idx"/> array
    /// rather than linked via <see cref="ExpressionNode.NextIdx"/>, because a parameter node's
    /// NextIdx may already be in use as part of an argument chain (e.g. when the same parameter
    /// is passed to a <c>New</c> or <c>Call</c> node).  This is a key design trade-off:
    /// one small allocation per lambda vs. avoiding silent list corruption.
    ///
    /// Sets <see cref="RootIdx"/> when <paramref name="isRoot"/> is <c>true</c> (default).
    /// </summary>
    public Idx Lambda(Type delegateType, Idx body, Idx[] parameters = null, bool isRoot = true)
    {
        // Store params as Idx[] in Info — do NOT call LinkList to avoid corrupting NextIdx
        // chains that the same param nodes may already participate in (e.g. as New/Call args).
        var lambdaIdx = AddNode(ExpressionType.Lambda, delegateType,
            info: parameters,   // Idx[] or null
            childIdx: body);    // ExtraIdx left as Nil (unused for Lambda)
        if (isRoot)
            RootIdx = lambdaIdx;
        return lambdaIdx;
    }

    // -------------------------------------------------------------------------
    // Factory — Conditional
    // -------------------------------------------------------------------------

    /// <summary>Adds a conditional (ternary) expression.</summary>
    public Idx Conditional(Idx test, Idx ifTrue, Idx ifFalse, Type type)
    {
        // ifTrue and ifFalse are siblings: link ifFalse as ifTrue.NextIdx
        ref var ifTrueNode = ref NodeAt(ifTrue);
        ifTrueNode.NextIdx = ifFalse;
        return AddNode(ExpressionType.Conditional, type, childIdx: test, extraIdx: ifTrue);
    }

    // -------------------------------------------------------------------------
    // Factory — Block
    // -------------------------------------------------------------------------

    /// <summary>Adds a block expression.</summary>
    public Idx Block(Type type, Idx[] exprs, Idx[] variables = null)
    {
        var firstExprIdx = LinkList(exprs);
        var firstVarIdx = variables == null || variables.Length == 0
            ? Idx.Nil
            : LinkList(variables);
        return AddNode(ExpressionType.Block, type, childIdx: firstExprIdx, extraIdx: firstVarIdx);
    }

    // -------------------------------------------------------------------------
    // Intrusive-list helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Chains the nodes at the given indices into a singly-linked list via
    /// <see cref="ExpressionNode.NextIdx"/> and returns the head.
    /// The last node's NextIdx is left as <see cref="Idx.Nil"/>.
    /// </summary>
    public Idx LinkList(Idx[] indices)
    {
        if (indices == null || indices.Length == 0)
            return Idx.Nil;

        for (var i = 0; i < indices.Length - 1; i++)
            NodeAt(indices[i]).NextIdx = indices[i + 1];

        // Ensure last node's NextIdx is nil (in case it was previously linked elsewhere)
        NodeAt(indices[indices.Length - 1]).NextIdx = Idx.Nil;

        return indices[0];
    }

    /// <summary>Iterates the sibling chain starting at <paramref name="head"/>.</summary>
    public IEnumerable<Idx> Siblings(Idx head)
    {
        var cur = head;
        while (!cur.IsNil)
        {
            yield return cur;
            cur = NodeAt(cur).NextIdx;
        }
    }

    // -------------------------------------------------------------------------
    // Conversion to System.Linq.Expressions
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts the flat tree back into a <see cref="SysExpr"/> hierarchy so it can be
    /// compiled with the standard compiler or FEC.
    ///
    /// Gaps / obstacles visible here:
    ///   • We need to materialise a <see cref="SysParam"/> for each Parameter node and
    ///     cache it by Idx so that shared parameters in nested lambdas resolve correctly.
    ///   • Closure constants are wrapped in a <c>ConstantExpression</c>; a real FEC
    ///     integration would inject them into an ArrayClosure instead.
    /// </summary>
    public SysExpr ToSystemExpression() => ToSystemExpression(RootIdx, new Dictionary<int, SysParam>());

    private SysExpr ToSystemExpression(Idx nodeIdx, Dictionary<int, SysParam> paramMap)
    {
        if (nodeIdx.IsNil)
            throw new InvalidOperationException("Cannot convert nil Idx to System.Linq.Expressions");

        ref var node = ref NodeAt(nodeIdx);

        switch (node.NodeType)
        {
            case ExpressionType.Constant:
            {
                var value = node.ConstantIndex >= 0
                    ? ClosureConstants.GetSurePresentRef(node.ConstantIndex)
                    : node.Info;
                return SysExpr.Constant(value, node.Type);
            }

            case ExpressionType.Parameter:
            {
                if (!paramMap.TryGetValue(nodeIdx.It, out var p))
                {
                    p = SysExpr.Parameter(node.Type, node.Info as string);
                    paramMap[nodeIdx.It] = p;
                }
                return p;
            }

            case ExpressionType.Default:
                return SysExpr.Default(node.Type);

            case ExpressionType.Lambda:
            {
                // params are stored as Idx[] in Info (not linked via NextIdx — see Lambda factory)
                var paramIdxs = node.Info as Idx[];
                var paramExprs = new List<SysParam>();
                if (paramIdxs != null)
                    foreach (var pIdx in paramIdxs)
                    {
                        // ToSystemExpression for parameters populates paramMap
                        var p = (SysParam)ToSystemExpression(pIdx, paramMap);
                        paramExprs.Add(p);
                    }
                // Build body AFTER registering parameters so they are found in paramMap
                var body = ToSystemExpression(node.ChildIdx, paramMap);
                return SysExpr.Lambda(node.Type, body, paramExprs);
            }

            case ExpressionType.New:
            {
                var ctor = (ConstructorInfo)node.Info;
                var args = SiblingList(node.ChildIdx, paramMap);
                return SysExpr.New(ctor, args);
            }

            case ExpressionType.Call:
            {
                var method = (MethodInfo)node.Info;
                if (method.IsStatic)
                {
                    var args = SiblingList(node.ChildIdx, paramMap);
                    return SysExpr.Call(method, args);
                }
                else
                {
                    var target = ToSystemExpression(node.ChildIdx, paramMap);
                    var args = SiblingList(node.ExtraIdx, paramMap);
                    return SysExpr.Call(target, method, args);
                }
            }

            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.Divide:
            case ExpressionType.Modulo:
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.Or:
            case ExpressionType.OrElse:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.Assign:
            case ExpressionType.LeftShift:
            case ExpressionType.RightShift:
            case ExpressionType.Power:
            case ExpressionType.Coalesce:
            {
                var left = ToSystemExpression(node.ChildIdx, paramMap);
                var right = ToSystemExpression(node.ExtraIdx, paramMap);
                return SysExpr.MakeBinary(node.NodeType, left, right, false, node.Info as MethodInfo);
            }

            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
            case ExpressionType.ArrayLength:
            case ExpressionType.Quote:
            case ExpressionType.TypeAs:
            case ExpressionType.Throw:
            case ExpressionType.Unbox:
            case ExpressionType.Increment:
            case ExpressionType.Decrement:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PostDecrementAssign:
            {
                var operand = ToSystemExpression(node.ChildIdx, paramMap);
                return SysExpr.MakeUnary(node.NodeType, operand, node.Type, node.Info as MethodInfo);
            }

            case ExpressionType.Conditional:
            {
                var test = ToSystemExpression(node.ChildIdx, paramMap);
                var ifTrue = ToSystemExpression(node.ExtraIdx, paramMap);
                var ifFalse = ToSystemExpression(NodeAt(node.ExtraIdx).NextIdx, paramMap);
                return SysExpr.Condition(test, ifTrue, ifFalse, node.Type);
            }

            case ExpressionType.Block:
            {
                var exprs = SiblingList(node.ChildIdx, paramMap);
                if (node.ExtraIdx.IsNil)
                    return SysExpr.Block(node.Type, exprs);

                var vars = new List<SysParam>();
                foreach (var vIdx in Siblings(node.ExtraIdx))
                    vars.Add((SysParam)ToSystemExpression(vIdx, paramMap));
                return SysExpr.Block(node.Type, vars, exprs);
            }

            default:
                throw new NotSupportedException(
                    $"FlatExpression → System.Linq.Expressions: NodeType {node.NodeType} is not yet mapped.");
        }
    }

    private List<SysExpr> SiblingList(Idx head, Dictionary<int, SysParam> paramMap)
    {
        var list = new List<SysExpr>();
        foreach (var idx in Siblings(head))
            list.Add(ToSystemExpression(idx, paramMap));
        return list;
    }

    // -------------------------------------------------------------------------
    // Structural equality
    // -------------------------------------------------------------------------

    /// <summary>
    /// Checks structural equality of two trees in O(n) time by comparing
    /// every node field and every closure constant.
    /// Win: no tree traversal required — a single loop over the flat arrays suffices.
    /// </summary>
    public static bool StructurallyEqual(ref ExpressionTree a, ref ExpressionTree b)
    {
        if (a.NodeCount != b.NodeCount) return false;
        if (a.ClosureConstants.Count != b.ClosureConstants.Count) return false;

        for (var i = 0; i < a.NodeCount; i++)
        {
            ref var na = ref a.Nodes.GetSurePresentRef(i);
            ref var nb = ref b.Nodes.GetSurePresentRef(i);
            if (na.NodeType != nb.NodeType) return false;
            if (na.Type != nb.Type) return false;
            if (!InfoEqual(na.Info, nb.Info)) return false;
            if (na.ConstantIndex != nb.ConstantIndex) return false;
            if (na.NextIdx.It != nb.NextIdx.It) return false;
            if (na.ChildIdx.It != nb.ChildIdx.It) return false;
            if (na.ExtraIdx.It != nb.ExtraIdx.It) return false;
        }

        for (var i = 0; i < a.ClosureConstants.Count; i++)
            if (!Equals(a.ClosureConstants.GetSurePresentRef(i),
                        b.ClosureConstants.GetSurePresentRef(i)))
                return false;

        return true;
    }

    private static bool InfoEqual(object infoA, object infoB)
    {
        if (infoA is Idx[] ia && infoB is Idx[] ib)
        {
            if (ia.Length != ib.Length) return false;
            for (var k = 0; k < ia.Length; k++)
                if (ia[k].It != ib[k].It) return false;
            return true;
        }
        return Equals(infoA, infoB);
    }

    // -------------------------------------------------------------------------
    // Debug / diagnostic
    // -------------------------------------------------------------------------

    /// <summary>Returns a human-readable dump of all nodes (useful for diagnostics).</summary>
    public string Dump()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"ExpressionTree  NodeCount={NodeCount}  ClosureConstants={ClosureConstants.Count}  RootIdx={RootIdx}");
        for (var i = 0; i < NodeCount; i++)
        {
            ref var n = ref Nodes.GetSurePresentRef(i);
            sb.AppendLine(
                $"  [{i + 1}] {n.NodeType,-22} type={n.Type?.Name,-14} " +
                $"info={InfoStr(n.Info),-30} " +
                $"ci={n.ConstantIndex,2}  " +
                $"child={n.ChildIdx}  extra={n.ExtraIdx}  next={n.NextIdx}");
        }
        if (ClosureConstants.Count > 0)
        {
            sb.AppendLine("  Closure constants:");
            for (var i = 0; i < ClosureConstants.Count; i++)
                sb.AppendLine($"    [{i}] = {ClosureConstants.GetSurePresentRef(i)}");
        }
        return sb.ToString();
    }

    private static string InfoStr(object info) =>
        info == null ? "—" :
        info is MethodBase mb ? mb.Name :
        info is Idx[] idxArr ? $"params[{string.Join(",", System.Linq.Enumerable.Select(idxArr, x => x.It))}]" :
        info.ToString();
}

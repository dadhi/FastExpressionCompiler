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

// POC for issue #512: data-oriented flat expression tree.
// Intrusive linked-list tree: ChildIdx (first child) + NextIdx (next sibling), 1-based into Nodes.
// 0/default == nil.  ExpressionTree keeps ≤16 nodes on the stack via Stack16<ExpressionNode>.

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

/// <summary>1-based index into <see cref="ExpressionTree.Nodes"/>. <c>default</c> == nil.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Idx : IEquatable<Idx>
{
    public int It;

    public bool IsNil => It == 0;
    public static Idx Nil => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Idx Of(int oneBasedIndex) => new Idx { It = oneBasedIndex };

    public bool Equals(Idx other) => It == other.It;
    public override bool Equals(object obj) => obj is Idx other && Equals(other);
    public override int GetHashCode() => It;
    public override string ToString() => IsNil ? "nil" : It.ToString();
}

/// <summary>
/// Fat node in <see cref="ExpressionTree.Nodes"/>. Intrusive linked-list tree encoding:
/// <list type="table">
///   <item><term>Constant</term>   <description>Info = boxed value; ConstantIndex ≥ 0 → value lives in ClosureConstants instead.</description></item>
///   <item><term>Parameter</term>  <description>Info = name (string or null).</description></item>
///   <item><term>Unary</term>      <description>Info = MethodInfo (nullable), ChildIdx = operand.</description></item>
///   <item><term>Binary</term>     <description>Info = MethodInfo (nullable), ChildIdx = left, ExtraIdx = right.</description></item>
///   <item><term>New</term>        <description>Info = ConstructorInfo, ChildIdx = first arg (chained via NextIdx).</description></item>
///   <item><term>Call</term>       <description>Info = MethodInfo, ChildIdx = instance-or-first-static-arg, ExtraIdx = first arg for instance calls.</description></item>
///   <item><term>Lambda</term>     <description>Info = Idx[] of params, ChildIdx = body. Params stored in Info rather than NextIdx chain because the same parameter node may already participate as a New/Call argument.</description></item>
///   <item><term>Block</term>      <description>ChildIdx = first expr, ExtraIdx = first variable (both chained via NextIdx).</description></item>
///   <item><term>Conditional</term><description>ChildIdx = test, ExtraIdx = ifTrue; ifFalse = ifTrue.NextIdx.</description></item>
/// </list>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ExpressionNode
{
    public ExpressionType NodeType;
    public Type Type;
    public object Info;
    /// <summary>≥ 0: index into <see cref="ExpressionTree.ClosureConstants"/>. -1: value is inline in Info.</summary>
    public int ConstantIndex;
    public Idx NextIdx;
    public Idx ChildIdx;
    public Idx ExtraIdx;
}

/// <summary>
/// Flat expression tree backed by a single flat Nodes array. Hold as a local or heap field —
/// do not pass by value (mutable struct; copy silently forks state).
/// </summary>
public struct ExpressionTree
{
    // First 16 nodes are on the stack; further nodes spill to a heap array.
    public SmallList<ExpressionNode, Stack16<ExpressionNode>, NoArrayPool<ExpressionNode>> Nodes;
    // First 4 closure constants on stack.
    public SmallList<object, Stack4<object>, NoArrayPool<object>> ClosureConstants;
    public Idx RootIdx;

    public int NodeCount => Nodes.Count;

    [UnscopedRef]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ExpressionNode NodeAt(Idx idx)
    {
        Debug.Assert(!idx.IsNil, "Cannot dereference a nil Idx");
        return ref Nodes.GetSurePresentRef(idx.It - 1);
    }

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
        return Idx.Of(Nodes.Count); // Count already incremented by AddDefaultAndGetRef
    }

    // Primitives with stable identity — safe to keep inline (ConstantIndex == -1).
    private static bool IsInlineable(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(double) || t == typeof(float) ||
        t == typeof(bool) || t == typeof(string) || t == typeof(char) ||
        t == typeof(byte) || t == typeof(short) || t == typeof(decimal) ||
        t == typeof(DateTime) || t == typeof(Guid);

    public Idx Constant(object value, bool putIntoClosure = false)
    {
        if (value == null)
            return AddNode(ExpressionType.Constant, typeof(object), null);

        var type = value.GetType();
        if (!putIntoClosure && IsInlineable(type))
            return AddNode(ExpressionType.Constant, type, value, constantIndex: -1);

        var ci = ClosureConstants.Count;
        ClosureConstants.Add(value);
        return AddNode(ExpressionType.Constant, type, null, constantIndex: ci);
    }

    public Idx Constant<T>(T value, bool putIntoClosure = false) =>
        Constant((object)value, putIntoClosure);

    public Idx Parameter(Type type, string name = null) =>
        AddNode(ExpressionType.Parameter, type, info: name);

    public Idx Variable(Type type, string name = null) =>
        AddNode(ExpressionType.Parameter, type, info: name);

    public Idx Default(Type type) =>
        AddNode(ExpressionType.Default, type);

    public Idx Unary(ExpressionType nodeType, Idx operand, Type type, MethodInfo method = null) =>
        AddNode(nodeType, type, info: method, childIdx: operand);

    public Idx Convert(Idx operand, Type toType) =>
        Unary(ExpressionType.Convert, operand, toType);

    public Idx Not(Idx operand) =>
        Unary(ExpressionType.Not, operand, typeof(bool));

    public Idx Negate(Idx operand, Type type) =>
        Unary(ExpressionType.Negate, operand, type);

    public Idx Binary(ExpressionType nodeType, Idx left, Idx right, Type type, MethodInfo method = null) =>
        AddNode(nodeType, type, info: method, childIdx: left, extraIdx: right);

    public Idx Add(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Add, left, right, type);

    public Idx Subtract(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Subtract, left, right, type);

    public Idx Multiply(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Multiply, left, right, type);

    public Idx Equal(Idx left, Idx right) =>
        Binary(ExpressionType.Equal, left, right, typeof(bool));

    public Idx Assign(Idx target, Idx value, Type type) =>
        Binary(ExpressionType.Assign, target, value, type);

    public Idx New(ConstructorInfo ctor, params Idx[] args)
    {
        var firstArgIdx = LinkList(args);
        return AddNode(ExpressionType.New, ctor.DeclaringType, info: ctor, childIdx: firstArgIdx);
    }

    public Idx Call(MethodInfo method, Idx instance, params Idx[] args)
    {
        var returnType = method.ReturnType == typeof(void) ? typeof(void) : method.ReturnType;
        var firstArgIdx = LinkList(args);
        return instance.IsNil
            ? AddNode(ExpressionType.Call, returnType, info: method, childIdx: firstArgIdx)
            : AddNode(ExpressionType.Call, returnType, info: method, childIdx: instance, extraIdx: firstArgIdx);
    }

    // Parameters stored in Info as Idx[] rather than chained via NextIdx, because the same
    // parameter node may already have its NextIdx used as part of a New/Call argument chain.
    public Idx Lambda(Type delegateType, Idx body, Idx[] parameters = null, bool isRoot = true)
    {
        var lambdaIdx = AddNode(ExpressionType.Lambda, delegateType, info: parameters, childIdx: body);
        if (isRoot)
            RootIdx = lambdaIdx;
        return lambdaIdx;
    }

    public Idx Conditional(Idx test, Idx ifTrue, Idx ifFalse, Type type)
    {
        NodeAt(ifTrue).NextIdx = ifFalse; // ifFalse hangs off ifTrue.NextIdx
        return AddNode(ExpressionType.Conditional, type, childIdx: test, extraIdx: ifTrue);
    }

    public Idx Block(Type type, Idx[] exprs, Idx[] variables = null)
    {
        var firstExprIdx = LinkList(exprs);
        var firstVarIdx = variables == null || variables.Length == 0 ? Idx.Nil : LinkList(variables);
        return AddNode(ExpressionType.Block, type, childIdx: firstExprIdx, extraIdx: firstVarIdx);
    }

    public Idx LinkList(Idx[] indices)
    {
        if (indices == null || indices.Length == 0)
            return Idx.Nil;
        for (var i = 0; i < indices.Length - 1; i++)
            NodeAt(indices[i]).NextIdx = indices[i + 1];
        NodeAt(indices[indices.Length - 1]).NextIdx = Idx.Nil; // reset in case node was previously linked
        return indices[0];
    }

    // Allocates an enumerator — suitable for tests and diagnostics; avoid in hot paths.
    public IEnumerable<Idx> Siblings(Idx head)
    {
        var cur = head;
        while (!cur.IsNil)
        {
            yield return cur;
            cur = NodeAt(cur).NextIdx;
        }
    }

    // Builds body after registering params so they are found in paramMap when encountered in the body.
    public SysExpr ToSystemExpression()
    {
        var paramMap = default(SmallMap16<int, SysParam, IntEq>);
        return ToSystemExpression(RootIdx, ref paramMap);
    }

    private SysExpr ToSystemExpression(Idx nodeIdx, ref SmallMap16<int, SysParam, IntEq> paramMap)
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
                ref var p = ref paramMap.Map.AddOrGetValueRef(nodeIdx.It, out var found);
                if (!found)
                    p = SysExpr.Parameter(node.Type, node.Info as string);
                return p;
            }

            case ExpressionType.Default:
                return SysExpr.Default(node.Type);

            case ExpressionType.Lambda:
            {
                var paramIdxs = node.Info as Idx[];
                var paramExprs = new List<SysParam>();
                if (paramIdxs != null)
                    foreach (var pIdx in paramIdxs)
                        paramExprs.Add((SysParam)ToSystemExpression(pIdx, ref paramMap));
                var body = ToSystemExpression(node.ChildIdx, ref paramMap);
                return SysExpr.Lambda(node.Type, body, paramExprs);
            }

            case ExpressionType.New:
                return SysExpr.New((ConstructorInfo)node.Info, SiblingList(node.ChildIdx, ref paramMap));

            case ExpressionType.Call:
            {
                var method = (MethodInfo)node.Info;
                return method.IsStatic
                    ? SysExpr.Call(method, SiblingList(node.ChildIdx, ref paramMap))
                    : SysExpr.Call(ToSystemExpression(node.ChildIdx, ref paramMap), method, SiblingList(node.ExtraIdx, ref paramMap));
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
                return SysExpr.MakeBinary(node.NodeType,
                    ToSystemExpression(node.ChildIdx, ref paramMap),
                    ToSystemExpression(node.ExtraIdx, ref paramMap),
                    false, node.Info as MethodInfo);

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
                return SysExpr.MakeUnary(node.NodeType,
                    ToSystemExpression(node.ChildIdx, ref paramMap),
                    node.Type, node.Info as MethodInfo);

            case ExpressionType.Conditional:
                return SysExpr.Condition(
                    ToSystemExpression(node.ChildIdx, ref paramMap),
                    ToSystemExpression(node.ExtraIdx, ref paramMap),
                    ToSystemExpression(NodeAt(node.ExtraIdx).NextIdx, ref paramMap),
                    node.Type);

            case ExpressionType.Block:
            {
                var exprs = SiblingList(node.ChildIdx, ref paramMap);
                if (node.ExtraIdx.IsNil)
                    return SysExpr.Block(node.Type, exprs);
                var vars = new List<SysParam>();
                var vCur = node.ExtraIdx;
                while (!vCur.IsNil)
                {
                    vars.Add((SysParam)ToSystemExpression(vCur, ref paramMap));
                    vCur = NodeAt(vCur).NextIdx;
                }
                return SysExpr.Block(node.Type, vars, exprs);
            }

            default:
                throw new NotSupportedException(
                    $"FlatExpression → System.Linq.Expressions: NodeType {node.NodeType} is not yet mapped.");
        }
    }

    private List<SysExpr> SiblingList(Idx head, ref SmallMap16<int, SysParam, IntEq> paramMap)
    {
        var list = new List<SysExpr>();
        var cur = head;
        while (!cur.IsNil)
        {
            list.Add(ToSystemExpression(cur, ref paramMap));
            cur = NodeAt(cur).NextIdx;
        }
        return list;
    }

    // O(n) structural equality — no traversal, single pass over the flat arrays.
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
        // Lambda Info is Idx[] — Equals() on arrays checks reference equality, not contents.
        if (infoA is Idx[] ia && infoB is Idx[] ib)
        {
            if (ia.Length != ib.Length) return false;
            for (var k = 0; k < ia.Length; k++)
                if (ia[k].It != ib[k].It) return false;
            return true;
        }
        return Equals(infoA, infoB);
    }

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
        info is Idx[] idxArr ? $"params[{string.Join(",", Enumerable.Select(idxArr, x => x.It))}]" :
        info.ToString();
}

// Experimental: the initial compilation path for the ExprTree, to learn how much of FEC could work on the flat nodes directly.
#nullable disable
namespace FastExpressionCompiler.LightExpression;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using FastExpressionCompiler.FlatExpression;
using FastExpressionCompiler.LightExpression.ImTools;
using ExprType = System.Linq.Expressions.ExpressionType;

public static partial class ExpressionCompiler
{
    /// <summary>Compiles the flat tree. The int/bool subset goes straight to IL from the nodes, no Expression objects are made;
    /// everything else is converted to the LightExpression and compiled the usual way.</summary>
    public static TDelegate CompileFast<TDelegate>(this ref ExprTree tree, bool ifFastFailedReturnNull = false,
        CompilerFlags flags = CompilerFlags.Default) where TDelegate : class =>
        tree.TryCompileFlat<TDelegate>()
        ?? ((LambdaExpression)tree.ToLightExpression()).CompileFast<TDelegate>(ifFastFailedReturnNull, flags);

    struct FlatVar
    {
        public ushort Id;
        public Type Type;
        public short Arg; // the argument index (the closure is 0) or -1
        public LocalBuilder Local; // the local, or the local StrongBox if Boxed
        public short Slot; // for Boxed: the StrongBox is the item of the closure NonPassedParams, or -1 if it is the Local
        public bool Boxed; // the variable is used by a nested lambda, so it lives in the StrongBox shared with that lambda
    }

    /// <summary>The nested lambda and the outer variables it uses, the closure NonPassedParams are in this order.</summary>
    private sealed class FlatLambda
    {
        public int Idx;
        public List<(ushort Id, Type Type)> Captures = new();
    }

    /// <summary>The state of one compilation. The lists are only created when there are nested lambdas.</summary>
    private sealed class FlatCtx
    {
        public object[] Constants; // the closure constants, then the open delegates of the nested lambdas
        public int NextSlot;
        public List<(ushort Id, Type Type)> Captured; // all variables used by the nested lambdas
        public List<FlatLambda> Lambdas;
        public List<(ushort Id, int Depth)> Declared; // scratch of the captures search
        public List<FlatLambda> Stack; // scratch of the captures search

        public bool IsCaptured(ushort id)
        {
            if (Captured != null)
                for (var i = 0; i < Captured.Count; ++i)
                    if (Captured[i].Id == id) return true;
            return false;
        }

        public FlatLambda FindLambda(int idx)
        {
            if (Lambdas != null)
                for (var i = 0; i < Lambdas.Count; ++i)
                    if (Lambdas[i].Idx == idx) return Lambdas[i];
            return null;
        }
    }

    private static readonly ConstructorInfo FlatArrayClosureCtor = typeof(ArrayClosure).GetConstructors()[0];

    private static void FlatAddCapture(List<(ushort Id, Type Type)> list, ushort id, Type type)
    {
        for (var i = 0; i < list.Count; ++i)
            if (list[i].Id == id) return;
        list.Add((id, type));
    }

    /// <summary>Finds which variables are used by the lambdas other than the declaring one. Recursive but only run for the nested lambdas.
    /// The variable is declared by the lambda parameter or the block variable, the usage is the Parameter node with the same id.</summary>
    private static void FlatFindCaptures(ref ExprTree tree, int idx, FlatCtx ctx)
    {
        ref var n = ref tree.Nodes.GetSurePresentRef(idx);
        switch (n.NodeType)
        {
            case ExprType.Constant:
                return;

            case ExprType.Parameter:
                {
                    var d = ctx.Declared.Count - 1;
                    while (d >= 0 && ctx.Declared[d].Id != n.ChildIdx) --d;
                    if (d < 0)
                        return; // not in scope, the emit throws
                    // the lambdas deeper than the declaring one are capturing it
                    for (var k = ctx.Declared[d].Depth; k < ctx.Stack.Count; ++k)
                    {
                        FlatAddCapture(ctx.Stack[k].Captures, n.ChildIdx, n.Type);
                        FlatAddCapture(ctx.Captured, n.ChildIdx, n.Type);
                    }
                    return;
                }

            case ExprType.Lambda:
                {
                    var lambda = new FlatLambda { Idx = idx };
                    ctx.Lambdas.Add(lambda);
                    ctx.Stack.Add(lambda);
                    var scope = ctx.Declared.Count;
                    var bodyIdx = n.ChildIdx;
                    var parIdx = tree.Nodes.GetSurePresentRef(bodyIdx).NextIdx;
                    for (var i = 1; i < n.ChildCount; ++i)
                    {
                        ref var par = ref tree.Nodes.GetSurePresentRef(parIdx);
                        ctx.Declared.Add((par.ChildIdx, ctx.Stack.Count));
                        parIdx = par.NextIdx;
                    }
                    FlatFindCaptures(ref tree, bodyIdx, ctx);
                    ctx.Declared.RemoveRange(scope, ctx.Declared.Count - scope);
                    ctx.Stack.RemoveAt(ctx.Stack.Count - 1);
                    return;
                }

            case ExprType.Block:
                {
                    ref var exprs = ref tree.Nodes.GetSurePresentRef(n.ChildIdx);
                    var scope = ctx.Declared.Count;
                    var varIdx = exprs.NextIdx;
                    for (var i = 1; i < n.ChildCount; ++i)
                    {
                        ref var v = ref tree.Nodes.GetSurePresentRef(varIdx);
                        ctx.Declared.Add((v.ChildIdx, ctx.Stack.Count));
                        varIdx = v.NextIdx;
                    }
                    var exprIdx = exprs.ChildIdx;
                    for (var i = 0; i < exprs.ChildCount; ++i)
                    {
                        FlatFindCaptures(ref tree, exprIdx, ctx);
                        exprIdx = tree.Nodes.GetSurePresentRef(exprIdx).NextIdx;
                    }
                    ctx.Declared.RemoveRange(scope, ctx.Declared.Count - scope);
                    return;
                }

            default:
                {
                    var childIdx = n.ChildIdx;
                    for (var i = 0; i < n.ChildCount; ++i)
                    {
                        FlatFindCaptures(ref tree, childIdx, ctx);
                        childIdx = tree.Nodes.GetSurePresentRef(childIdx).NextIdx;
                    }
                    return;
                }
        }
    }

    /// <summary>The lambda return and parameter types must be flat and be the same as the delegate has. The nested lambda
    /// delegate must be the Func (the closure is curried into it) with the supported number of parameters.</summary>
    private static bool FlatLambdaFits(ref ExprTree tree, ref ExprNode lambda, bool nested)
    {
        var invoke = lambda.Type.GetMethod("Invoke");
        if (invoke == null)
            return false;
        ref var body = ref tree.Nodes.GetSurePresentRef(lambda.ChildIdx);
        var returnType = invoke.ReturnType;
        var pars = invoke.GetParameters();
        if (!IsFlatType(returnType) || !FlatFits(returnType, body.Type) || pars.Length != lambda.ChildCount - 1)
            return false;

        var types = nested ? new Type[pars.Length] : null;
        var parIdx = body.NextIdx;
        for (var i = 0; i < pars.Length; ++i)
        {
            ref var par = ref tree.Nodes.GetSurePresentRef(parIdx);
            if (par.Type != pars[i].ParameterType)
                return false;
            if (nested)
                types[i] = par.Type;
            parIdx = par.NextIdx;
        }
        return !nested || pars.Length <= 5 && Tools.GetFuncOrActionType(types, returnType) == lambda.Type; // 5 is the CurryClosureFuncs max
    }

    private static Type FlatBoxType(Type t) => typeof(StrongBox<>).MakeGenericType(t);

    private static LocalBuilder FlatNewBox(ILGenerator il, Type t)
    {
        var boxType = FlatBoxType(t);
        var box = il.DeclareLocal(boxType);
        il.Emit(OpCodes.Newobj, boxType.GetConstructor(Type.EmptyTypes));
        il.Emit(OpCodes.Stloc, box);
        return box;
    }

    private static void FlatLoadBox(ILGenerator il, in FlatVar v)
    {
        if (v.Slot < 0)
        {
            il.Emit(OpCodes.Ldloc, v.Local);
            return;
        }
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, ArrayClosureWithNonPassedParamsField);
        il.Emit(OpCodes.Ldc_I4, (int)v.Slot);
        il.Emit(OpCodes.Ldelem_Ref);
        il.Emit(OpCodes.Castclass, FlatBoxType(v.Type));
    }

    private static FlatVar FlatFindVar(ref SmallList<FlatVar, Stack8<FlatVar>, NoArrayPool<FlatVar>> vars, ushort id)
    {
        for (var i = vars.Count - 1; i >= 0; --i)
        {
            ref var v = ref vars.GetSurePresentRef(i);
            if (v.Id == id)
                return v;
        }
        throw new InvalidOperationException("The parameter is not in scope"); // todo: @wip by construction check
    }

    private static bool IsFlatType(Type t) => t == typeof(int) || t == typeof(bool) || !t.IsValueType && !t.IsByRef && !t.IsPointer;

    // the lambda and delegate return/parameter types are checked once per compile, not per node
    private static bool FlatFits(Type to, Type from) => to == from || !to.IsValueType && !from.IsValueType && to.IsAssignableFrom(from);

    /// <summary>The collect phase on the flat nodes: one linear pass over the contiguous array, no recursion, no stack, no per-node casts.
    /// What the tree construction already guarantees is not checked here: the child counts, the not-null children, the parameter
    /// and label ids, the closure constants (they are in the separate list, so `IsClosureBoundConstant` is already answered).
    /// Returns `OK` if the tree is the int/bool subset which <see cref="TryCompileFlat{TDelegate}"/> can emit.</summary>
    public static Result TryCollectFlatInfo(this ref ExprTree tree) => tree.CollectFlat(out _);

    private static Result CollectFlat(this ref ExprTree tree, out int lambdas)
    {
        lambdas = 0;
        var count = tree.Nodes.Count;
        if (count < 2)
            return Result.NotSupported_UnknownExpression;

        for (var i = 1; i < count; ++i)
        {
            ref var n = ref tree.Nodes.GetSurePresentRef(i);
            if (n.Kind == ExprNodeKind.BlockExprs)
                continue;
            if (n.Kind != ExprNodeKind.Expression)
                return Result.NotSupported_UnknownExpression;

            var t = n.Type;
            switch (n.NodeType)
            {
                case ExprType.Add:
                case ExprType.Subtract:
                case ExprType.Multiply:
                    if (t != typeof(int)) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.Conditional:
                case ExprType.Block:
                    if (!IsFlatType(t)) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.Assign:
                    // the target must be a parameter, the member assignment is not emitted yet
                    if (!IsFlatType(t) || tree.Nodes.GetSurePresentRef(n.ChildIdx).NodeType != ExprType.Parameter) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.Parameter:
                    if (!IsFlatType(t) || n.Flags != 0) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.New:
                    // the arguments are not type-checked, same as the Light: the factories did that
                    if (!IsFlatType(t) || n.Obj is not ConstructorInfo) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.Call:
                    if (!IsFlatType(t) || n.Obj is not MethodInfo m || m.IsGenericMethodDefinition) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.MemberAccess:
                    if (!IsFlatType(t) || !(n.Obj is FieldInfo f && !f.IsLiteral || n.Obj is PropertyInfo p && p.CanRead))
                        return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.Constant:
                    {
                        var isInline = ReferenceEquals(n.Obj, ExprNode.InlineValueMarker);
                        if (isInline ? t != typeof(int) && t != typeof(bool)
                            : t.IsValueType || !(ReferenceEquals(n.Obj, ExprTree.ClosureConstantMarker) || n.Obj == null || n.Obj is string && t == typeof(string)))
                            return Result.NotSupported_UnknownExpression;
                        break;
                    }
                case ExprType.Equal:
                case ExprType.GreaterThan:
                    if (tree.Nodes.GetSurePresentRef(n.ChildIdx).Type != typeof(int)) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.AndAlso:
                case ExprType.OrElse:
                case ExprType.Not:
                    if (t != typeof(bool)) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.Lambda:
                    ++lambdas;
                    if (!FlatLambdaFits(ref tree, ref n, i != tree.RootIdx)) return Result.NotSupported_UnknownExpression;
                    break;
                default:
                    return Result.NotSupported_UnknownExpression;
            }
        }

        return lambdas != 0 && tree.Nodes.GetSurePresentRef(tree.RootIdx).NodeType == ExprType.Lambda ? Result.OK : Result.NotSupported_UnknownExpression;
    }

    /// <summary>Returns null if the tree is not the supported subset or the delegate type is not the lambda type.</summary>
    public static TDelegate TryCompileFlat<TDelegate>(this ref ExprTree tree) where TDelegate : class
    {
        if (tree.CollectFlat(out var lambdas) != Result.OK)
            return null;

        if (tree.Nodes.GetSurePresentRef(tree.RootIdx).Type != typeof(TDelegate))
            return null;

        var constantCount = tree.ClosureConstants.Count;
        var ctx = new FlatCtx { NextSlot = constantCount };
        var slotCount = constantCount + lambdas - 1; // the slot per nested lambda for its open delegate
        if (slotCount != 0)
        {
            ctx.Constants = new object[slotCount];
            for (var i = 0; i < constantCount; ++i)
                ctx.Constants[i] = tree.ClosureConstants[i];
        }

        if (lambdas > 1)
        {
            ctx.Captured = new();
            ctx.Lambdas = new();
            ctx.Declared = new();
            ctx.Stack = new();
            FlatFindCaptures(ref tree, tree.RootIdx, ctx); // todo: @perf the recursive pass, the parent links and the flags at construction would make it a linear scan
        }

        var closure = ctx.Constants == null ? EmptyArrayClosure : new ArrayClosure(ctx.Constants);
        return (TDelegate)(object)EmitFlatLambda(ref tree, tree.RootIdx, ctx, typeof(TDelegate), closure);
    }

    /// <summary>Emits the lambda into its own DynamicMethod. The root is closed over the closure; the nested one
    /// (the delegate type is null) becomes the open delegate with the closure as the first parameter, to be curried at run-time.</summary>
    private static Delegate EmitFlatLambda(ref ExprTree tree, int lambdaIdx, FlatCtx ctx, Type delegateType, object closure)
    {
        ref var lambda = ref tree.Nodes.GetSurePresentRef(lambdaIdx);
        var bodyIdx = lambda.ChildIdx;
        var paramCount = lambda.ChildCount - 1;
        var paramTypes = new Type[paramCount + 1];
        paramTypes[0] = typeof(ArrayClosure);
        var parIdx = tree.Nodes.GetSurePresentRef(bodyIdx).NextIdx;
        for (var i = 1; i <= paramCount; ++i)
        {
            ref var par = ref tree.Nodes.GetSurePresentRef(parIdx);
            paramTypes[i] = par.Type;
            parIdx = par.NextIdx;
        }

        var returnType = lambda.Type.GetMethod("Invoke").ReturnType;
        var dynMethod = new DynamicMethod(string.Empty, returnType, paramTypes, typeof(ArrayClosure), true);
        var il = dynMethod.GetILGenerator();

        var vars = default(SmallList<FlatVar, Stack8<FlatVar>, NoArrayPool<FlatVar>>);
        parIdx = tree.Nodes.GetSurePresentRef(bodyIdx).NextIdx;
        for (var i = 1; i <= paramCount; ++i)
        {
            ref var par = ref tree.Nodes.GetSurePresentRef(parIdx);
            if (ctx.IsCaptured(par.ChildIdx))
            {
                var box = FlatNewBox(il, par.Type);
                il.Emit(OpCodes.Ldloc, box);
                il.Emit(OpCodes.Ldarg, (short)i);
                il.Emit(OpCodes.Stfld, FlatBoxType(par.Type).GetField("Value"));
                vars.Add(new FlatVar { Id = par.ChildIdx, Type = par.Type, Arg = -1, Local = box, Slot = -1, Boxed = true });
            }
            else
                vars.Add(new FlatVar { Id = par.ChildIdx, Type = par.Type, Arg = (short)i, Slot = -1 });
            parIdx = par.NextIdx;
        }

        var captures = ctx.FindLambda(lambdaIdx)?.Captures;
        for (var j = 0; captures != null && j < captures.Count; ++j)
            vars.Add(new FlatVar { Id = captures[j].Id, Type = captures[j].Type, Arg = -1, Slot = (short)j, Boxed = true });

        EmitFlat(ref tree, bodyIdx, il, ref vars, ctx, false);
        il.Emit(OpCodes.Ret);

        return delegateType != null
            ? dynMethod.CreateDelegate(delegateType, closure)
            : dynMethod.CreateDelegate(Tools.GetFuncOrActionType(paramTypes, returnType), null);
    }

    private static void EmitFlat(ref ExprTree tree, int idx, ILGenerator il,
        ref SmallList<FlatVar, Stack8<FlatVar>, NoArrayPool<FlatVar>> vars, FlatCtx ctx, bool ignoreResult)
    {
        ref var n = ref tree.Nodes.GetSurePresentRef(idx);
        var nodeType = n.NodeType;
        switch (nodeType)
        {
            case ExprType.Constant:
                if (ignoreResult)
                    return;
                if (ReferenceEquals(n.Obj, ExprNode.InlineValueMarker))
                    il.Emit(OpCodes.Ldc_I4, (int)n.InlineValue);
                else if (ReferenceEquals(n.Obj, ExprTree.ClosureConstantMarker))
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, ArrayClosureArrayField);
                    il.Emit(OpCodes.Ldc_I4, (int)n.ChildIdx);
                    il.Emit(OpCodes.Ldelem_Ref);
                    if (n.Type != typeof(object))
                        il.Emit(OpCodes.Castclass, n.Type);
                }
                else if (n.Obj is string s)
                    il.Emit(OpCodes.Ldstr, s);
                else
                    il.Emit(OpCodes.Ldnull);
                return;

            case ExprType.Parameter:
                {
                    if (ignoreResult)
                        return;
                    var v = FlatFindVar(ref vars, n.ChildIdx);
                    if (v.Boxed)
                    {
                        FlatLoadBox(il, in v);
                        il.Emit(OpCodes.Ldfld, FlatBoxType(v.Type).GetField("Value"));
                    }
                    else if (v.Arg >= 0) il.Emit(OpCodes.Ldarg, v.Arg); else il.Emit(OpCodes.Ldloc, v.Local);
                    return;
                }

            case ExprType.Assign:
                {
                    var leftIdx = n.ChildIdx;
                    ref var left = ref tree.Nodes.GetSurePresentRef(leftIdx);
                    var rightIdx = left.NextIdx;
                    var v = FlatFindVar(ref vars, left.ChildIdx);
                    if (v.Boxed)
                    {
                        FlatLoadBox(il, in v);
                        EmitFlat(ref tree, rightIdx, il, ref vars, ctx, false);
                        LocalBuilder result = null;
                        if (!ignoreResult)
                        {
                            il.Emit(OpCodes.Dup);
                            result = il.DeclareLocal(v.Type);
                            il.Emit(OpCodes.Stloc, result);
                        }
                        il.Emit(OpCodes.Stfld, FlatBoxType(v.Type).GetField("Value"));
                        if (result != null)
                            il.Emit(OpCodes.Ldloc, result);
                        return;
                    }
                    EmitFlat(ref tree, rightIdx, il, ref vars, ctx, false);
                    if (!ignoreResult)
                        il.Emit(OpCodes.Dup);
                    if (v.Arg >= 0) il.Emit(OpCodes.Starg, v.Arg); else il.Emit(OpCodes.Stloc, v.Local);
                    return;
                }

            case ExprType.Lambda:
                {
                    if (ignoreResult)
                        return;
                    // the nested lambda is the separate method; at run-time the open delegate is curried with the new closure
                    // made of the shared constants and the StrongBoxes of the used outer variables
                    var captures = ctx.FindLambda(idx).Captures;
                    var open = EmitFlatLambda(ref tree, idx, ctx, null, null);
                    var openType = open.GetType();
                    var slot = ctx.NextSlot++;
                    ctx.Constants[slot] = open;

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, ArrayClosureArrayField);
                    il.Emit(OpCodes.Ldc_I4, slot);
                    il.Emit(OpCodes.Ldelem_Ref);
                    il.Emit(OpCodes.Castclass, openType);

                    if (captures.Count != 0)
                    {
                        il.Emit(OpCodes.Ldc_I4, captures.Count);
                        il.Emit(OpCodes.Newarr, typeof(object));
                        for (var j = 0; j < captures.Count; ++j)
                        {
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Ldc_I4, j);
                            FlatLoadBox(il, FlatFindVar(ref vars, captures[j].Id));
                            il.Emit(OpCodes.Stelem_Ref);
                        }
                    }
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, ArrayClosureArrayField);
                    il.Emit(OpCodes.Newobj, captures.Count != 0 ? ArrayClosureWithNonPassedParamsAndConstantsCtor : FlatArrayClosureCtor);

                    var typeArgs = openType.GetGenericArguments();
                    il.Emit(OpCodes.Call, CurryClosureFuncs.Methods[typeArgs.Length - 2].MakeGenericMethod(typeArgs));
                    return;
                }

            case ExprType.New:
            case ExprType.Call:
                {
                    var argIdx = n.ChildIdx;
                    for (var i = 0; i < n.ChildCount; ++i)
                    {
                        EmitFlat(ref tree, argIdx, il, ref vars, ctx, false);
                        argIdx = tree.Nodes.GetSurePresentRef(argIdx).NextIdx;
                    }
                    if (nodeType == ExprType.New)
                        il.Emit(OpCodes.Newobj, (ConstructorInfo)n.Obj);
                    else
                    {
                        var method = (MethodInfo)n.Obj;
                        il.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, method);
                    }
                    break;
                }

            case ExprType.MemberAccess:
                {
                    if (n.ChildCount != 0)
                        EmitFlat(ref tree, n.ChildIdx, il, ref vars, ctx, false);
                    if (n.Obj is FieldInfo field)
                        il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
                    else
                    {
                        var getter = ((PropertyInfo)n.Obj).GetGetMethod(true);
                        il.Emit(getter.IsStatic ? OpCodes.Call : OpCodes.Callvirt, getter);
                    }
                    break;
                }

            case ExprType.Not:
                EmitFlat(ref tree, n.ChildIdx, il, ref vars, ctx, false);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ceq);
                break;

            case ExprType.Conditional:
                {
                    var testIdx = n.ChildIdx;
                    var ifTrueIdx = tree.Nodes.GetSurePresentRef(testIdx).NextIdx;
                    var ifFalseIdx = tree.Nodes.GetSurePresentRef(ifTrueIdx).NextIdx;
                    var elseLabel = il.DefineLabel();
                    var endLabel = il.DefineLabel();
                    EmitFlat(ref tree, testIdx, il, ref vars, ctx, false);
                    il.Emit(OpCodes.Brfalse, elseLabel);
                    EmitFlat(ref tree, ifTrueIdx, il, ref vars, ctx, ignoreResult);
                    il.Emit(OpCodes.Br, endLabel);
                    il.MarkLabel(elseLabel);
                    EmitFlat(ref tree, ifFalseIdx, il, ref vars, ctx, ignoreResult);
                    il.MarkLabel(endLabel);
                    return;
                }

            case ExprType.AndAlso:
            case ExprType.OrElse:
                {
                    var isAnd = nodeType == ExprType.AndAlso;
                    var rightIdx = tree.Nodes.GetSurePresentRef(n.ChildIdx).NextIdx;
                    var shortCircuit = il.DefineLabel();
                    var endLabel = il.DefineLabel();
                    EmitFlat(ref tree, n.ChildIdx, il, ref vars, ctx, false);
                    il.Emit(isAnd ? OpCodes.Brfalse : OpCodes.Brtrue, shortCircuit);
                    EmitFlat(ref tree, rightIdx, il, ref vars, ctx, false);
                    il.Emit(OpCodes.Br, endLabel);
                    il.MarkLabel(shortCircuit);
                    il.Emit(isAnd ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
                    il.MarkLabel(endLabel);
                    break;
                }

            case ExprType.Block:
                {
                    // layout: the first child is the BlockExprs list, the rest are the variables
                    var exprsIdx = n.ChildIdx;
                    ref var exprs = ref tree.Nodes.GetSurePresentRef(exprsIdx);
                    var varIdx = exprs.NextIdx;
                    var scopeStart = vars.Count;
                    for (var i = 1; i < n.ChildCount; ++i)
                    {
                        ref var v = ref tree.Nodes.GetSurePresentRef(varIdx);
                        if (ctx.IsCaptured(v.ChildIdx))
                            vars.Add(new FlatVar { Id = v.ChildIdx, Type = v.Type, Arg = -1, Local = FlatNewBox(il, v.Type), Slot = -1, Boxed = true });
                        else
                            vars.Add(new FlatVar { Id = v.ChildIdx, Type = v.Type, Arg = -1, Local = il.DeclareLocal(v.Type), Slot = -1 });
                        varIdx = v.NextIdx;
                    }

                    var last = exprs.ChildCount - 1;
                    var exprIdx = exprs.ChildIdx;
                    for (var i = 0; i <= last; ++i)
                    {
                        EmitFlat(ref tree, exprIdx, il, ref vars, ctx, i != last || ignoreResult);
                        exprIdx = tree.Nodes.GetSurePresentRef(exprIdx).NextIdx;
                    }
                    vars.Count = scopeStart; // the block variables go out of the scope
                    return;
                }

            default: // the binary int/bool operations
                {
                    EmitFlat(ref tree, n.ChildIdx, il, ref vars, ctx, false);
                    EmitFlat(ref tree, tree.Nodes.GetSurePresentRef(n.ChildIdx).NextIdx, il, ref vars, ctx, false);
                    switch (nodeType)
                    {
                        case ExprType.Add: il.Emit(OpCodes.Add); break;
                        case ExprType.Subtract: il.Emit(OpCodes.Sub); break;
                        case ExprType.Multiply: il.Emit(OpCodes.Mul); break;
                        case ExprType.Equal: il.Emit(OpCodes.Ceq); break;
                        default: il.Emit(OpCodes.Cgt); break; // GreaterThan, the rest is rejected by the collect
                    }
                    break;
                }
        }

        if (ignoreResult)
            il.Emit(OpCodes.Pop);
    }
}

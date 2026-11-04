// Experimental: the initial compilation path for the ExprTree, to learn how much of FEC could work on the flat nodes directly.
#nullable disable
namespace FastExpressionCompiler.LightExpression;

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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
        public int Local; // the local index (no LocalBuilder allocated), or the local StrongBox if Boxed
        public short Slot; // the item of the closure NonPassedParams if the variable is captured from the outer lambda, or -1
        public bool Boxed; // the variable is assigned and used by a nested lambda, so it lives in the StrongBox shared with that lambda
    }

    /// <summary>The outer variable used by the nested lambda. The closure NonPassedParams of the lambda are its captures in the list order.
    /// ByValue: the variable is never assigned, so the NonPassedParams item is the (boxed) value, otherwise it is the StrongBox.</summary>
    private struct FlatCapture
    {
        public int LambdaIdx;
        public ushort Id;
        public Type Type;
        public bool ByValue;
    }

    /// <summary>The state of one compilation, a local of the TryCompileFlat passed by ref.</summary>
    private struct FlatCtx
    {
        public object[] Constants; // the closure constants, then the open delegates of the nested lambdas
        public int NextSlot;
        public SmallList<FlatCapture, Stack8<FlatCapture>, NoArrayPool<FlatCapture>> Captures;

        public bool IsBoxed(ushort id)
        {
            for (var i = 0; i < Captures.Count; ++i)
            {
                ref var c = ref Captures.GetSurePresentRef(i);
                if (c.Id == id && !c.ByValue) return true;
            }
            return false;
        }
    }

    private static readonly ConstructorInfo FlatArrayClosureCtor = typeof(ArrayClosure).GetConstructors()[0];

    /// <summary>Finds which variables are used by the lambdas other than the declaring one. Recursive but only run for the nested lambdas.
    /// The variable is declared by the lambda parameter or the block variable, the usage is the Parameter node with the same id.</summary>
    private static void FlatFindCaptures(ref ExprTree tree, int idx, ref FlatCtx ctx,
        ref SmallList<(ushort Id, int Depth), Stack8<(ushort Id, int Depth)>, NoArrayPool<(ushort Id, int Depth)>> declared,
        ref SmallList<int, Stack8<int>, NoArrayPool<int>> lambdas)
    {
        ref var n = ref tree.Nodes.GetSurePresentRef(idx);
        switch (n.NodeType)
        {
            case ExprType.Constant:
                return;

            case ExprType.Parameter:
                {
                    var d = declared.Count - 1;
                    while (d >= 0 && declared.GetSurePresentRef(d).Id != n.ChildIdx) --d;
                    if (d < 0)
                        return; // not in scope, the emit throws
                    // the lambdas deeper than the declaring one are capturing it
                    for (var k = declared.GetSurePresentRef(d).Depth; k < lambdas.Count; ++k)
                    {
                        var lambdaIdx = lambdas.GetSurePresentRef(k);
                        var found = false;
                        for (var c = 0; c < ctx.Captures.Count && !found; ++c)
                        {
                            ref var cap = ref ctx.Captures.GetSurePresentRef(c);
                            found = cap.LambdaIdx == lambdaIdx && cap.Id == n.ChildIdx;
                        }
                        if (!found)
                            ctx.Captures.Add(new FlatCapture { LambdaIdx = lambdaIdx, Id = n.ChildIdx, Type = n.Type, ByValue = !tree.IsAssigned(n.ChildIdx) });
                    }
                    return;
                }

            case ExprType.Lambda:
                {
                    lambdas.Add(idx);
                    var scope = declared.Count;
                    var bodyIdx = n.ChildIdx;
                    var parIdx = tree.Nodes.GetSurePresentRef(bodyIdx).NextIdx;
                    for (var i = 1; i < n.ChildCount; ++i)
                    {
                        ref var par = ref tree.Nodes.GetSurePresentRef(parIdx);
                        declared.Add((par.ChildIdx, lambdas.Count));
                        parIdx = par.NextIdx;
                    }
                    FlatFindCaptures(ref tree, bodyIdx, ref ctx, ref declared, ref lambdas);
                    declared.Count = scope;
                    --lambdas.Count;
                    return;
                }

            case ExprType.Block:
                {
                    ref var exprs = ref tree.Nodes.GetSurePresentRef(n.ChildIdx);
                    var scope = declared.Count;
                    var varIdx = exprs.NextIdx;
                    for (var i = 1; i < n.ChildCount; ++i)
                    {
                        ref var v = ref tree.Nodes.GetSurePresentRef(varIdx);
                        declared.Add((v.ChildIdx, lambdas.Count));
                        varIdx = v.NextIdx;
                    }
                    var exprIdx = exprs.ChildIdx;
                    for (var i = 0; i < exprs.ChildCount; ++i)
                    {
                        FlatFindCaptures(ref tree, exprIdx, ref ctx, ref declared, ref lambdas);
                        exprIdx = tree.Nodes.GetSurePresentRef(exprIdx).NextIdx;
                    }
                    declared.Count = scope;
                    return;
                }

            default:
                {
                    var childIdx = n.ChildIdx;
                    for (var i = 0; i < n.ChildCount; ++i)
                    {
                        FlatFindCaptures(ref tree, childIdx, ref ctx, ref declared, ref lambdas);
                        childIdx = tree.Nodes.GetSurePresentRef(childIdx).NextIdx;
                    }
                    return;
                }
        }
    }

    /// <summary>The delegate type facts, found once per type: the reflection (Invoke, the Func types, the Curry method) is the biggest
    /// part of the compile of the small lambda. The open delegate and the Curry are only for the nested lambda: the Func (the closure
    /// is curried into it) with the supported number of parameters, otherwise they are null.</summary>
    private sealed class FlatDelegateInfo
    {
        public Type ReturnType;
        public Type[] ParamTypes;
        public Type[] ClosureParamTypes; // the closure then the parameters, for the DynamicMethod
        public Type OpenType; // Func<closure, params, return>
        public MethodInfo CurryMethod;
    }

    // the cache keeps the delegate types alive, same as the other FEC type caches
    private static readonly ConcurrentDictionary<Type, FlatDelegateInfo> FlatDelegates = new();

    private static FlatDelegateInfo FlatGetDelegateInfo(Type delegateType)
    {
        if (FlatDelegates.TryGetValue(delegateType, out var info))
            return info;

        var invoke = delegateType.GetMethod("Invoke");
        if (invoke != null)
        {
            var pars = invoke.GetParameters();
            info = new FlatDelegateInfo { ReturnType = invoke.ReturnType, ParamTypes = new Type[pars.Length], ClosureParamTypes = new Type[pars.Length + 1] };
            info.ClosureParamTypes[0] = typeof(ArrayClosure);
            for (var i = 0; i < pars.Length; ++i)
                info.ParamTypes[i] = info.ClosureParamTypes[i + 1] = pars[i].ParameterType;

            if (pars.Length <= 5 && info.ReturnType != typeof(void) && Tools.GetFuncOrActionType(info.ParamTypes, info.ReturnType) == delegateType) // 5 is the CurryClosureFuncs max
            {
                info.OpenType = Tools.GetFuncOrActionType(info.ClosureParamTypes, info.ReturnType);
                info.CurryMethod = CurryClosureFuncs.Methods[pars.Length].MakeGenericMethod(info.OpenType.GetGenericArguments());
            }
        }
        return FlatDelegates.GetOrAdd(delegateType, info);
    }

    /// <summary>The lambda return and parameter types must be flat and be the same as the delegate has.</summary>
    private static bool FlatLambdaFits(ref ExprTree tree, ref ExprNode lambda, bool nested)
    {
        var info = FlatGetDelegateInfo(lambda.Type);
        if (info == null)
            return false;
        ref var body = ref tree.Nodes.GetSurePresentRef(lambda.ChildIdx);
        if (!IsFlatType(info.ReturnType) || !FlatFits(info.ReturnType, body.Type) || info.ParamTypes.Length != lambda.ChildCount - 1)
            return false;

        var parIdx = body.NextIdx;
        for (var i = 0; i < info.ParamTypes.Length; ++i)
        {
            ref var par = ref tree.Nodes.GetSurePresentRef(parIdx);
            if (par.Type != info.ParamTypes[i])
                return false;
            parIdx = par.NextIdx;
        }
        return !nested || info.OpenType != null;
    }

    private sealed class FlatBoxInfo { public Type Type; public ConstructorInfo Ctor; public FieldInfo Value; }

    private static readonly ConcurrentDictionary<Type, FlatBoxInfo> FlatBoxes = new();

    private static FlatBoxInfo FlatBox(Type t)
    {
        if (FlatBoxes.TryGetValue(t, out var info))
            return info;
        var boxType = typeof(StrongBox<>).MakeGenericType(t);
        return FlatBoxes.GetOrAdd(t, new FlatBoxInfo { Type = boxType, Ctor = boxType.GetConstructor(Type.EmptyTypes), Value = boxType.GetField("Value") });
    }

    /// <summary>Creates the StrongBox and stores it into the new local, returns the local index.</summary>
    private static int FlatNewBox(ILGenerator il, Type t)
    {
        var info = FlatBox(t);
        il.Emit(OpCodes.Newobj, info.Ctor);
        return EmittingVisitor.EmitStoreLocalVariable(il, info.Type);
    }

    /// <summary>Loads the item of the closure NonPassedParams: the StrongBox or the boxed value of the captured variable.</summary>
    private static void FlatLoadNonPassed(ILGenerator il, int slot)
    {
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, ArrayClosureWithNonPassedParamsField);
        EmittingVisitor.EmitLoadConstantInt(il, slot);
        il.Emit(OpCodes.Ldelem_Ref);
    }

    private static void FlatLoadBox(ILGenerator il, in FlatVar v)
    {
        if (v.Slot < 0)
        {
            EmittingVisitor.EmitLoadLocalVariable(il, v.Local);
            return;
        }
        FlatLoadNonPassed(il, v.Slot);
        il.Emit(OpCodes.Castclass, FlatBox(v.Type).Type);
    }

    private static void FlatLoadVar(ILGenerator il, in FlatVar v)
    {
        if (v.Boxed)
        {
            FlatLoadBox(il, in v);
            il.Emit(OpCodes.Ldfld, FlatBox(v.Type).Value);
        }
        else if (v.Slot >= 0)
        {
            FlatLoadNonPassed(il, v.Slot);
            il.Emit(OpCodes.Unbox_Any, v.Type);
        }
        else if (v.Arg >= 0) il.Emit(OpCodes.Ldarg, v.Arg);
        else EmittingVisitor.EmitLoadLocalVariable(il, v.Local);
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

#if NET9_0_OR_GREATER
    /// <summary>The Flat view for the shared <see cref="EmittingVisitor.EmitListInit{TView, TCtx}"/>: the New node is the first child,
    /// then the ElementInit nodes (the Add method in Obj, the arguments as the children). The cursor walks them in the order the skeleton asks.</summary>
    private ref struct FlatListInitView : EmittingVisitor.IListInitView<FlatCtx>
    {
        public ref ExprTree Tree;
        public ref SmallList<FlatVar, Stack8<FlatVar>, NoArrayPool<FlatVar>> Vars;
        public int NewIdx;
        private int _initIdx; // 0 until the first Add call is asked for

        public Type Type => Tree.Nodes.GetSurePresentRef(NewIdx).Type;
        public ConstructorInfo Constructor => (ConstructorInfo)Tree.Nodes.GetSurePresentRef(NewIdx).Obj;
        public int InitCount { get; set; }

        public MethodInfo GetAddMethod(int i)
        {
            if (i == 0)
                _initIdx = Tree.Nodes.GetSurePresentRef(NewIdx).NextIdx;
            return (MethodInfo)Tree.Nodes.GetSurePresentRef(_initIdx).Obj;
        }

        [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
        public bool TryEmitNewArgs(ILGenerator il, ref FlatCtx ctx)
        {
            ref var newNode = ref Tree.Nodes.GetSurePresentRef(NewIdx);
            var argIdx = newNode.ChildIdx;
            for (var i = 0; i < newNode.ChildCount; ++i)
            {
                EmitFlat(ref Tree, argIdx, il, ref Vars, ref ctx, false);
                argIdx = Tree.Nodes.GetSurePresentRef(argIdx).NextIdx;
            }
            return true;
        }

        [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
        public bool TryEmitAddArgs(int i, ILGenerator il, ref FlatCtx ctx, ParameterInfo[] addParams)
        {
            ref var init = ref Tree.Nodes.GetSurePresentRef(_initIdx);
            var argIdx = init.ChildIdx;
            for (var a = 0; a < init.ChildCount; ++a)
            {
                EmitFlat(ref Tree, argIdx, il, ref Vars, ref ctx, false);
                argIdx = Tree.Nodes.GetSurePresentRef(argIdx).NextIdx;
            }
            _initIdx = init.NextIdx;
            return true;
        }
    }
#endif

    private static bool IsFlatType(Type t) => t == typeof(int) || t == typeof(bool) || !t.IsValueType && !t.IsByRef && !t.IsPointer;

    // the lambda and delegate return/parameter types are checked once per compile, not per node
    private static bool FlatFits(Type to, Type from) => to == from || !to.IsValueType && !from.IsValueType && to.IsAssignableFrom(from);

    /// <summary>The collect phase on the flat nodes: one linear pass over the contiguous array, no recursion, no stack, no per-node casts.
    /// What the tree construction already guarantees is not checked here: the child counts, the not-null children, the parameter
    /// and label ids, the closure constants (they are in the separate list, so `IsClosureBoundConstant` is already answered).
    /// Returns `OK` if the tree is the int/bool subset which <see cref="TryCompileFlat{TDelegate}"/> can emit.</summary>
    public static Result TryCollectFlatInfo(this ref ExprTree tree) => tree.CollectFlat();

    private static Result CollectFlat(this ref ExprTree tree)
    {
        var count = tree.Nodes.Count;
        if (count < 2)
            return Result.NotSupported_UnknownExpression;

        for (var i = 1; i < count; ++i)
        {
            ref var n = ref tree.Nodes.GetSurePresentRef(i);
            if (n.Kind == ExprNodeKind.BlockExprs)
                continue;
#if NET9_0_OR_GREATER
            if (n.Kind == ExprNodeKind.ElementInit)
            {
                if (n.Obj is MethodInfo am && !am.IsStatic && !am.IsGenericMethodDefinition)
                    continue;
                return Result.NotSupported_UnknownExpression;
            }
#endif
            if (n.Kind == ExprNodeKind.MemberAssignment)
            {
                // the instance field or the property with the setter, the binding is emitted by the MemberInit
                if (!IsFlatType(n.Type) || !(n.Obj is FieldInfo bf && !bf.IsInitOnly && !bf.IsLiteral && !bf.IsStatic ||
                    n.Obj is PropertyInfo bp && bp.GetSetMethod(true) is { IsStatic: false }))
                    return Result.NotSupported_UnknownExpression;
                continue;
            }
            if (n.Kind != ExprNodeKind.Expression)
                return Result.NotSupported_UnknownExpression;

            var t = n.Type;
            switch (n.NodeType)
            {
                case ExprType.MemberInit:
#if NET9_0_OR_GREATER
                case ExprType.ListInit: // emitted by the shared EmittingVisitor.EmitListInit through the FlatListInitView
#endif
                    if (!IsFlatType(t)) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.NewArrayInit:
                    if (!t.IsArray || !IsFlatType(t.GetElementType())) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.ArrayIndex:
                    {
                        ref var arr = ref tree.Nodes.GetSurePresentRef(n.ChildIdx);
                        if (!arr.Type.IsArray || arr.Type.GetArrayRank() != 1 || !IsFlatType(t) ||
                            tree.Nodes.GetSurePresentRef(arr.NextIdx).Type != typeof(int))
                            return Result.NotSupported_UnknownExpression;
                        break;
                    }
                case ExprType.Convert:
                    // the reference cast only, no user-defined conversion method, no boxing and no numeric conversions
                    if (n.Obj != null || t.IsValueType || !IsFlatType(t) || tree.Nodes.GetSurePresentRef(n.ChildIdx).Type.IsValueType)
                        return Result.NotSupported_UnknownExpression;
                    break;
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
                    // by-ref parameters are rejected here and Assign is the only emitted mutation, so `AssignedParams` is the complete list for the by-value captures
                    if (!IsFlatType(t) || n.Flags != 0) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.New:
                    // the arguments are not type-checked, same as the Light: the factories did that
                    if (!IsFlatType(t) || n.Obj is not ConstructorInfo) return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.Call:
                    // the instance of the value type needs the address and the constrained call, so the Light handles it
                    if (!IsFlatType(t) || n.Obj is not MethodInfo m || m.IsGenericMethodDefinition ||
                        !m.IsStatic && tree.Nodes.GetSurePresentRef(n.ChildIdx).Type.IsValueType)
                        return Result.NotSupported_UnknownExpression;
                    break;
                case ExprType.MemberAccess:
                    if (!IsFlatType(t) || !(n.Obj is FieldInfo f && !f.IsLiteral || n.Obj is PropertyInfo p && p.CanRead) ||
                        n.ChildCount != 0 && tree.Nodes.GetSurePresentRef(n.ChildIdx).Type.IsValueType)
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
                    if (!FlatLambdaFits(ref tree, ref n, i != tree.RootIdx)) return Result.NotSupported_UnknownExpression;
                    break;
                default:
                    return Result.NotSupported_UnknownExpression;
            }
        }

        return tree.LambdaCount != 0 && tree.Nodes.GetSurePresentRef(tree.RootIdx).NodeType == ExprType.Lambda ? Result.OK : Result.NotSupported_UnknownExpression;
    }

    /// <summary>Folds the tree of the inline int/bool constants and the operations over them (the bool as 0/1). False if anything else is inside.</summary>
    private static bool FlatFold(ref ExprTree tree, int idx, out int result)
    {
        ref var n = ref tree.Nodes.GetSurePresentRef(idx);
        result = 0;
        if (n.Kind != ExprNodeKind.Expression)
            return false;
        if (n.NodeType == ExprType.Constant)
        {
            if (!ReferenceEquals(n.Obj, ExprNode.InlineValueMarker))
                return false;
            result = (int)n.InlineValue;
            return true;
        }
        if (n.NodeType == ExprType.Not)
        {
            if (!FlatFold(ref tree, n.ChildIdx, out var v))
                return false;
            result = v == 0 ? 1 : 0;
            return true;
        }
        if (n.NodeType == ExprType.Conditional)
        {
            var ifTrueIdx = tree.Nodes.GetSurePresentRef(n.ChildIdx).NextIdx;
            var ifFalseIdx = tree.Nodes.GetSurePresentRef(ifTrueIdx).NextIdx;
            return FlatFold(ref tree, n.ChildIdx, out var test) && FlatFold(ref tree, test != 0 ? ifTrueIdx : ifFalseIdx, out result);
        }

        var nodeType = n.NodeType;
        if (nodeType != ExprType.AndAlso && nodeType != ExprType.OrElse && nodeType != ExprType.Add && nodeType != ExprType.Subtract &&
            nodeType != ExprType.Multiply && nodeType != ExprType.Equal && nodeType != ExprType.GreaterThan)
            return false;
        if (!FlatFold(ref tree, n.ChildIdx, out var l))
            return false;
        var rightIdx = tree.Nodes.GetSurePresentRef(n.ChildIdx).NextIdx;
        if (nodeType == ExprType.AndAlso && l == 0 || nodeType == ExprType.OrElse && l != 0)
        {
            result = l; // short-circuit, the right is not even looked at, same as the emit
            return true;
        }
        if (!FlatFold(ref tree, rightIdx, out var r))
            return false;
        result = nodeType switch
        {
            ExprType.Add => l + r,
            ExprType.Subtract => l - r,
            ExprType.Multiply => l * r,
            ExprType.Equal => l == r ? 1 : 0,
            ExprType.GreaterThan => l > r ? 1 : 0,
            _ => r // AndAlso / OrElse with the left not short-circuiting
        };
        return true;
    }

    /// <summary>Returns null if the tree is not the supported subset or the delegate type is not the lambda type.</summary>
    public static TDelegate TryCompileFlat<TDelegate>(this ref ExprTree tree) where TDelegate : class
    {
        if (tree.CollectFlat() != Result.OK)
            return null;

        if (tree.Nodes.GetSurePresentRef(tree.RootIdx).Type != typeof(TDelegate))
            return null;

        // the constant bool lambda needs no DynamicMethod at all, same as FEC does with its Interpreter
        if (typeof(TDelegate) == typeof(Func<bool>) && FlatFold(ref tree, tree.Nodes.GetSurePresentRef(tree.RootIdx).ChildIdx, out var folded))
            return (TDelegate)(object)(folded != 0 ? ExpressionCompiler.Interpreter.TrueFunc : ExpressionCompiler.Interpreter.FalseFunc);

        var constantCount = tree.ClosureConstants.Count;
        var ctx = new FlatCtx { NextSlot = constantCount };
        var slotCount = constantCount + tree.LambdaCount - 1; // the slot per nested lambda for its open delegate
        if (slotCount != 0)
        {
            ctx.Constants = new object[slotCount];
            for (var i = 0; i < constantCount; ++i)
                ctx.Constants[i] = tree.ClosureConstants[i];
        }

        if (tree.LambdaCount > 1)
        {
            var declared = default(SmallList<(ushort Id, int Depth), Stack8<(ushort Id, int Depth)>, NoArrayPool<(ushort Id, int Depth)>>);
            var lambdas = default(SmallList<int, Stack8<int>, NoArrayPool<int>>);
            FlatFindCaptures(ref tree, tree.RootIdx, ref ctx, ref declared, ref lambdas);
        }

        var closure = ctx.Constants == null ? EmptyArrayClosure : new ArrayClosure(ctx.Constants);
        return (TDelegate)(object)EmitFlatLambda(ref tree, tree.RootIdx, ref ctx, typeof(TDelegate), closure);
    }

    /// <summary>Emits the lambda into its own DynamicMethod. The root is closed over the closure; the nested one
    /// (the delegate type is null) becomes the open delegate with the closure as the first parameter, to be curried at run-time.</summary>
    private static Delegate EmitFlatLambda(ref ExprTree tree, int lambdaIdx, ref FlatCtx ctx, Type delegateType, object closure)
    {
        ref var lambda = ref tree.Nodes.GetSurePresentRef(lambdaIdx);
        var bodyIdx = lambda.ChildIdx;
        var info = FlatGetDelegateInfo(lambda.Type);
        var paramCount = info.ParamTypes.Length;
        var dynMethod = new DynamicMethod(string.Empty, info.ReturnType, info.ClosureParamTypes, typeof(ArrayClosure), true);
        var il = DynamicMethodHacks.RentPooledOrNewILGenerator(dynMethod, info.ReturnType, info.ClosureParamTypes);

        var vars = default(SmallList<FlatVar, Stack8<FlatVar>, NoArrayPool<FlatVar>>);
        var parIdx = tree.Nodes.GetSurePresentRef(bodyIdx).NextIdx;
        for (var i = 1; i <= paramCount; ++i)
        {
            ref var par = ref tree.Nodes.GetSurePresentRef(parIdx);
            if (ctx.IsBoxed(par.ChildIdx))
            {
                var box = FlatNewBox(il, par.Type);
                EmittingVisitor.EmitLoadLocalVariable(il, box);
                il.Emit(OpCodes.Ldarg, (short)i);
                il.Emit(OpCodes.Stfld, FlatBox(par.Type).Value);
                vars.Add(new FlatVar { Id = par.ChildIdx, Type = par.Type, Arg = -1, Local = box, Slot = -1, Boxed = true });
            }
            else
                vars.Add(new FlatVar { Id = par.ChildIdx, Type = par.Type, Arg = (short)i, Slot = -1 });
            parIdx = par.NextIdx;
        }

        for (int c = 0, slot = 0; c < ctx.Captures.Count; ++c)
        {
            ref var cap = ref ctx.Captures.GetSurePresentRef(c);
            if (cap.LambdaIdx == lambdaIdx)
                vars.Add(new FlatVar { Id = cap.Id, Type = cap.Type, Arg = -1, Slot = (short)slot++, Boxed = !cap.ByValue });
        }
        EmitFlat(ref tree, bodyIdx, il, ref vars, ref ctx, false);
        il.Emit(OpCodes.Ret);

        var result = dynMethod.CreateDelegate(delegateType ?? info.OpenType, closure);
        DynamicMethodHacks.FreePooledILGenerator(dynMethod, il); // only after the CreateDelegate, as FEC does
        return result;
    }

    private static void EmitFlat(ref ExprTree tree, int idx, ILGenerator il,
        ref SmallList<FlatVar, Stack8<FlatVar>, NoArrayPool<FlatVar>> vars, ref FlatCtx ctx, bool ignoreResult)
    {
        ref var n = ref tree.Nodes.GetSurePresentRef(idx);
        var nodeType = n.NodeType;
        switch (nodeType)
        {
            case ExprType.Constant:
                if (ignoreResult)
                    return;
                if (ReferenceEquals(n.Obj, ExprNode.InlineValueMarker))
                    EmittingVisitor.EmitLoadConstantInt(il, (int)n.InlineValue);
                else if (ReferenceEquals(n.Obj, ExprTree.ClosureConstantMarker))
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, ArrayClosureArrayField);
                    EmittingVisitor.EmitLoadConstantInt(il, n.ChildIdx);
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
                if (ignoreResult)
                    return;
                FlatLoadVar(il, FlatFindVar(ref vars, n.ChildIdx));
                return;

            case ExprType.Assign:
                {
                    var leftIdx = n.ChildIdx;
                    ref var left = ref tree.Nodes.GetSurePresentRef(leftIdx);
                    var rightIdx = left.NextIdx;
                    var v = FlatFindVar(ref vars, left.ChildIdx);
                    if (v.Boxed)
                    {
                        FlatLoadBox(il, in v);
                        EmitFlat(ref tree, rightIdx, il, ref vars, ref ctx, false);
                        var result = -1;
                        if (!ignoreResult)
                        {
                            il.Emit(OpCodes.Dup);
                            result = EmittingVisitor.EmitStoreLocalVariable(il, v.Type);
                        }
                        il.Emit(OpCodes.Stfld, FlatBox(v.Type).Value);
                        if (result != -1)
                            EmittingVisitor.EmitLoadLocalVariable(il, result);
                        return;
                    }
                    EmitFlat(ref tree, rightIdx, il, ref vars, ref ctx, false);
                    if (!ignoreResult)
                        il.Emit(OpCodes.Dup);
                    if (v.Arg >= 0) il.Emit(OpCodes.Starg, v.Arg); else EmittingVisitor.EmitStoreLocalVariable(il, v.Local);
                    return;
                }

            case ExprType.Lambda:
                {
                    if (ignoreResult)
                        return;
                    // the nested lambda is the separate method; at run-time the open delegate is curried with the new closure
                    // made of the shared constants and the captured variables: the StrongBox, or the boxed value if never assigned
                    var info = FlatGetDelegateInfo(n.Type);
                    var open = EmitFlatLambda(ref tree, idx, ref ctx, null, null);
                    var slot = ctx.NextSlot++;
                    ctx.Constants[slot] = open;

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, ArrayClosureArrayField);
                    EmittingVisitor.EmitLoadConstantInt(il, slot);
                    il.Emit(OpCodes.Ldelem_Ref);
                    il.Emit(OpCodes.Castclass, info.OpenType);

                    var captureCount = 0;
                    for (var c = 0; c < ctx.Captures.Count; ++c)
                        if (ctx.Captures.GetSurePresentRef(c).LambdaIdx == idx) ++captureCount;

                    if (captureCount != 0)
                    {
                        EmittingVisitor.EmitLoadConstantInt(il, captureCount);
                        il.Emit(OpCodes.Newarr, typeof(object));
                        for (int c = 0, j = 0; c < ctx.Captures.Count; ++c)
                        {
                            ref var cap = ref ctx.Captures.GetSurePresentRef(c);
                            if (cap.LambdaIdx != idx)
                                continue;
                            il.Emit(OpCodes.Dup);
                            EmittingVisitor.EmitLoadConstantInt(il, j++);
                            var v = FlatFindVar(ref vars, cap.Id);
                            if (v.Slot >= 0) // captured by the outer lambda too, pass the item through
                                FlatLoadNonPassed(il, v.Slot);
                            else if (v.Boxed)
                                FlatLoadBox(il, in v);
                            else
                            {
                                FlatLoadVar(il, in v);
                                if (v.Type.IsValueType)
                                    il.Emit(OpCodes.Box, v.Type);
                            }
                            il.Emit(OpCodes.Stelem_Ref);
                        }
                    }
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, ArrayClosureArrayField);
                    il.Emit(OpCodes.Newobj, captureCount != 0 ? ArrayClosureWithNonPassedParamsAndConstantsCtor : FlatArrayClosureCtor);

                    il.Emit(OpCodes.Call, info.CurryMethod);
                    return;
                }

            case ExprType.New:
            case ExprType.Call:
                {
                    var argIdx = n.ChildIdx;
                    for (var i = 0; i < n.ChildCount; ++i)
                    {
                        EmitFlat(ref tree, argIdx, il, ref vars, ref ctx, false);
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
                        EmitFlat(ref tree, n.ChildIdx, il, ref vars, ref ctx, false);
                    if (n.Obj is FieldInfo field)
                        il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
                    else
                    {
                        var getter = ((PropertyInfo)n.Obj).GetGetMethod(true);
                        il.Emit(getter.IsStatic ? OpCodes.Call : OpCodes.Callvirt, getter);
                    }
                    break;
                }

            case ExprType.MemberInit:
                {
                    EmitFlat(ref tree, n.ChildIdx, il, ref vars, ref ctx, false);
                    var bindingIdx = tree.Nodes.GetSurePresentRef(n.ChildIdx).NextIdx;
                    for (var i = 1; i < n.ChildCount; ++i)
                    {
                        ref var binding = ref tree.Nodes.GetSurePresentRef(bindingIdx);
                        il.Emit(OpCodes.Dup);
                        EmitFlat(ref tree, binding.ChildIdx, il, ref vars, ref ctx, false);
                        if (binding.Obj is FieldInfo field)
                            il.Emit(OpCodes.Stfld, field);
                        else
                            il.Emit(OpCodes.Callvirt, ((PropertyInfo)binding.Obj).GetSetMethod(true));
                        bindingIdx = binding.NextIdx;
                    }
                    break;
                }

#if NET9_0_OR_GREATER
            case ExprType.ListInit:
                {
                    var view = new FlatListInitView { Tree = ref tree, Vars = ref vars, NewIdx = n.ChildIdx, InitCount = n.ChildCount - 1 };
                    if (!EmittingVisitor.EmitListInit(ref view, il, ref ctx))
                        throw new InvalidOperationException("The collect accepted the ListInit which cannot be emitted");
                    break;
                }
#endif

            case ExprType.NewArrayInit:
                {
                    var elemType = n.Type.GetElementType();
                    EmittingVisitor.EmitLoadConstantInt(il, n.ChildCount);
                    il.Emit(OpCodes.Newarr, elemType);
                    var elemIdx = n.ChildIdx;
                    for (var i = 0; i < n.ChildCount; ++i)
                    {
                        il.Emit(OpCodes.Dup);
                        EmittingVisitor.EmitLoadConstantInt(il, i);
                        EmitFlat(ref tree, elemIdx, il, ref vars, ref ctx, false);
                        il.Emit(elemType == typeof(int) ? OpCodes.Stelem_I4 : elemType == typeof(bool) ? OpCodes.Stelem_I1 : OpCodes.Stelem_Ref);
                        elemIdx = tree.Nodes.GetSurePresentRef(elemIdx).NextIdx;
                    }
                    break;
                }

            case ExprType.ArrayIndex:
                {
                    EmitFlat(ref tree, n.ChildIdx, il, ref vars, ref ctx, false);
                    EmitFlat(ref tree, tree.Nodes.GetSurePresentRef(n.ChildIdx).NextIdx, il, ref vars, ref ctx, false);
                    il.Emit(n.Type == typeof(int) ? OpCodes.Ldelem_I4 : n.Type == typeof(bool) ? OpCodes.Ldelem_U1 : OpCodes.Ldelem_Ref);
                    break;
                }

            case ExprType.Convert:
                {
                    EmitFlat(ref tree, n.ChildIdx, il, ref vars, ref ctx, false);
                    if (!n.Type.IsAssignableFrom(tree.Nodes.GetSurePresentRef(n.ChildIdx).Type))
                        il.Emit(OpCodes.Castclass, n.Type);
                    break;
                }

            case ExprType.Not:
                EmitFlat(ref tree, n.ChildIdx, il, ref vars, ref ctx, false);
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
                    EmitFlat(ref tree, testIdx, il, ref vars, ref ctx, false);
                    il.Emit(OpCodes.Brfalse, elseLabel);
                    EmitFlat(ref tree, ifTrueIdx, il, ref vars, ref ctx, ignoreResult);
                    il.Emit(OpCodes.Br, endLabel);
                    il.MarkLabel(elseLabel);
                    EmitFlat(ref tree, ifFalseIdx, il, ref vars, ref ctx, ignoreResult);
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
                    EmitFlat(ref tree, n.ChildIdx, il, ref vars, ref ctx, false);
                    il.Emit(isAnd ? OpCodes.Brfalse : OpCodes.Brtrue, shortCircuit);
                    EmitFlat(ref tree, rightIdx, il, ref vars, ref ctx, false);
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
                        if (ctx.IsBoxed(v.ChildIdx))
                            vars.Add(new FlatVar { Id = v.ChildIdx, Type = v.Type, Arg = -1, Local = FlatNewBox(il, v.Type), Slot = -1, Boxed = true });
                        else
                            vars.Add(new FlatVar { Id = v.ChildIdx, Type = v.Type, Arg = -1, Local = il.GetNextLocalVarIndex(v.Type), Slot = -1 });
                        varIdx = v.NextIdx;
                    }

                    var last = exprs.ChildCount - 1;
                    var exprIdx = exprs.ChildIdx;
                    for (var i = 0; i <= last; ++i)
                    {
                        EmitFlat(ref tree, exprIdx, il, ref vars, ref ctx, i != last || ignoreResult);
                        exprIdx = tree.Nodes.GetSurePresentRef(exprIdx).NextIdx;
                    }
                    vars.Count = scopeStart; // the block variables go out of the scope
                    return;
                }

            default: // the binary int/bool operations
                {
                    EmitFlat(ref tree, n.ChildIdx, il, ref vars, ref ctx, false);
                    EmitFlat(ref tree, tree.Nodes.GetSurePresentRef(n.ChildIdx).NextIdx, il, ref vars, ref ctx, false);
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

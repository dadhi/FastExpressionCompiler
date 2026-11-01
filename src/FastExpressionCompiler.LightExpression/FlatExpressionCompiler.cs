// Experimental: the initial compilation path for the ExprTree, to learn how much of FEC could work on the flat nodes directly.
#nullable disable
namespace FastExpressionCompiler.LightExpression;

using System;
using System.Reflection;
using System.Reflection.Emit;
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
        public short Arg; // the argument index (the closure is 0) or -1 for the local
        public LocalBuilder Local;
    }

    private static bool IsFlatType(Type t) => t == typeof(int) || t == typeof(bool) || !t.IsValueType && !t.IsByRef && !t.IsPointer;

    // the lambda and delegate return/parameter types are checked once per compile, not per node
    private static bool FlatFits(Type to, Type from) => to == from || !to.IsValueType && !from.IsValueType && to.IsAssignableFrom(from);

    /// <summary>The collect phase on the flat nodes: one linear pass over the contiguous array, no recursion, no stack, no per-node casts.
    /// What the tree construction already guarantees is not checked here: the child counts, the not-null children, the parameter
    /// and label ids, the closure constants (they are in the separate list, so `IsClosureBoundConstant` is already answered).
    /// Returns `OK` if the tree is the int/bool subset which <see cref="TryCompileFlat{TDelegate}"/> can emit.</summary>
    public static Result TryCollectFlatInfo(this ref ExprTree tree)
    {
        var count = tree.Nodes.Count;
        if (count < 2)
            return Result.NotSupported_UnknownExpression;

        var lambdas = 0;
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
                    if (++lambdas != 1) return Result.NotSupported_UnknownExpression; // the nested lambdas need the closure
                    break;
                default:
                    return Result.NotSupported_UnknownExpression;
            }
        }

        return lambdas == 1 && tree.Nodes.GetSurePresentRef(tree.RootIdx).NodeType == ExprType.Lambda ? Result.OK : Result.NotSupported_UnknownExpression;
    }

    /// <summary>Returns null if the tree is not the supported subset or the delegate type is not the lambda type.</summary>
    public static TDelegate TryCompileFlat<TDelegate>(this ref ExprTree tree) where TDelegate : class
    {
        if (tree.TryCollectFlatInfo() != Result.OK)
            return null;

        ref var lambda = ref tree.Nodes.GetSurePresentRef(tree.RootIdx);
        if (lambda.Type != typeof(TDelegate))
            return null;

        var bodyIdx = lambda.ChildIdx;
        var returnType = tree.Nodes.GetSurePresentRef(bodyIdx).Type;
        var invoke = typeof(TDelegate).GetMethod("Invoke");
        var delegateReturn = invoke?.ReturnType;
        if (delegateReturn == null || !IsFlatType(delegateReturn) || !FlatFits(delegateReturn, returnType))
            return null;

        var paramCount = lambda.ChildCount - 1;
        var delegatePars = invoke.GetParameters();
        if (delegatePars.Length != paramCount)
            return null;
        var paramTypes = new Type[paramCount + 1];
        paramTypes[0] = typeof(ArrayClosure);
        var firstParIdx = tree.Nodes.GetSurePresentRef(bodyIdx).NextIdx;
        for (var i = 1; i <= paramCount; ++i)
        {
            ref var par = ref tree.Nodes.GetSurePresentRef(firstParIdx);
            if (par.Type != delegatePars[i - 1].ParameterType)
                return null;
            paramTypes[i] = par.Type;
            firstParIdx = par.NextIdx;
        }

        var dynMethod = new DynamicMethod(string.Empty, delegateReturn, paramTypes, typeof(ArrayClosure), true);
        var il = dynMethod.GetILGenerator();

        var vars = default(SmallList<FlatVar, Stack8<FlatVar>, NoArrayPool<FlatVar>>);
        var parIdx = tree.Nodes.GetSurePresentRef(bodyIdx).NextIdx;
        for (var i = 1; i <= paramCount; ++i)
        {
            ref var par = ref tree.Nodes.GetSurePresentRef(parIdx);
            vars.Add(new FlatVar { Id = par.ChildIdx, Arg = (short)i });
            parIdx = par.NextIdx;
        }

        EmitFlat(ref tree, bodyIdx, il, ref vars, false);
        il.Emit(OpCodes.Ret);
        object closure = EmptyArrayClosure;
        if (tree.ClosureConstants.Count != 0)
        {
            var constants = new object[tree.ClosureConstants.Count];
            for (var i = 0; i < constants.Length; ++i)
                constants[i] = tree.ClosureConstants[i];
            closure = new ArrayClosure(constants);
        }
        return (TDelegate)(object)dynMethod.CreateDelegate(typeof(TDelegate), closure);
    }

    private static void EmitFlat(ref ExprTree tree, int idx, ILGenerator il,
        ref SmallList<FlatVar, Stack8<FlatVar>, NoArrayPool<FlatVar>> vars, bool ignoreResult)
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
                if (ignoreResult)
                    return;
                for (var i = vars.Count - 1; i >= 0; --i)
                {
                    ref var v = ref vars.GetSurePresentRef(i);
                    if (v.Id != n.ChildIdx)
                        continue;
                    if (v.Arg >= 0) il.Emit(OpCodes.Ldarg, v.Arg); else il.Emit(OpCodes.Ldloc, v.Local);
                    return;
                }
                throw new InvalidOperationException("The parameter is not in scope"); // todo: @wip by construction check

            case ExprType.Assign:
                {
                    var leftIdx = n.ChildIdx;
                    ref var left = ref tree.Nodes.GetSurePresentRef(leftIdx);
                    EmitFlat(ref tree, left.NextIdx, il, ref vars, false);
                    if (!ignoreResult)
                        il.Emit(OpCodes.Dup);
                    for (var i = vars.Count - 1; i >= 0; --i)
                    {
                        ref var v = ref vars.GetSurePresentRef(i);
                        if (v.Id != left.ChildIdx)
                            continue;
                        if (v.Arg >= 0) il.Emit(OpCodes.Starg, v.Arg); else il.Emit(OpCodes.Stloc, v.Local);
                        return;
                    }
                    throw new InvalidOperationException("The assigned parameter is not in scope");
                }

            case ExprType.New:
            case ExprType.Call:
                {
                    var argIdx = n.ChildIdx;
                    for (var i = 0; i < n.ChildCount; ++i)
                    {
                        EmitFlat(ref tree, argIdx, il, ref vars, false);
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
                        EmitFlat(ref tree, n.ChildIdx, il, ref vars, false);
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
                EmitFlat(ref tree, n.ChildIdx, il, ref vars, false);
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
                    EmitFlat(ref tree, testIdx, il, ref vars, false);
                    il.Emit(OpCodes.Brfalse, elseLabel);
                    EmitFlat(ref tree, ifTrueIdx, il, ref vars, ignoreResult);
                    il.Emit(OpCodes.Br, endLabel);
                    il.MarkLabel(elseLabel);
                    EmitFlat(ref tree, ifFalseIdx, il, ref vars, ignoreResult);
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
                    EmitFlat(ref tree, n.ChildIdx, il, ref vars, false);
                    il.Emit(isAnd ? OpCodes.Brfalse : OpCodes.Brtrue, shortCircuit);
                    EmitFlat(ref tree, rightIdx, il, ref vars, false);
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
                        vars.Add(new FlatVar { Id = v.ChildIdx, Arg = -1, Local = il.DeclareLocal(v.Type) });
                        varIdx = v.NextIdx;
                    }

                    var last = exprs.ChildCount - 1;
                    var exprIdx = exprs.ChildIdx;
                    for (var i = 0; i <= last; ++i)
                    {
                        EmitFlat(ref tree, exprIdx, il, ref vars, i != last || ignoreResult);
                        exprIdx = tree.Nodes.GetSurePresentRef(exprIdx).NextIdx;
                    }
                    vars.Count = scopeStart; // the block variables go out of the scope
                    return;
                }

            default: // the binary int/bool operations
                {
                    EmitFlat(ref tree, n.ChildIdx, il, ref vars, false);
                    EmitFlat(ref tree, tree.Nodes.GetSurePresentRef(n.ChildIdx).NextIdx, il, ref vars, false);
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

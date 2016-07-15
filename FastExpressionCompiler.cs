/*
The MIT License (MIT)

Copyright (c) 2016 Maksim Volkau

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included AddOrUpdateServiceFactory
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

namespace DryIoc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>Compiles expression to delegate by emitting the IL directly.
    /// The emitter is ~10 times faster than Expression.Compile.</summary>
    public static class FastExpressionCompiler
    {
        /// <summary>Compiles expression to delegate by emitting the IL. 
        /// If sub-expressions are not supported by emitter, then the method returns null.
        /// The usage should be calling the method, if result is null then calling the Expression.Compile.</summary>
        /// <param name="bodyExpr">Lambda body.</param>
        /// <param name="paramExprs">Lambda parameter expressions.</param>
        /// <param name="paramTypes">The types of parameters.</param>
        /// <param name="returnType">The return type.</param>
        /// <returns>Result delegate or null, if unable to compile.</returns>
        public static TDelegate TryCompile<TDelegate>(
            Expression bodyExpr, 
            ParameterExpression[] paramExprs, 
            Type[] paramTypes,
            Type returnType)
            where TDelegate : class
        {
            var constantExprs = new List<ConstantExpression>(8);
            CollectBoundConstants(bodyExpr, constantExprs);

            object closure = null;
            ClosureInfo closureInfo = null;

            DynamicMethod method;
            if (constantExprs.Count == 0)
            {
                method = new DynamicMethod(string.Empty, returnType, paramTypes,
                    typeof(FastExpressionCompiler).Module, skipVisibility: true);
            }
            else
            {
                var constants = new object[constantExprs.Count];
                for (var i = 0; i < constants.Length; i++)
                    constants[i] = constantExprs[i].Value;

                if (constants.Length <= Closure.CreateMethods.Length)
                {
                    var createClosureMethod = Closure.CreateMethods[constants.Length - 1];

                    var constantTypes = new Type[constantExprs.Count];
                    for (var i = 0; i < constants.Length; i++)
                        constantTypes[i] = constantExprs[i].Type;

                    var createClosure = createClosureMethod.MakeGenericMethod(constantTypes);

                    closure = createClosure.Invoke(null, constants);
                    closureInfo = new ClosureInfo(constantExprs, closure.GetType().GetTypeInfo().DeclaredFields.ToArrayOrSelf());
                }
                else
                {
                    var arrayClosure = new ArrayClosure(constants);
                    closure = arrayClosure;
                    closureInfo = new ClosureInfo(constantExprs);
                }

                var closureType = closure.GetType();
                var closureAndParamTypes = GetClosureAndParamTypes(paramTypes, closureType);

                method = new DynamicMethod(string.Empty, returnType, closureAndParamTypes, closureType, skipVisibility: true);
            }

            var il = method.GetILGenerator();
            var emitted = EmittingVisitor.TryEmit(bodyExpr, paramExprs, il, closureInfo);
            if (emitted)
            {
                il.Emit(OpCodes.Ret);
                return (TDelegate)(object)method.CreateDelegate(typeof(TDelegate), closure);
            }

            return null;
        }

        private static Type[] GetClosureAndParamTypes(Type[] paramTypes, Type closureType)
        {
            var paramCount = paramTypes.Length;
            var closureAndParamTypes = new Type[paramCount + 1];
            closureAndParamTypes[0] = closureType;
            if (paramCount == 1)
                closureAndParamTypes[1] = paramTypes[0];
            else if (paramCount > 1)
                Array.Copy(paramTypes, 0, closureAndParamTypes, 1, paramCount);
            return closureAndParamTypes;
        }

        private sealed class ClosureInfo
        {
            public readonly IList<ConstantExpression> ConstantExpressions;

            public readonly FieldInfo[] ConstantClosureFields;
            public readonly bool IsClosureArray;

            public ClosureInfo(IList<ConstantExpression> constantExpressions, FieldInfo[] constantClosureFields = null)
            {
                ConstantExpressions = constantExpressions;
                ConstantClosureFields = constantClosureFields;
                IsClosureArray = constantClosureFields == null;
            }
        }

        #region Closures

        internal static class Closure
        {
            public static readonly MethodInfo[] CreateMethods =
                typeof(Closure).GetTypeInfo().DeclaredMethods.ToArrayOrSelf();

            public static Closure<T1> CreateClosure<T1>(T1 v1)
            {
                return new Closure<T1>(v1);
            }

            public static Closure<T1, T2> CreateClosure<T1, T2>(T1 v1, T2 v2)
            {
                return new Closure<T1, T2>(v1, v2);
            }

            public static Closure<T1, T2, T3> CreateClosure<T1, T2, T3>(T1 v1, T2 v2, T3 v3)
            {
                return new Closure<T1, T2, T3>(v1, v2, v3);
            }

            public static Closure<T1, T2, T3, T4> CreateClosure<T1, T2, T3, T4>(T1 v1, T2 v2, T3 v3, T4 v4)
            {
                return new Closure<T1, T2, T3, T4>(v1, v2, v3, v4);
            }

            public static Closure<T1, T2, T3, T4, T5> CreateClosure<T1, T2, T3, T4, T5>(T1 v1, T2 v2, T3 v3, T4 v4,
                T5 v5)
            {
                return new Closure<T1, T2, T3, T4, T5>(v1, v2, v3, v4, v5);
            }

            public static Closure<T1, T2, T3, T4, T5, T6> CreateClosure<T1, T2, T3, T4, T5, T6>(T1 v1, T2 v2, T3 v3,
                T4 v4, T5 v5, T6 v6)
            {
                return new Closure<T1, T2, T3, T4, T5, T6>(v1, v2, v3, v4, v5, v6);
            }

            public static Closure<T1, T2, T3, T4, T5, T6, T7> CreateClosure<T1, T2, T3, T4, T5, T6, T7>(T1 v1, T2 v2,
                T3 v3, T4 v4, T5 v5, T6 v6, T7 v7)
            {
                return new Closure<T1, T2, T3, T4, T5, T6, T7>(v1, v2, v3, v4, v5, v6, v7);
            }

            public static Closure<T1, T2, T3, T4, T5, T6, T7, T8> CreateClosure<T1, T2, T3, T4, T5, T6, T7, T8>(
                T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8)
            {
                return new Closure<T1, T2, T3, T4, T5, T6, T7, T8>(v1, v2, v3, v4, v5, v6, v7, v8);
            }

            public static Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9> CreateClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
                T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9)
            {
                return new Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9>(v1, v2, v3, v4, v5, v6, v7, v8, v9);
            }

            public static Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> CreateClosure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
                T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10)
            {
                return new Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10);
            }
        }

        internal class Closure<T1>
        {
            public T1 V1;

            public Closure(T1 v1)
            {
                V1 = v1;
            }
        }

        internal sealed class Closure<T1, T2>
        {
            public T1 V1;
            public T2 V2;

            public Closure(T1 v1, T2 v2)
            {
                V1 = v1;
                V2 = v2;
            }
        }

        internal sealed class Closure<T1, T2, T3>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;

            public Closure(T1 v1, T2 v2, T3 v3)
            {
                V1 = v1;
                V2 = v2;
                V3 = v3;
            }
        }

        internal sealed class Closure<T1, T2, T3, T4>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;

            public Closure(T1 v1, T2 v2, T3 v3, T4 v4)
            {
                V1 = v1;
                V2 = v2;
                V3 = v3;
                V4 = v4;
            }
        }

        internal sealed class Closure<T1, T2, T3, T4, T5>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;

            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5)
            {
                V1 = v1;
                V2 = v2;
                V3 = v3;
                V4 = v4;
                V5 = v5;
            }
        }

        internal sealed class Closure<T1, T2, T3, T4, T5, T6>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;

            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6)
            {
                V1 = v1;
                V2 = v2;
                V3 = v3;
                V4 = v4;
                V5 = v5;
                V6 = v6;
            }
        }

        internal sealed class Closure<T1, T2, T3, T4, T5, T6, T7>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public T7 V7;

            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7)
            {
                V1 = v1;
                V2 = v2;
                V3 = v3;
                V4 = v4;
                V5 = v5;
                V6 = v6;
                V7 = v7;
            }
        }

        internal sealed class Closure<T1, T2, T3, T4, T5, T6, T7, T8>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public T7 V7;
            public T8 V8;

            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8)
            {
                V1 = v1;
                V2 = v2;
                V3 = v3;
                V4 = v4;
                V5 = v5;
                V6 = v6;
                V7 = v7;
                V8 = v8;
            }
        }

        internal sealed class Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public T7 V7;
            public T8 V8;
            public T9 V9;

            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9)
            {
                V1 = v1;
                V2 = v2;
                V3 = v3;
                V4 = v4;
                V5 = v5;
                V6 = v6;
                V7 = v7;
                V8 = v8;
                V9 = v9;
            }
        }

        internal sealed class Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public T7 V7;
            public T8 V8;
            public T9 V9;
            public T10 V10;

            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10)
            {
                V1 = v1;
                V2 = v2;
                V3 = v3;
                V4 = v4;
                V5 = v5;
                V6 = v6;
                V7 = v7;
                V8 = v8;
                V9 = v9;
                V10 = v10;
            }
        }

        internal sealed class ArrayClosure
        {
            public readonly object[] Constants;
            public static FieldInfo ArrayField = typeof(ArrayClosure).GetTypeInfo().DeclaredFields.First(f => !f.IsStatic);

            public ArrayClosure(object[] constants)
            {
                Constants = constants;
            }
        }

        #endregion

        #region Collect Bound Constants

        private static bool IsBoundConstant(object value)
        {
            return value != null && 
                !(value is int || value is double || value is bool || 
                value is string || value is Type || value.GetType().IsEnum);
        }

        private static void CollectBoundConstants(Expression expr, List<ConstantExpression> constants)
        {
            if (expr == null)
                return;

            switch (expr.NodeType)
            {
                case ExpressionType.Constant:
                    var constantExpr = (ConstantExpression)expr;
                    if (IsBoundConstant(constantExpr.Value))
                        constants.Add(constantExpr);
                    break;

                case ExpressionType.Call:
                    CollectBoundConstants(((MethodCallExpression)expr).Object, constants);
                    CollectBoundConstants(((MethodCallExpression)expr).Arguments, constants);
                    break;

                case ExpressionType.MemberAccess:
                    CollectBoundConstants(((MemberExpression)expr).Expression, constants);
                    break;

                case ExpressionType.New:
                    CollectBoundConstants(((NewExpression)expr).Arguments, constants);
                    break;

                case ExpressionType.NewArrayInit:
                    CollectBoundConstants(((NewArrayExpression)expr).Expressions, constants);
                    break;

                // property initializer
                case ExpressionType.MemberInit:
                    var memberInitExpr = (MemberInitExpression)expr;
                    CollectBoundConstants(memberInitExpr.NewExpression, constants);

                    var memberBindings = memberInitExpr.Bindings;
                    for (var i = 0; i < memberBindings.Count; ++i)
                    {
                        var memberBinding = memberBindings[i];
                        if (memberBinding.BindingType == MemberBindingType.Assignment)
                            CollectBoundConstants(((MemberAssignment)memberBinding).Expression, constants);
                    }
                    break;

                default:
                    var unaryExpr = expr as UnaryExpression;
                    if (unaryExpr != null)
                    {
                        CollectBoundConstants(unaryExpr.Operand, constants);
                    }
                    else
                    {
                        var binaryExpr = expr as BinaryExpression;
                        if (binaryExpr != null)
                        {
                            CollectBoundConstants(binaryExpr.Left, constants);
                            CollectBoundConstants(binaryExpr.Right, constants);
                        }
                    }
                    break;
            }
        }

        private static void CollectBoundConstants(IList<Expression> exprs, List<ConstantExpression> constants)
        {
            var count = exprs.Count;
            for (var i = 0; i < count; i++)
                CollectBoundConstants(exprs[i], constants);
        }

        #endregion

        /// <summary>Supports emitting of selected expressions, e.g. lambdaExpr are not supported yet.
        /// When emitter find not supported expression it will return false from <see cref="TryEmit"/>, so I could fallback
        /// to normal and slow Expression.Compile.</summary>
        private static class EmittingVisitor 
        {
            public static bool TryEmit(Expression expr, IList<ParameterExpression> paramExprs, ILGenerator il, ClosureInfo closure)
            {
                switch (expr.NodeType)
                {
                    case ExpressionType.Parameter:
                        return EmitParameter((ParameterExpression)expr, paramExprs, il);
                    case ExpressionType.Convert:
                        return EmitConvert((UnaryExpression)expr, paramExprs, il, closure);
                    case ExpressionType.ArrayIndex:
                        return EmitArrayIndex((BinaryExpression)expr, paramExprs, il, closure);
                    case ExpressionType.Constant:
                        return EmitConstant((ConstantExpression)expr, il, closure);
                    case ExpressionType.New:
                        return EmitNew((NewExpression)expr, paramExprs, il, closure);
                    case ExpressionType.NewArrayInit:
                        return EmitNewArray((NewArrayExpression)expr, paramExprs, il, closure);
                    case ExpressionType.MemberInit:
                        return EmitMemberInit((MemberInitExpression)expr, paramExprs, il, closure);
                    case ExpressionType.Call:
                        return EmitMethodCall((MethodCallExpression)expr, paramExprs, il, closure);
                    case ExpressionType.MemberAccess:
                        return EmitMemberAccess((MemberExpression)expr, paramExprs, il, closure);
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                        return EmitComparison((BinaryExpression)expr, paramExprs, il, closure);
                    default:
                        // Not supported yet: nested lambdas (Invoke)
                        return false;
                }
            }

            private static bool EmitParameter(ParameterExpression p, IList<ParameterExpression> ps, ILGenerator il)
            {
                var pIndex = ps.IndexOf(p);
                if (pIndex == -1)
                    return false;

                switch (pIndex)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        if (pIndex <= byte.MaxValue)
                            il.Emit(OpCodes.Ldarg_S, (byte)pIndex);
                        else
                            il.Emit(OpCodes.Ldarg, pIndex);
                        break;
                }

                return true;
            }

            private static bool EmitBinary(BinaryExpression b, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var ok = TryEmit(b.Left, ps, il, closure);
                if (ok)
                    ok = TryEmit(b.Right, ps, il, closure);
                // skips TryEmit(b.Conversion) for NodeType.Coalesce (?? operation)
                return ok;
            }

            private static bool EmitMany(IList<Expression> es, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                for (int i = 0, n = es.Count; i < n; i++)
                    if (!TryEmit(es[i], ps, il, closure))
                        return false;
                return true;
            }

            private static bool EmitConvert(UnaryExpression node, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var ok = TryEmit(node.Operand, ps, il, closure);
                if (ok)
                {
                    var convertTargetType = node.Type;
                    if (convertTargetType == typeof(object))
                        return false;
                    il.Emit(OpCodes.Castclass, convertTargetType);
                }
                return ok;
            }

            private static bool EmitConstant(ConstantExpression constantExpr, ILGenerator il, ClosureInfo closure)
            {
                var constant = constantExpr.Value;
                if (constant == null)
                {
                    il.Emit(OpCodes.Ldnull);
                }
                else if (constant is int || constant.GetType().IsEnum)
                {
                    EmitLoadConstantInt(il, (int)constant);
                }
                else if (constant is double)
                {
                    il.Emit(OpCodes.Ldc_R8, (double)constant);
                }
                else if (constant is bool)
                {
                    il.Emit((bool)constant ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                }
                else if (constant is string)
                {
                    il.Emit(OpCodes.Ldstr, (string)constant);
                }
                else if (constant is Type)
                {
                    il.Emit(OpCodes.Ldtoken, (Type)constant);
                    var getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                    il.Emit(OpCodes.Call, getTypeFromHandle);
                }
                else if (closure != null)
                {
                    var constantIndex = closure.ConstantExpressions.IndexOf(constantExpr);
                    if (constantIndex == -1)
                        return false;

                    // load closure argument: Closure or Closure Array
                    il.Emit(OpCodes.Ldarg_0);

                    if (!closure.IsClosureArray)
                    {
                        // load closure field
                        il.Emit(OpCodes.Ldfld, closure.ConstantClosureFields[constantIndex]);
                    }
                    else
                    {
                        // load array field
                        il.Emit(OpCodes.Ldfld, ArrayClosure.ArrayField);

                        // load array item index
                        EmitLoadConstantInt(il, constantIndex);

                        // load item from index
                        il.Emit(OpCodes.Ldelem_Ref);

                        // case if needed
                        var castType = constantExpr.Type;
                        if (castType != typeof(object))
                            il.Emit(OpCodes.Castclass, castType);
                    }
                }
                else
                {
                    return false;
                }

                // boxing the value type, otherwise we can get a strange result when 0 is treated as Null.
                if (constantExpr.Type == typeof(object) &&
                    constant != null && constant.GetType().IsValueType())
                    il.Emit(OpCodes.Box, constant.GetType());

                return true;
            }

            private static bool EmitNew(NewExpression n, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var ok = EmitMany(n.Arguments, ps, il, closure);
                if (ok)
                    il.Emit(OpCodes.Newobj, n.Constructor);
                return ok;
            }

            private static bool EmitNewArray(NewArrayExpression na, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var elems = na.Expressions;
                var arrType = na.Type;
                var elemType = arrType.GetElementType();
                var isElemOfValueType = elemType.IsValueType;

                var arrVar = il.DeclareLocal(arrType);

                EmitLoadConstantInt(il, elems.Count);
                il.Emit(OpCodes.Newarr, elemType);
                il.Emit(OpCodes.Stloc, arrVar);

                var ok = true;
                for (int i = 0, n = elems.Count; i < n && ok; i++)
                {
                    il.Emit(OpCodes.Ldloc, arrVar);
                    EmitLoadConstantInt(il, i);

                    // loading element address for later copying of value into it.
                    if (isElemOfValueType)
                        il.Emit(OpCodes.Ldelema, elemType);

                    ok = TryEmit(elems[i], ps, il, closure);
                    if (ok)
                    {
                        if (isElemOfValueType)
                            il.Emit(OpCodes.Stobj, elemType); // store element of value type by array element address
                        else
                            il.Emit(OpCodes.Stelem_Ref);
                    }
                }

                il.Emit(OpCodes.Ldloc, arrVar);
                return ok;
            }

            private static bool EmitArrayIndex(BinaryExpression ai, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var ok = EmitBinary(ai, ps, il, closure);
                if (ok)
                    il.Emit(OpCodes.Ldelem_Ref);
                return ok;
            }

            private static bool EmitMemberInit(MemberInitExpression mi, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var ok = EmitNew(mi.NewExpression, ps, il, closure);
                if (!ok) return false;

                var obj = il.DeclareLocal(mi.Type);
                il.Emit(OpCodes.Stloc, obj);

                var bindings = mi.Bindings;
                for (int i = 0, n = bindings.Count; i < n; i++)
                {
                    var binding = bindings[i];
                    if (binding.BindingType != MemberBindingType.Assignment)
                        return false;
                    il.Emit(OpCodes.Ldloc, obj);

                    ok = TryEmit(((MemberAssignment)binding).Expression, ps, il, closure);
                    if (!ok) return false;

                    var prop = binding.Member as PropertyInfo;
                    if (prop != null)
                    {
                        var setMethod = prop.GetSetMethod();
                        if (setMethod == null)
                            return false;
                        EmitMethodCall(setMethod, il);
                    }
                    else
                    {
                        var field = binding.Member as FieldInfo;
                        if (field == null)
                            return false;
                        il.Emit(OpCodes.Stfld, field);
                    }
                }

                il.Emit(OpCodes.Ldloc, obj);
                return true;
            }

            private static bool EmitMethodCall(MethodCallExpression m, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var ok = true;
                if (m.Object != null)
                    ok = TryEmit(m.Object, ps, il, closure);

                if (ok && m.Arguments.Count != 0)
                    ok = EmitMany(m.Arguments, ps, il, closure);

                if (ok)
                    EmitMethodCall(m.Method, il);

                return ok;
            }

            private static bool EmitMemberAccess(MemberExpression m, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                if (m.Expression != null)
                {
                    var ok = TryEmit(m.Expression, ps, il, closure);
                    if (!ok) return false;
                }

                var field = m.Member as FieldInfo;
                if (field != null)
                {
                    il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
                    return true;
                }

                var property = m.Member as PropertyInfo;
                if (property != null)
                {
                    var getMethod = property.GetGetMethod();
                    if (getMethod == null)
                        return false;
                    EmitMethodCall(getMethod, il);
                }

                return true;
            }

            private static bool EmitComparison(BinaryExpression c, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var ok = EmitBinary(c, ps, il, closure);
                if (ok)
                {
                    switch (c.NodeType)
                    {
                        case ExpressionType.Equal:
                            il.Emit(OpCodes.Ceq);
                            break;
                        case ExpressionType.LessThan:
                            il.Emit(OpCodes.Clt);
                            break;
                        case ExpressionType.GreaterThan:
                            il.Emit(OpCodes.Cgt);
                            break;
                        case ExpressionType.NotEqual:
                            il.Emit(OpCodes.Ceq);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ceq);
                            break;
                        case ExpressionType.LessThanOrEqual:
                            il.Emit(OpCodes.Cgt);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ceq);
                            break;
                        case ExpressionType.GreaterThanOrEqual:
                            il.Emit(OpCodes.Clt);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ceq);
                            break;
                    }
                }
                return ok;
            }

            private static void EmitMethodCall(MethodInfo method, ILGenerator il)
            {
                il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
            }

            private static void EmitLoadConstantInt(ILGenerator il, int i)
            {
                switch (i)
                {
                    case 0:
                        il.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldc_I4_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldc_I4_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldc_I4_3);
                        break;
                    case 4:
                        il.Emit(OpCodes.Ldc_I4_4);
                        break;
                    case 5:
                        il.Emit(OpCodes.Ldc_I4_5);
                        break;
                    case 6:
                        il.Emit(OpCodes.Ldc_I4_6);
                        break;
                    case 7:
                        il.Emit(OpCodes.Ldc_I4_7);
                        break;
                    case 8:
                        il.Emit(OpCodes.Ldc_I4_8);
                        break;
                    default:
                        il.Emit(OpCodes.Ldc_I4, i);
                        break;
                }
            }
        }
    }
}

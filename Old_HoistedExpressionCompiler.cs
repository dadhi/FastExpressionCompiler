using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ExpressionToCodeLib.Internal
{
    internal static class OptimizedExpressionCompilerImpl
    {
        public static Func<T> TryCompile<T>(Expression<Func<T>> expression)
        {
            var closure = TryGetClosure(expression.Body);
            var method = closure == null
                ? new DynamicMethod(string.Empty, typeof(T), Type.EmptyTypes, typeof(OptimizedExpressionCompiler).Module)
                : new DynamicMethod(string.Empty, typeof(T), new[] { closure.GetType() }, closure.GetType());

            var il = method.GetILGenerator();
            var emitted = EmittingVisitor.TryEmit(expression.Body, il, closure != null);

            if (emitted) {
                il.Emit(OpCodes.Ret);
                return (Func<T>)method.CreateDelegate(typeof(Func<T>), closure);
            }

            return null;
        }

        static object TryGetClosure(Expression expr)
        {
            if (expr == null) {
                return null;
            }

            switch (expr.NodeType) {
                case ExpressionType.Constant:
                    var valueExpr = (ConstantExpression)expr;
                    return valueExpr.Type.IsClosureType() ? valueExpr.Value : null;
                case ExpressionType.Call:
                    return TryGetClosure(((MethodCallExpression)expr).Object)
                        ?? ((MethodCallExpression)expr).Arguments.Select(TryGetClosure).FirstOrDefault(c => c != null);
                case ExpressionType.MemberAccess:
                    return TryGetClosure(((MemberExpression)expr).Expression);
                case ExpressionType.New:
                    return ((NewExpression)expr).Arguments.Select(TryGetClosure).FirstOrDefault(c => c != null);
                case ExpressionType.NewArrayInit:
                    return ((NewArrayExpression)expr).Expressions.Select(TryGetClosure).FirstOrDefault(c => c != null);
                // todo: add support for the rest of node types:
                case ExpressionType.MemberInit:
                    return null;
                default:
                    var unaryExpr = expr as UnaryExpression;
                    if (unaryExpr != null) {
                        return TryGetClosure(unaryExpr.Operand);
                    }
                    var binaryExpr = expr as BinaryExpression;
                    if (binaryExpr != null) {
                        return TryGetClosure(binaryExpr.Left) ?? TryGetClosure(binaryExpr.Right);
                    }
                    return null;
            }
        }

        static bool IsClosureType(this Type type) { return type.Name.Contains("<>c__DisplayClass"); }

        /// <summary>Supports emitting IL for selected expressions (May be extended per NodeType basis).
        /// When emitter find not supported expression it will return false from <see cref="TryEmit"/>, 
        /// so the compilation may fallback to usual/slow Expression.Compile.</summary>
        static class EmittingVisitor
        {
            public static bool TryEmit(Expression expr, ILGenerator il, bool withClosure)
            {
                switch (expr.NodeType) {
                    case ExpressionType.Convert:
                        return VisitConvert((UnaryExpression)expr, il, withClosure);
                    case ExpressionType.ArrayIndex:
                        return VisitArrayIndex((BinaryExpression)expr, il, withClosure);
                    case ExpressionType.Constant:
                        return VisitConstant((ConstantExpression)expr, il, withClosure);
                    case ExpressionType.New:
                        return VisitNew((NewExpression)expr, il, withClosure);
                    case ExpressionType.NewArrayInit:
                        return VisitNewArray((NewArrayExpression)expr, il, withClosure);
                    case ExpressionType.MemberInit:
                        return VisitMemberInit((MemberInitExpression)expr, il, withClosure);
                    case ExpressionType.Call:
                        return VisitMethodCall((MethodCallExpression)expr, il, withClosure);
                    case ExpressionType.MemberAccess:
                        return VisitMemberAccess((MemberExpression)expr, il, withClosure);
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                        return VisitComparison((BinaryExpression)expr, il, withClosure);
                    default:
                        // todo: add support for the rest of node types
                        return false;
                }
            }

            static bool VisitBinary(BinaryExpression b, ILGenerator il, bool withClosure)
            {
                var ok = TryEmit(b.Left, il, withClosure);
                if (ok) {
                    ok = TryEmit(b.Right, il, withClosure);
                }
                // skips TryVisit(b.Conversion) for NodeType.Coalesce (?? operation)
                return ok;
            }

            static bool VisitExpressionList(IList<Expression> eList, ILGenerator state, bool withClosure)
            {
                var ok = true;
                for (int i = 0, n = eList.Count; i < n && ok; i++) {
                    ok = TryEmit(eList[i], state, withClosure);
                }
                return ok;
            }

            static bool VisitConvert(UnaryExpression node, ILGenerator il, bool withClosure)
            {
                var ok = TryEmit(node.Operand, il, withClosure);
                if (ok) {
                    var convertTargetType = node.Type;
                    // not supported, probably required for converting ValueType
                    if (convertTargetType == typeof(object)) {
                        return false;
                    }
                    il.Emit(OpCodes.Castclass, convertTargetType);
                }
                return ok;
            }

            static bool VisitConstant(ConstantExpression node, ILGenerator il, bool withClosure)
            {
                var value = node.Value;
                if (value == null) {
                    il.Emit(OpCodes.Ldnull);
                } else if (value is int || value.GetType().IsEnum) {
                    EmitLoadConstantInt(il, (int)value);
                } else if (value is double) {
                    il.Emit(OpCodes.Ldc_R8, (double)value);
                } else if (value is bool) {
                    il.Emit((bool)value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                } else if (value is string) {
                    il.Emit(OpCodes.Ldstr, (string)value);
                } else if (withClosure && value.GetType().IsClosureType()) {
                    il.Emit(OpCodes.Ldarg_0);
                } else {
                    return false;
                }
                return true;
            }

            static bool VisitNew(NewExpression node, ILGenerator il, bool withClosure)
            {
                var ok = VisitExpressionList(node.Arguments, il, withClosure);
                if (ok) {
                    il.Emit(OpCodes.Newobj, node.Constructor);
                }
                return ok;
            }

            static bool VisitNewArray(NewArrayExpression node, ILGenerator il, bool withClosure)
            {
                var elems = node.Expressions;
                var arrType = node.Type;
                var elemType = arrType.GetElementType();
                var isElemOfValueType = elemType.IsValueType;

                var arrVar = il.DeclareLocal(arrType);

                EmitLoadConstantInt(il, elems.Count);
                il.Emit(OpCodes.Newarr, elemType);
                il.Emit(OpCodes.Stloc, arrVar);

                var ok = true;
                for (int i = 0, n = elems.Count; i < n && ok; i++) {
                    il.Emit(OpCodes.Ldloc, arrVar);
                    EmitLoadConstantInt(il, i);

                    // loading element address for later copying of value into it.
                    if (isElemOfValueType) {
                        il.Emit(OpCodes.Ldelema, elemType);
                    }

                    ok = TryEmit(elems[i], il, withClosure);
                    if (ok) {
                        if (isElemOfValueType) {
                            il.Emit(OpCodes.Stobj, elemType); // store element of value type by array element address
                        } else {
                            il.Emit(OpCodes.Stelem_Ref);
                        }
                    }
                }

                il.Emit(OpCodes.Ldloc, arrVar);
                return ok;
            }

            static bool VisitArrayIndex(BinaryExpression node, ILGenerator il, bool withClosure)
            {
                var ok = VisitBinary(node, il, withClosure);
                if (ok) {
                    il.Emit(OpCodes.Ldelem_Ref);
                }
                return ok;
            }

            static bool VisitMemberInit(MemberInitExpression mi, ILGenerator il, bool withClosure)
            {
                var ok = VisitNew(mi.NewExpression, il, withClosure);
                if (!ok) {
                    return false;
                }

                var obj = il.DeclareLocal(mi.Type);
                il.Emit(OpCodes.Stloc, obj);

                var bindings = mi.Bindings;
                for (int i = 0, n = bindings.Count; i < n; i++) {
                    var binding = bindings[i];
                    if (binding.BindingType != MemberBindingType.Assignment) {
                        return false;
                    }
                    il.Emit(OpCodes.Ldloc, obj);

                    ok = TryEmit(((MemberAssignment)binding).Expression, il, withClosure);
                    if (!ok) {
                        return false;
                    }

                    var prop = binding.Member as PropertyInfo;
                    if (prop != null) {
                        var setMethod = prop.GetSetMethod();
                        if (setMethod == null) {
                            return false;
                        }
                        EmitMethodCall(setMethod, il);
                    } else {
                        var field = binding.Member as FieldInfo;
                        if (field == null) {
                            return false;
                        }
                        il.Emit(OpCodes.Stfld, field);
                    }
                }

                il.Emit(OpCodes.Ldloc, obj);
                return true;
            }

            static bool VisitMethodCall(MethodCallExpression expr, ILGenerator il, bool withClosure)
            {
                var ok = true;
                if (expr.Object != null) {
                    ok = TryEmit(expr.Object, il, withClosure);
                    if (ok && expr.Object.Type.IsValueType) {
                        // for instance methods store and load instance variable
                        var objectVar = il.DeclareLocal(expr.Object.Type);
                        il.Emit(OpCodes.Stloc, objectVar);
                        il.Emit(OpCodes.Ldloca, objectVar);
                    }
                }

                if (ok && expr.Arguments.Count != 0) {
                    ok = VisitExpressionList(expr.Arguments, il, withClosure);
                }

                if (ok) {
                    EmitMethodCall(expr.Method, il);
                }

                return ok;
            }

            static bool VisitMemberAccess(MemberExpression expr, ILGenerator il, bool withClosure)
            {
                if (expr.Expression != null) {
                    var ok = TryEmit(expr.Expression, il, withClosure);
                    if (!ok) {
                        return false;
                    }
                }

                var field = expr.Member as FieldInfo;
                if (field != null) {
                    il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
                    return true;
                }

                var property = expr.Member as PropertyInfo;
                if (property != null) {
                    var getMethod = property.GetGetMethod();
                    if (getMethod == null) {
                        return false;
                    }
                    EmitMethodCall(getMethod, il);
                }

                return true;
            }

            static bool VisitComparison(BinaryExpression comparison, ILGenerator il, bool withClosure)
            {
                var ok = VisitBinary(comparison, il, withClosure);
                if (ok) {
                    switch (comparison.NodeType) {
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

            static void EmitMethodCall(MethodInfo method, ILGenerator il) { il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method); }

            static void EmitLoadConstantInt(ILGenerator il, int i)
            {
                switch (i) {
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

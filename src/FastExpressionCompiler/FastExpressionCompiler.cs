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

namespace FastExpressionCompiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>Compiles expression to delegate by emitting the IL directly.
    /// The emitter is ~20 times faster than Expression.Compile.</summary>
    public static partial class ExpressionCompiler
    {
        /// <summary>First tries to compile fast and if failed (null result), then falls back to Expression.Compile.</summary>
        /// <typeparam name="T">Type of compiled delegate return result.</typeparam>
        /// <param name="lambdaExpr">Expr to compile.</param>
        /// <returns>Compiled delegate.</returns>
        public static Func<T> Compile<T>(Expression<Func<T>> lambdaExpr)
        {
            return TryCompile<Func<T>>(lambdaExpr.Body, lambdaExpr.Parameters, Empty<Type>(), typeof(T))
                   ?? lambdaExpr.Compile();
        }

        /// <summary>Compiles lambda expression to <typeparamref name="TDelegate"/>.</summary>
        /// <typeparam name="TDelegate">The compatible delegate type, otherwise case will throw.</typeparam>
        /// <param name="lambdaExpr">Lambda expression to compile.</param>
        /// <returns>Compiled delegate.</returns>
        public static TDelegate Compile<TDelegate>(LambdaExpression lambdaExpr)
            where TDelegate : class
        {
            return TryCompile<TDelegate>(lambdaExpr) ?? (TDelegate)(object)lambdaExpr.Compile();
        }

        /// <summary>Tries to compile lambda expression to <typeparamref name="TDelegate"/>.</summary>
        /// <typeparam name="TDelegate">The compatible delegate type, otherwise case will throw.</typeparam>
        /// <param name="lambdaExpr">Lambda expression to compile.</param>
        /// <returns>Compiled delegate.</returns>
        public static TDelegate TryCompile<TDelegate>(LambdaExpression lambdaExpr)
            where TDelegate : class
        {
            var paramExprs = lambdaExpr.Parameters;
            var paramTypes = GetParamExprTypes(paramExprs);
            var expr = lambdaExpr.Body;
            return TryCompile<TDelegate>(expr, paramExprs, paramTypes, expr.Type);
        }

        private static Type[] GetParamExprTypes(IList<ParameterExpression> paramExprs)
        {
            var paramsCount = paramExprs.Count;
            if (paramsCount == 0)
                return Empty<Type>();

            if (paramsCount == 1)
                return new[] { paramExprs[0].Type };

            var paramTypes = new Type[paramsCount];
            for (var i = 0; i < paramTypes.Length; i++)
                paramTypes[i] = paramExprs[i].Type;
            return paramTypes;
        }

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
            IList<ParameterExpression> paramExprs,
            Type[] paramTypes,
            Type returnType) where TDelegate : class
        {
            ClosureInfo ignored = null;
            return (TDelegate)TryCompile(ref ignored,
                typeof(TDelegate), paramTypes, returnType, bodyExpr, paramExprs);
        }

        /// <summary>Tries to compile lambda expression to <typeparamref name="TDelegate"/>.</summary>
        /// <typeparam name="TDelegate">The compatible delegate type, otherwise case will throw.</typeparam>
        /// <param name="lambdaExpr">Lambda expression to compile.</param>
        /// <returns>Compiled delegate.</returns>
        public static TDelegate TryCompile<TDelegate>(LambdaExpressionInfo lambdaExpr)
            where TDelegate : class
        {
            var paramExprs = lambdaExpr.Parameters;
            var paramTypes = GetParamExprTypes(paramExprs);
            var expr = lambdaExpr.Body;
            return TryCompile<TDelegate>(expr, paramExprs, paramTypes, expr.Type);
        }

        /// <summary>Compiles expression to delegate by emitting the IL. 
        /// If sub-expressions are not supported by emitter, then the method returns null.
        /// The usage should be calling the method, if result is null then calling the Expression.Compile.</summary>
        /// <param name="bodyExpr">Lambda body.</param>
        /// <param name="paramExprs">Lambda parameter expressions.</param>
        /// <param name="paramTypes">The types of parameters.</param>
        /// <param name="returnType">The return type.</param>
        /// <returns>Result delegate or null, if unable to compile.</returns>
        public static TDelegate TryCompile<TDelegate>(
            ExpressionInfo bodyExpr,
            IList<ParameterExpression> paramExprs,
            Type[] paramTypes,
            Type returnType) where TDelegate : class
        {
            ClosureInfo ignored = null;
            return (TDelegate)TryCompile(ref ignored,
                typeof(TDelegate), paramTypes, returnType, bodyExpr, paramExprs);
        }

        private struct Expr
        {
            public static implicit operator Expr(Expression expr)
            {
                return expr == null ? default(Expr) : new Expr(expr, expr.NodeType, expr.Type);
            }

            public static implicit operator Expr(ExpressionInfo expr)
            {
                return expr == null ? default(Expr) : new Expr(expr, expr.NodeType, expr.Type);
            }

            public object Expression;
            public ExpressionType NodeType;
            public Type Type;

            private Expr(object expression, ExpressionType nodeType, Type type)
            {
                Expression = expression;
                NodeType = nodeType;
                Type = type;
            }
        }

        private static object TryCompile(ref ClosureInfo closureInfo,
            Type delegateType, Type[] paramTypes, Type returnType,
            Expr bodyExpr, IList<ParameterExpression> paramExprs)
        {
            if (!TryCollectBoundConstants(ref closureInfo, bodyExpr, paramExprs))
                return null;

            if (closureInfo == null)
            {
                var method = new DynamicMethod(string.Empty, returnType, paramTypes,
                    typeof(ExpressionCompiler), skipVisibility: true);
                if (TryEmit(method, bodyExpr, paramExprs, null))
                    return method.CreateDelegate(delegateType);
                return null;
            }

            var closureObject = closureInfo.ConstructClosure();
            var closureType = closureObject.GetType();
            var closureAndParamTypes = GetClosureAndParamTypes(paramTypes, closureType);

            var boundMethod = new DynamicMethod(string.Empty, returnType, closureAndParamTypes,
                closureType, skipVisibility: true);

            if (TryEmit(boundMethod, bodyExpr, paramExprs, closureInfo))
                return boundMethod.CreateDelegate(delegateType, closureObject);
            return null;
        }

        private static bool TryEmit(DynamicMethod method,
            Expr bodyExpr, IList<ParameterExpression> paramExprs,
            ClosureInfo closureInfo)
        {
            var il = method.GetILGenerator();
            if (!EmittingVisitor.TryEmit(bodyExpr, paramExprs, il, closureInfo))
                return false;

            il.Emit(OpCodes.Ret); // emits return from generated method
            return true;
        }

        private static Type[] GetClosureAndParamTypes(Type[] paramTypes, Type closureType)
        {
            var paramCount = paramTypes.Length;
            if (paramCount == 0)
                return new[] { closureType };

            if (paramCount == 1)
                return new[] { closureType, paramTypes[0] };

            var closureAndParamTypes = new Type[paramCount + 1];
            closureAndParamTypes[0] = closureType;
            Array.Copy(paramTypes, 0, closureAndParamTypes, 1, paramCount);
            return closureAndParamTypes;
        }

        private struct ConstantInfo
        {
            public object Expression;
            public Type Type;
            public object Value;
            public ConstantInfo(object expr, object value, Type type)
            {
                Expression = expr;
                Value = value;
                Type = type;
            }
        }

        private static class EmptyArray<T>
        {
            public static readonly T[] Value = new T[0];
        }

        private static T[] Empty<T>()
        {
            return EmptyArray<T>.Value;
        }

        private static T[] Append<T>(this T[] source, T value)
        {
            if (source == null || source.Length == 0)
                return new[] { value };
            if (source.Length == 1)
                return new[] { source[0], value };
            if (source.Length == 2)
                return new[] { source[0], source[1], value };
            var sourceLength = source.Length;
            var result = new T[sourceLength + 1];
            Array.Copy(source, result, sourceLength);
            result[sourceLength] = value;
            return result;
        }

        private static int IndexOf<T>(this T[] source, Func<T, bool> predicate)
        {
            if (source == null || source.Length == 0)
                return -1;
            if (source.Length == 1)
                return predicate(source[0]) ? 0 : -1;
            for (var i = 0; i < source.Length; ++i)
                if (predicate(source[i]))
                    return i;
            return -1;
        }

        private sealed class ClosureInfo
        {
            public ConstantInfo[] Constants = Empty<ConstantInfo>();
            public ParameterExpression[] UsedParameters = Empty<ParameterExpression>();
            public NestedLambdaInfo[] NestedLambdas = Empty<NestedLambdaInfo>();

            // Field infos are needed to load field of closure object on stack in emitter
            // It is also an indicator that we use typed Closure object and not an array
            public FieldInfo[] Fields { get; private set; }
            public bool IsArray { get { return Fields == null; } }

            public void Add(object expr, object value, Type type)
            {
                Constants = Constants.Append(new ConstantInfo(expr, value, type));
            }

            public void Add(ParameterExpression expr)
            {
                UsedParameters = UsedParameters.Append(expr);
            }

            public void Add(NestedLambdaInfo lambda)
            {
                NestedLambdas = NestedLambdas.Append(lambda);
            }

            public object ConstructClosure()
            {
                var constantCount = Constants.Length;
                var paramCount = UsedParameters.Length;
                var constantPlusParamCount = constantCount + paramCount;

                var nestedLambdaCount = NestedLambdas.Length;
                var totalItemCount = constantPlusParamCount + nestedLambdaCount;

                var items = new object[totalItemCount];

                // Deciding to create typed or array based closure, based on number of closed constants
                // Not null array of constant types means a typed closure can be created
                var typedClosureCreateMethods = Closure.TypedClosureCreateMethods;
                var fieldTypes = totalItemCount <= typedClosureCreateMethods.Length
                    ? new Type[totalItemCount]
                    : null;

                if (constantCount != 0)
                    for (var i = 0; i < constantCount; i++)
                    {
                        var constantExpr = Constants[i];
                        items[i] = constantExpr.Value;
                        if (fieldTypes != null)
                            fieldTypes[i] = constantExpr.Type;
                    }

                if (paramCount != 0)
                    for (var i = 0; i < paramCount; i++)
                    {
                        items[constantCount + i] = null;
                        if (fieldTypes != null)
                            fieldTypes[constantCount + i] = UsedParameters[i].Type;
                    }

                if (nestedLambdaCount != 0)
                    for (var i = 0; i < nestedLambdaCount; i++)
                    {
                        var lambda = NestedLambdas[i].Lambda;
                        items[constantPlusParamCount + i] = lambda;
                        if (fieldTypes != null)
                            fieldTypes[constantPlusParamCount + i] = lambda.GetType();
                    }

                if (fieldTypes == null)
                    return new ArrayClosure(items);

                var createClosureMethod = typedClosureCreateMethods[totalItemCount - 1];
                var createClosure = createClosureMethod.MakeGenericMethod(fieldTypes);

                var closure = createClosure.Invoke(null, items);

                var fields = closure.GetType().GetTypeInfo().DeclaredFields;
                Fields = fields as FieldInfo[] ?? fields.ToArray();

                return closure;
            }
        }

        #region Closures

        internal static class Closure
        {
            public static readonly MethodInfo[] TypedClosureCreateMethods =
                typeof(Closure).GetTypeInfo().DeclaredMethods.ToArray();

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

        internal sealed class Closure<T1>
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

        private sealed class NestedLambdaInfo
        {
            public readonly object Lambda;
            public readonly Expression LambdaExpr; // to find the lambda in bigger parent expression
            public readonly ClosureInfo ClosureInfo;

            public NestedLambdaInfo(object lambda, Expression lambdaExpr, ClosureInfo closureInfo)
            {
                Lambda = lambda;
                LambdaExpr = lambdaExpr;
                ClosureInfo = closureInfo;
            }
        }

        private static bool IsBoundConstant(object value)
        {
            if (value == null)
                return false;

            var typeInfo = value.GetType().GetTypeInfo();
            return !typeInfo.IsPrimitive
                && !(value is string)
                && !(value is Type)
                && !typeInfo.IsEnum;
        }

        // @paramExprs is required for nested lambda compilation
        private static bool TryCollectBoundConstants(
            ref ClosureInfo closure, Expr e, IList<ParameterExpression> paramExprs)
        {
            var expr = e.Expression;
            if (expr == null)
                return false;

            switch (e.NodeType)
            {
                case ExpressionType.Constant:
                    var constExprInfo = expr as ConstantExpressionInfo;
                    var value = constExprInfo != null ? constExprInfo.Value : ((ConstantExpression)expr).Value;
                    if (value is Delegate || IsBoundConstant(value))
                        (closure ?? (closure = new ClosureInfo())).Add(expr, value, e.Type);
                    break;

                case ExpressionType.Parameter:
                    // if parameter is used But no passed (not in parameter expressions)
                    // it means parameter is provided by outer lambda and should be put in closure for current lambda
                    var paramExpr = (ParameterExpression)expr;
                    if (paramExprs.IndexOf(paramExpr) == -1)
                        (closure ?? (closure = new ClosureInfo())).Add(paramExpr);
                    break;

                case ExpressionType.Call:
                    var callExprInfo = expr as MethodCallExpressionInfo;
                    if (callExprInfo != null)
                        return (callExprInfo.Object == null ||
                            TryCollectBoundConstants(ref closure, callExprInfo.Object, paramExprs)) &&
                            TryCollectBoundConstants(ref closure, callExprInfo.Arguments, paramExprs);

                    var callExpr = (MethodCallExpression)expr;
                    return (callExpr.Object == null ||
                        TryCollectBoundConstants(ref closure, callExpr.Object, paramExprs)) &&
                        TryCollectBoundConstants(ref closure, callExpr.Arguments, paramExprs);

                case ExpressionType.MemberAccess:
                    var memberExprInfo = expr as MemberExpressionInfo;
                    if (memberExprInfo != null)
                        return memberExprInfo.Expression == null ||
                               TryCollectBoundConstants(ref closure, memberExprInfo.Expression, paramExprs);

                    var memberExpr = ((MemberExpression)expr).Expression;
                    return memberExpr == null ||
                        TryCollectBoundConstants(ref closure, memberExpr, paramExprs);

                case ExpressionType.New:
                    var newExprInfo = expr as NewExpressionInfo;
                    return newExprInfo != null
                        ? TryCollectBoundConstants(ref closure, newExprInfo.Arguments, paramExprs)
                        : TryCollectBoundConstants(ref closure, ((NewExpression)expr).Arguments, paramExprs);

                case ExpressionType.NewArrayInit:
                    return TryCollectBoundConstants(ref closure, ((NewArrayExpression)expr).Expressions, paramExprs);

                // property initializer
                case ExpressionType.MemberInit:
                    var memberInitExpr = (MemberInitExpression)expr;
                    if (!TryCollectBoundConstants(ref closure, memberInitExpr.NewExpression, paramExprs))
                        return false;

                    var memberBindings = memberInitExpr.Bindings;
                    for (var i = 0; i < memberBindings.Count; ++i)
                    {
                        var memberBinding = memberBindings[i];
                        if (memberBinding.BindingType == MemberBindingType.Assignment &&
                            !TryCollectBoundConstants(ref closure, ((MemberAssignment)memberBinding).Expression, paramExprs))
                            return false;
                    }
                    break;

                // nested lambda
                case ExpressionType.Lambda:

                    // 1. Try to compile nested lambda in place
                    // 2. Check that parameters used in compiled lambda are passed or closed by outer lambda
                    // 3. Add the compiled lambda to closure of outer lambda for later invocation

                    var lambdaExpr = (LambdaExpression)expr;
                    var lambdaParamExprs = lambdaExpr.Parameters;
                    var paramTypes = GetParamExprTypes(lambdaParamExprs);

                    ClosureInfo nestedClosure = null;
                    var lambda = TryCompile(ref nestedClosure,
                        lambdaExpr.Type, paramTypes, lambdaExpr.Body.Type, lambdaExpr.Body, lambdaParamExprs);

                    if (lambda == null)
                        return false;

                    var lambdaInfo = new NestedLambdaInfo(lambda, lambdaExpr, nestedClosure);

                    closure = closure ?? new ClosureInfo();
                    closure.Add(lambdaInfo);

                    // if nested parameter is no matched with any outer parameter, that ensure it goes to outer closure
                    if (nestedClosure != null && nestedClosure.UsedParameters != null)
                    {
                        var nestedClosedParams = nestedClosure.UsedParameters;
                        for (var i = 0; i < nestedClosedParams.Length; i++)
                        {
                            var nestedClosedParamExpr = nestedClosedParams[i];
                            if (paramExprs.Count == 0 || paramExprs.IndexOf(nestedClosedParamExpr) == -1)
                                closure.Add(nestedClosedParamExpr);
                        }
                    }

                    break;

                case ExpressionType.Invoke:
                    var invocationExpr = (InvocationExpression)expr;
                    return TryCollectBoundConstants(ref closure, invocationExpr.Expression, paramExprs)
                        && TryCollectBoundConstants(ref closure, invocationExpr.Arguments, paramExprs);

                case ExpressionType.Conditional:
                    var conditionalExpr = (ConditionalExpression)expr;
                    return TryCollectBoundConstants(ref closure, conditionalExpr.Test, paramExprs)
                        && TryCollectBoundConstants(ref closure, conditionalExpr.IfTrue, paramExprs)
                        && TryCollectBoundConstants(ref closure, conditionalExpr.IfFalse, paramExprs);

                default:
                    var unaryExpr = expr as UnaryExpression;
                    if (unaryExpr != null)
                        return TryCollectBoundConstants(ref closure, unaryExpr.Operand, paramExprs);

                    var binaryExpr = expr as BinaryExpression;
                    if (binaryExpr != null)
                        return TryCollectBoundConstants(ref closure, binaryExpr.Left, paramExprs)
                            && TryCollectBoundConstants(ref closure, binaryExpr.Right, paramExprs);
                    break;
            }

            return true;
        }

        private static bool TryCollectBoundConstants(ref ClosureInfo closure, ExpressionInfo[] exprs, IList<ParameterExpression> paramExprs)
        {
            for (var i = 0; i < exprs.Length; i++)
                if (!TryCollectBoundConstants(ref closure, exprs[i], paramExprs))
                    return false;
            return true;
        }

        private static bool TryCollectBoundConstants(ref ClosureInfo closure, IList<Expression> exprs, IList<ParameterExpression> paramExprs)
        {
            for (var i = 0; i < exprs.Count; i++)
                if (!TryCollectBoundConstants(ref closure, exprs[i], paramExprs))
                    return false;
            return true;
        }

        #endregion

        /// <summary>Supports emitting of selected expressions, e.g. lambdaExpr are not supported yet.
        /// When emitter find not supported expression it will return false from <see cref="TryEmit"/>, so I could fallback
        /// to normal and slow Expression.Compile.</summary>
        private static class EmittingVisitor
        {
            private static readonly MethodInfo _getDelegateTargetProperty = typeof(Delegate).GetTypeInfo()
                .DeclaredMethods.First(m => m.Name == "get_Target");

            private static readonly MethodInfo _getTypeFromHandleMethod = typeof(Type).GetTypeInfo()
                .DeclaredMethods.First(m => m.Name == "GetTypeFromHandle");

            public static bool TryEmit(Expr e, IList<ParameterExpression> paramExprs, ILGenerator il, ClosureInfo closure)
            {
                var expr = e.Expression;
                switch (e.NodeType)
                {
                    case ExpressionType.Parameter:
                        return EmitParameter((ParameterExpression)expr, paramExprs, il, closure);
                    case ExpressionType.Convert:
                        return EmitConvert((UnaryExpression)expr, paramExprs, il, closure);
                    case ExpressionType.ArrayIndex:
                        return EmitArrayIndex((BinaryExpression)expr, paramExprs, il, closure);
                    case ExpressionType.Constant:
                        return EmitConstant(e, il, closure);
                    case ExpressionType.Call:
                        return EmitMethodCall(e, paramExprs, il, closure);
                    case ExpressionType.MemberAccess:
                        return EmitMemberAccess(e, paramExprs, il, closure);
                    case ExpressionType.New:
                        return EmitNew(e, paramExprs, il, closure);
                    case ExpressionType.NewArrayInit:
                        return EmitNewArray((NewArrayExpression)expr, paramExprs, il, closure);
                    case ExpressionType.MemberInit:
                        return EmitMemberInit((MemberInitExpression)expr, paramExprs, il, closure);
                    case ExpressionType.Lambda:
                        return EmitNestedLambda((LambdaExpression)expr, paramExprs, il, closure);

                    case ExpressionType.Invoke:
                        return EmitInvokeLambda((InvocationExpression)expr, paramExprs, il, closure);

                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                        return EmitComparison((BinaryExpression)expr, paramExprs, il, closure);

                    case ExpressionType.AndAlso:
                    case ExpressionType.OrElse:
                        return EmitLogicalOperator((BinaryExpression)expr, paramExprs, il, closure);

                    case ExpressionType.Conditional:
                        return EmitTernararyOperator((ConditionalExpression)expr, paramExprs, il, closure);

                    //case ExpressionType.Coalesce:
                    default:
                        return false;
                }
            }

            private static bool EmitParameter(ParameterExpression p, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var paramIndex = ps.IndexOf(p);

                // if parameter is passed, then just load it on stack
                if (paramIndex != -1)
                {
                    if (closure != null)
                        paramIndex += 1; // shift parameter indices by one, because the first one will be closure
                    LoadParamArg(il, paramIndex);
                    return true;
                }

                // Otherwise (parameter isn't passed) then it is probably passed into outer lambda,
                // so it should be loaded from closure
                if (closure == null)
                    return false;

                var usedParamIndex = closure.UsedParameters.IndexOf(it => it == p);
                if (usedParamIndex == -1)
                    return false;  // what??? no chance

                var closureItemIndex = usedParamIndex + closure.Constants.Length;
                LoadClosureFieldOrItem(il, closure, closureItemIndex, p.Type);
                return true;
            }

            private static void LoadParamArg(ILGenerator il, int paramIndex)
            {
                switch (paramIndex)
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
                        if (paramIndex <= byte.MaxValue)
                            il.Emit(OpCodes.Ldarg_S, (byte)paramIndex);
                        else
                            il.Emit(OpCodes.Ldarg, paramIndex);
                        break;
                }
            }

            private static bool EmitBinary(BinaryExpression e, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                return TryEmit(e.Left, ps, il, closure)
                    && TryEmit(e.Right, ps, il, closure);
            }

            private static bool EmitMany(IList<Expression> es, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                for (int i = 0, n = es.Count; i < n; i++)
                    if (!TryEmit(es[i], ps, il, closure))
                        return false;
                return true;
            }

            private static bool EmitMany(IList<ExpressionInfo> es, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                for (int i = 0, n = es.Count; i < n; i++)
                    if (!TryEmit(es[i], ps, il, closure))
                        return false;
                return true;
            }

            private static bool EmitConvert(UnaryExpression e, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                if (!TryEmit(e.Operand, ps, il, closure))
                    return false;

                var targetType = e.Type;
                if (targetType == typeof(object))
                    return false;

                if (targetType == typeof(int))
                    il.Emit(OpCodes.Conv_I4);
                else if (targetType == typeof(float))
                    il.Emit(OpCodes.Conv_R4);
                else if (targetType == typeof(uint))
                    il.Emit(OpCodes.Conv_U4);
                else if (targetType == typeof(sbyte))
                    il.Emit(OpCodes.Conv_I1);
                else if (targetType == typeof(byte))
                    il.Emit(OpCodes.Conv_U1);
                else if (targetType == typeof(short))
                    il.Emit(OpCodes.Conv_I2);
                else if (targetType == typeof(ushort))
                    il.Emit(OpCodes.Conv_U2);
                else if (targetType == typeof(long))
                    il.Emit(OpCodes.Conv_I8);
                else if (targetType == typeof(ulong))
                    il.Emit(OpCodes.Conv_U8);
                else if (targetType == typeof(double))
                    il.Emit(OpCodes.Conv_R8);
                else
                    il.Emit(OpCodes.Castclass, targetType);

                return true;
            }

            private static bool EmitConstant(Expr e, ILGenerator il, ClosureInfo closure)
            {
                var expr = e.Expression;
                var constExprInfo = expr as ConstantExpressionInfo;
                var constantValue = constExprInfo != null ? constExprInfo.Value : ((ConstantExpression)expr).Value;
                if (constantValue == null)
                {
                    il.Emit(OpCodes.Ldnull);
                    return true;
                }

                var constantActualType = constantValue.GetType();
                if (constantActualType.GetTypeInfo().IsEnum)
                    constantActualType = Enum.GetUnderlyingType(constantActualType);

                if (constantActualType == typeof(int))
                {
                    EmitLoadConstantInt(il, (int)constantValue);
                }
                else if (constantActualType == typeof(char))
                {
                    EmitLoadConstantInt(il, (char)constantValue);
                }
                else if (constantActualType == typeof(short))
                {
                    EmitLoadConstantInt(il, (short)constantValue);
                }
                else if (constantActualType == typeof(byte))
                {
                    EmitLoadConstantInt(il, (byte)constantValue);
                }
                else if (constantActualType == typeof(ushort))
                {
                    EmitLoadConstantInt(il, (ushort)constantValue);
                }
                else if (constantActualType == typeof(sbyte))
                {
                    EmitLoadConstantInt(il, (sbyte)constantValue);
                }
                else if (constantActualType == typeof(uint))
                {
                    unchecked
                    {
                        EmitLoadConstantInt(il, (int)(uint)constantValue);
                    }
                }
                else if (constantActualType == typeof(long))
                {
                    il.Emit(OpCodes.Ldc_I8, (long)constantValue);
                }
                else if (constantActualType == typeof(ulong))
                {
                    unchecked
                    {
                        il.Emit(OpCodes.Ldc_I8, (long)(ulong)constantValue);
                    }
                }
                else if (constantActualType == typeof(float))
                {
                    il.Emit(OpCodes.Ldc_R8, (float)constantValue);
                }
                else if (constantActualType == typeof(double))
                {
                    il.Emit(OpCodes.Ldc_R8, (double)constantValue);
                }
                else if (constantActualType == typeof(bool))
                {
                    il.Emit((bool)constantValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                }
                else if (constantValue is string)
                {
                    il.Emit(OpCodes.Ldstr, (string)constantValue);
                }
                else if (constantValue is Type)
                {
                    il.Emit(OpCodes.Ldtoken, (Type)constantValue);
                    il.Emit(OpCodes.Call, _getTypeFromHandleMethod);
                }
                else if (closure != null)
                {
                    var constantIndex = closure.Constants.IndexOf(it => it.Expression == expr);
                    if (constantIndex == -1)
                        return false;
                    LoadClosureFieldOrItem(il, closure, constantIndex, e.Type);

                }
                else return false;

                // boxing the value type, otherwise we can get a strange result when 0 is treated as Null.
                if (e.Type == typeof(object) && constantActualType.GetTypeInfo().IsValueType)
                    il.Emit(OpCodes.Box, constantValue.GetType()); // using normal type for Enum instead of underlying type

                return true;
            }

            private static void LoadClosureFieldOrItem(ILGenerator il, ClosureInfo closure, int constantIndex, Type constantType)
            {
                // load closure argument: Closure object or Closure array
                il.Emit(OpCodes.Ldarg_0);

                if (!closure.IsArray)
                {
                    // load closure field
                    il.Emit(OpCodes.Ldfld, closure.Fields[constantIndex]);
                    return;
                }

                // load array field
                il.Emit(OpCodes.Ldfld, ArrayClosure.ArrayField);

                // load array item index
                EmitLoadConstantInt(il, constantIndex);

                // load item from index
                il.Emit(OpCodes.Ldelem_Ref);

                // Cast or unbox the object item depending if it is a class or value type
                if (constantType != typeof(object))
                {
                    if (constantType.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Unbox_Any, constantType);
                    else
                        il.Emit(OpCodes.Castclass, constantType);
                }
            }

            private static bool EmitNew(Expr e, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var newExprInfo = e.Expression as NewExpressionInfo;
                if (newExprInfo != null)
                {
                    if (!EmitMany(newExprInfo.Arguments, ps, il, closure))
                        return false;
                    il.Emit(OpCodes.Newobj, newExprInfo.Constructor);
                }
                else
                {
                    var newExpr = (NewExpression)e.Expression;
                    if (!EmitMany(newExpr.Arguments, ps, il, closure))
                        return false;
                    il.Emit(OpCodes.Newobj, newExpr.Constructor);
                }
                return true;
            }

            private static bool EmitNewArray(NewArrayExpression e, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var elems = e.Expressions;
                var arrType = e.Type;
                var elemType = arrType.GetElementType();
                var isElemOfValueType = elemType.GetTypeInfo().IsValueType;

                var arrVar = il.DeclareLocal(arrType);

                EmitLoadConstantInt(il, elems.Count);
                il.Emit(OpCodes.Newarr, elemType);
                il.Emit(OpCodes.Stloc, arrVar);

                for (int i = 0, n = elems.Count; i < n; i++)
                {
                    il.Emit(OpCodes.Ldloc, arrVar);
                    EmitLoadConstantInt(il, i);

                    // loading element address for later copying of value into it.
                    if (isElemOfValueType)
                        il.Emit(OpCodes.Ldelema, elemType);

                    if (!TryEmit(elems[i], ps, il, closure))
                        return false;

                    if (isElemOfValueType)
                        il.Emit(OpCodes.Stobj, elemType); // store element of value type by array element address
                    else
                        il.Emit(OpCodes.Stelem_Ref);
                }

                il.Emit(OpCodes.Ldloc, arrVar);
                return true;
            }

            private static bool EmitArrayIndex(BinaryExpression e, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                if (!EmitBinary(e, ps, il, closure))
                    return false;
                il.Emit(OpCodes.Ldelem_Ref);
                return true;
            }

            private static bool EmitMemberInit(MemberInitExpression mi, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                if (!EmitNew(mi.NewExpression, ps, il, closure))
                    return false;

                var obj = il.DeclareLocal(mi.Type);
                il.Emit(OpCodes.Stloc, obj);

                var bindings = mi.Bindings;
                for (int i = 0, n = bindings.Count; i < n; i++)
                {
                    var binding = bindings[i];
                    if (binding.BindingType != MemberBindingType.Assignment)
                        return false;
                    il.Emit(OpCodes.Ldloc, obj);

                    if (!TryEmit(((MemberAssignment)binding).Expression, ps, il, closure))
                        return false;

                    var prop = binding.Member as PropertyInfo;
                    if (prop != null)
                    {
                        var propSetMethodName = "set_" + prop.Name;
                        var setMethod = prop.DeclaringType.GetTypeInfo()
                            .DeclaredMethods.FirstOrDefault(m => m.Name == propSetMethodName);
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

            private static bool EmitMethodCall(Expr e, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var exprObj = e.Expression;
                var exprInfo = exprObj as MethodCallExpressionInfo;
                if (exprInfo != null)
                {
                    if (exprInfo.Object != null)
                    {
                        if (!TryEmit(exprInfo.Object, ps, il, closure)) return false;
                        IfValueTypeStoreAndLoadValueAddress(il, exprInfo.Object.Type);
                    }

                    if (exprInfo.Arguments.Length != 0 &&
                        !EmitMany(exprInfo.Arguments, ps, il, closure))
                        return false;
                }
                else
                {
                    var expr = (MethodCallExpression)exprObj;
                    if (expr.Object != null)
                    {
                        if (!TryEmit(expr.Object, ps, il, closure)) return false;
                        IfValueTypeStoreAndLoadValueAddress(il, expr.Object.Type);
                    }

                    if (expr.Arguments.Count != 0 &&
                        !EmitMany(expr.Arguments, ps, il, closure))
                        return false;
                }

                var method = exprInfo != null ? exprInfo.Method : ((MethodCallExpression)exprObj).Method;
                EmitMethodCall(method, il);
                return true;
            }

            private static void IfValueTypeStoreAndLoadValueAddress(ILGenerator il, Type ownerType)
            {
                if (ownerType.GetTypeInfo().IsValueType)
                {
                    var valueVar = il.DeclareLocal(ownerType);
                    il.Emit(OpCodes.Stloc, valueVar);
                    il.Emit(OpCodes.Ldloca, valueVar);
                }
            }

            private static bool EmitMemberAccess(Expr e, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                var exprObj = e.Expression;
                var exprInfo = exprObj as MemberExpressionInfo;
                if (exprInfo != null)
                {
                    if (exprInfo.Expression != null)
                    {
                        if (!TryEmit(exprInfo.Expression, ps, il, closure)) return false;
                        IfValueTypeStoreAndLoadValueAddress(il, exprInfo.Expression.Type);
                    }
                }
                else
                {
                    var instanceExpr = ((MemberExpression)exprObj).Expression;
                    if (instanceExpr != null)
                    {
                        if (!TryEmit(instanceExpr, ps, il, closure)) return false;
                        IfValueTypeStoreAndLoadValueAddress(il, instanceExpr.Type);
                    }
                }

                var member = exprInfo != null ? exprInfo.Member : ((MemberExpression)exprObj).Member;
                var field = member as FieldInfo;
                if (field != null)
                {
                    il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
                    return true;
                }

                var prop = member as PropertyInfo;
                if (prop != null)
                {
                    var propGetMethodName = "get_" + prop.Name;
                    var getMethod = prop.DeclaringType.GetTypeInfo()
                        .DeclaredMethods.FirstOrDefault(m => m.Name == propGetMethodName);
                    if (getMethod == null)
                        return false;
                    EmitMethodCall(getMethod, il);
                }
                return true;
            }

            private static bool EmitNestedLambda(LambdaExpression lambdaExpr, IList<ParameterExpression> paramExprs, ILGenerator il, ClosureInfo closure)
            {
                // First, find in closed compiled lambdas the one corresponding to the current lambda expression.
                var nestedLambdas = closure.NestedLambdas;
                var nestedLambdaIndex = nestedLambdas.Length - 1;
                while (nestedLambdaIndex >= 0 && nestedLambdas[nestedLambdaIndex].LambdaExpr != lambdaExpr)
                    --nestedLambdaIndex;

                // Situation with not found lambda is not possible/exceptional - 
                // means that we somehow skipped the lambda expression while collecting closure info.
                if (nestedLambdaIndex == -1)
                    return false;

                var nestedLambdaInfo = nestedLambdas[nestedLambdaIndex];
                var nestedLambda = nestedLambdaInfo.Lambda;
                var nestedLambdaType = nestedLambda.GetType();

                // Next we are loading compiled lambda on stack
                var closureItemIndex = nestedLambdaIndex + closure.Constants.Length + closure.UsedParameters.Length;
                LoadClosureFieldOrItem(il, closure, closureItemIndex, nestedLambdaType);

                // If lambda does not use any outer parameters to be set in closure, then we're done
                var nestedLambdaClosure = nestedLambdaInfo.ClosureInfo;
                if (nestedLambdaClosure == null ||
                    nestedLambdaClosure.UsedParameters == null)
                    return true;

                // Sets closure param placeholder fields to the param values 
                var nestedLambdaUsedParamExprs = nestedLambdaClosure.UsedParameters;
                for (var i = 0; i < nestedLambdaUsedParamExprs.Length; i++)
                {
                    var nestedUsedParamExpr = nestedLambdaUsedParamExprs[i];

                    // copy lambda field on stack in order to set it Target.Param to param value
                    il.Emit(OpCodes.Dup);

                    // load lambda.Target property
                    EmitMethodCall(_getDelegateTargetProperty, il);

                    // params go after constants
                    var nestedUsedParamIndex = i + nestedLambdaClosure.Constants.Length;

                    if (nestedLambdaClosure.IsArray)
                    {
                        // load array
                        il.Emit(OpCodes.Ldfld, ArrayClosure.ArrayField);

                        // load array item index
                        EmitLoadConstantInt(il, nestedUsedParamIndex);
                    }

                    var paramIndex = paramExprs.IndexOf(nestedUsedParamExpr);
                    if (paramIndex != -1) // load param from input params
                    {
                        // +1 is set cause of added first closure argument
                        LoadParamArg(il, paramIndex + 1);
                    }
                    else // load parameter from outer closure
                    {
                        if (closure.UsedParameters == null)
                            return false; // impossible, better to throw?

                        var outerClosureParamIndex = closure.UsedParameters.IndexOf(it => it == nestedUsedParamExpr);
                        if (outerClosureParamIndex == -1)
                            return false; // impossible, better to throw?

                        var outerClosureParamItemIndex = closure.Constants.Length + outerClosureParamIndex;
                        LoadClosureFieldOrItem(il, closure, outerClosureParamItemIndex, nestedUsedParamExpr.Type);
                    }

                    if (nestedLambdaClosure.IsArray)
                    {
                        // box value types before setting the object array item
                        if (nestedUsedParamExpr.Type.GetTypeInfo().IsValueType)
                            il.Emit(OpCodes.Box, nestedUsedParamExpr.Type);

                        // load item from index
                        il.Emit(OpCodes.Stelem_Ref);
                    }
                    else
                    {
                        var closedParamField = nestedLambdaClosure.Fields[nestedUsedParamIndex];
                        il.Emit(OpCodes.Stfld, closedParamField);
                    }
                }

                return true;
            }

            private static bool EmitInvokeLambda(InvocationExpression e, IList<ParameterExpression> paramExprs, ILGenerator il, ClosureInfo closure)
            {
                if (!TryEmit(e.Expression, paramExprs, il, closure) ||
                    !EmitMany(e.Arguments, paramExprs, il, closure))
                    return false;

                var invokeMethod = e.Expression.Type.GetTypeInfo().DeclaredMethods.First(m => m.Name == "Invoke");
                EmitMethodCall(invokeMethod, il);
                return true;
            }

            private static bool EmitComparison(BinaryExpression e, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                if (!TryEmit(e.Left, ps, il, closure) ||
                    !TryEmit(e.Right, ps, il, closure))
                    return false;

                switch (e.NodeType)
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
                return true;
            }

            private static bool EmitLogicalOperator(BinaryExpression e, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                if (!TryEmit(e.Left, ps, il, closure))
                    return false;

                var labelSkipRight = il.DefineLabel();
                var isAnd = e.NodeType == ExpressionType.AndAlso;
                il.Emit(isAnd ? OpCodes.Brfalse_S : OpCodes.Brtrue_S, labelSkipRight);

                if (!TryEmit(e.Right, ps, il, closure))
                    return false;
                var labelDone = il.DefineLabel();
                il.Emit(OpCodes.Br, labelDone);

                il.MarkLabel(labelSkipRight); // label the second branch
                il.Emit(isAnd ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);

                il.MarkLabel(labelDone);
                return true;
            }

            private static bool EmitTernararyOperator(ConditionalExpression e, IList<ParameterExpression> ps, ILGenerator il, ClosureInfo closure)
            {
                if (!TryEmit(e.Test, ps, il, closure))
                    return false;

                var labelIfFalse = il.DefineLabel();
                il.Emit(OpCodes.Brfalse_S, labelIfFalse);

                if (!TryEmit(e.IfTrue, ps, il, closure))
                    return false;

                var labelDone = il.DefineLabel();
                il.Emit(OpCodes.Br, labelDone);

                il.MarkLabel(labelIfFalse);
                if (!TryEmit(e.IfFalse, ps, il, closure))
                    return false;

                il.MarkLabel(labelDone);
                return true;
            }

            private static void EmitMethodCall(MethodInfo method, ILGenerator il)
            {
                il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
            }

            private static void EmitLoadConstantInt(ILGenerator il, int i)
            {
                switch (i)
                {
                    case -1:
                        il.Emit(OpCodes.Ldc_I4_M1);
                        break;
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

    /// <summary>Base expression.</summary>
    public abstract class ExpressionInfo
    {
        /// <summary>Expression node type.</summary>
        public abstract ExpressionType NodeType { get; }

        /// <summary>All expressions should have a Type.</summary>
        public abstract Type Type { get; }

        /// <summary>Analog of Expression.Constant</summary>
        public static ConstantExpressionInfo Constant(object value, Type type = null)
        {
            return new ConstantExpressionInfo(value, type);
        }

        /// <summary>Analog of Expression.New</summary>
        public static NewExpressionInfo New(ConstructorInfo ctor, params ExpressionInfo[] arguments)
        {
            return new NewExpressionInfo(ctor, arguments);
        }

        /// <summary>Static method call</summary>
        public static MethodCallExpressionInfo Call(MethodInfo method, params ExpressionInfo[] arguments)
        {
            return new MethodCallExpressionInfo(null, method, arguments);
        }

        /// <summary>Instance method call</summary>
        public static MethodCallExpressionInfo Call(
            ExpressionInfo instance, MethodInfo method, params ExpressionInfo[] arguments)
        {
            return new MethodCallExpressionInfo(instance, method, arguments);
        }

        /// <summary>Static property</summary>
        public static PropertyExpressionInfo Property(PropertyInfo property)
        {
            return new PropertyExpressionInfo(null, property);
        }

        /// <summary>Instance property</summary>
        public static PropertyExpressionInfo Property(ExpressionInfo instance, PropertyInfo property)
        {
            return new PropertyExpressionInfo(instance, property);
        }

        /// <summary>Analog of Expression.Lambda</summary>
        public static LambdaExpressionInfo Lambda(ExpressionInfo body, params ParameterExpression[] parameters)
        {
            return new LambdaExpressionInfo(body, parameters);
        }
    }

    /// <summary>Analog of ConstantExpression.</summary>
    public class ConstantExpressionInfo : ExpressionInfo
    {
        /// <inheritdoc />
        public override ExpressionType NodeType { get { return ExpressionType.Constant; } }

        /// <inheritdoc />
        public override Type Type { get; }

        /// <summary>Value of constant.</summary>
        public readonly object Value;

        /// <summary>Constructor</summary>
        public ConstantExpressionInfo(object value, Type type = null)
        {
            Value = value;
            Type = type ?? (value == null ? typeof(object) : value.GetType());
        }
    }

    /// <summary>Base class for expressions with arguments.</summary>
    public abstract class ArgumentsExpressionInfo : ExpressionInfo
    {
        /// <summary>List of arguments</summary>
        public readonly ExpressionInfo[] Arguments;

        /// <summary>Constructor</summary>
        protected ArgumentsExpressionInfo(ExpressionInfo[] arguments)
        {
            Arguments = arguments;
        }
    }

    /// <summary>Analog of NewExpression</summary>
    public class NewExpressionInfo : ArgumentsExpressionInfo
    {
        /// <inheritdoc />
        public override ExpressionType NodeType { get { return ExpressionType.New; } }

        /// <inheritdoc />
        public override Type Type { get { return Constructor.DeclaringType; } }

        /// <summary>The constructor info.</summary>
        public readonly ConstructorInfo Constructor;

        /// <summary>Construct from constructor info and argument expressions</summary>
        public NewExpressionInfo(ConstructorInfo constructor, params ExpressionInfo[] arguments) : base(arguments)
        {
            Constructor = constructor;
        }
    }

    /// <summary>Analog of MethodCallExpression</summary>
    public class MethodCallExpressionInfo : ArgumentsExpressionInfo
    {
        /// <inheritdoc />
        public override ExpressionType NodeType { get { return ExpressionType.Call; } }

        /// <inheritdoc />
        public override Type Type { get { return Method.ReturnType; } }

        /// <summary>The method info.</summary>
        public readonly MethodInfo Method;

        /// <summary>Instance expression, null if static.</summary>
        public readonly ExpressionInfo Object;

        /// <summary>Construct from method info and argument expressions</summary>
        public MethodCallExpressionInfo(
            ExpressionInfo @object, MethodInfo method, params ExpressionInfo[] arguments) : base(arguments)
        {
            Object = @object;
            Method = method;
        }
    }

    /// <summary>Analog of MemberExpression</summary>
    public abstract class MemberExpressionInfo : ExpressionInfo
    {
        /// <inheritdoc />
        public override ExpressionType NodeType { get { return ExpressionType.MemberAccess; } }

        /// <summary>Member info.</summary>
        public readonly MemberInfo Member;

        /// <summary>Instance expression, null if static.</summary>
        public readonly ExpressionInfo Expression;

        /// <summary>Constructs with</summary>
        protected MemberExpressionInfo(ExpressionInfo expression, MemberInfo member)
        {
            Expression = expression;
            Member = member;
        }
    }

    /// <summary>Analog of PropertyExpression</summary>
    public class PropertyExpressionInfo : MemberExpressionInfo
    {
        /// <inheritdoc />
        public override Type Type { get { return ((PropertyInfo)Member).PropertyType; } }

        /// <summary>Construct from property info</summary>
        public PropertyExpressionInfo(ExpressionInfo instance, PropertyInfo property)
            : base(instance, property) { }
    }

    /// <summary>LambdaExpression</summary>
    public class LambdaExpressionInfo : ExpressionInfo
    {
        /// <inheritdoc />
        public override ExpressionType NodeType { get { return ExpressionType.Lambda; } }

        /// <inheritdoc />
        public override Type Type { get { return Body.Type; } }

        /// <summary>Lambda body.</summary>
        public readonly ExpressionInfo Body;

        /// <summary>List of parameters.</summary>
        public readonly ParameterExpression[] Parameters;

        /// <summary>Constructor</summary>
        public LambdaExpressionInfo(ExpressionInfo body, ParameterExpression[] parameters)
        {
            Body = body;
            Parameters = parameters;
        }
    }

    /// <summary>Typedclambda expression.</summary>
    public sealed class ExpressionInfo<TDelegate> : LambdaExpressionInfo
    {
        /// <summary>Tyoe of lambda</summary>
        public Type DelegateType { get { return typeof(TDelegate); } }

        /// <summary>Constructor</summary>
        public ExpressionInfo(ExpressionInfo body, ParameterExpression[] parameters) : base(body, parameters) { }
    }
}
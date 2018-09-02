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

// ReSharper disable CoVariantArrayConversion

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression
#else
namespace FastExpressionCompiler
#endif
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>Compiles expression to delegate ~20 times faster than Expression.Compile.
    /// Partial to extend with your things when used as source file.</summary>
    // ReSharper disable once PartialTypeWithSinglePart
    public static partial class ExpressionCompiler
    {
        #region Expression.CompileFast overloads for Delegate, Funcs, and Actions

        /// <summary>Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static TDelegate CompileFast<TDelegate>(this LambdaExpression lambdaExpr, bool ifFastFailedReturnNull = false) where TDelegate : class =>
            TryCompile<TDelegate>(lambdaExpr.Body, lambdaExpr.Parameters, Tools.GetParamTypes(lambdaExpr.Parameters), lambdaExpr.ReturnType) ??
            (ifFastFailedReturnNull ? null : (TDelegate)(object)lambdaExpr
#if LIGHT_EXPRESSION
                .ToLambdaExpression()
#endif
                .Compile());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Delegate CompileFast(this LambdaExpression lambdaExpr, bool ifFastFailedReturnNull = false) =>
            lambdaExpr.CompileFast<Delegate>(ifFastFailedReturnNull);

        private static TDelegate CompileSys<TDelegate>(this Expression<TDelegate> lambdaExpr) where TDelegate : class =>
            lambdaExpr
#if LIGHT_EXPRESSION
            .ToLambdaExpression()
#endif
            .Compile();

        /// <summary>Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static TDelegate CompileFast<TDelegate>(this Expression<TDelegate> lambdaExpr, bool ifFastFailedReturnNull = false)
            where TDelegate : class => ((LambdaExpression)lambdaExpr).CompileFast<TDelegate>(ifFastFailedReturnNull);

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<R> CompileFast<R>(this Expression<Func<R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<R>>(lambdaExpr.Body, lambdaExpr.Parameters, Tools.Empty<Type>(), typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, R> CompileFast<T1, R>(this Expression<Func<T1, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, R> CompileFast<T1, T2, R>(this Expression<Func<T1, T2, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, R> CompileFast<T1, T2, T3, R>(
            this Expression<Func<T1, T2, T3, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to TDelegate type. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, T4, R> CompileFast<T1, T2, T3, T4, R>(
            this Expression<Func<T1, T2, T3, T4, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, T4, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, T4, T5, R> CompileFast<T1, T2, T3, T4, T5, R>(
            this Expression<Func<T1, T2, T3, T4, T5, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, T4, T5, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Func<T1, T2, T3, T4, T5, T6, R> CompileFast<T1, T2, T3, T4, T5, T6, R>(
            this Expression<Func<T1, T2, T3, T4, T5, T6, R>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Func<T1, T2, T3, T4, T5, T6, R>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }, typeof(R))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action CompileFast(this Expression<Action> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action>(lambdaExpr.Body, lambdaExpr.Parameters, Tools.Empty<Type>(), typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1> CompileFast<T1>(this Expression<Action<T1>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2> CompileFast<T1, T2>(this Expression<Action<T1, T2>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3> CompileFast<T1, T2, T3>(this Expression<Action<T1, T2, T3>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3, T4> CompileFast<T1, T2, T3, T4>(
            this Expression<Action<T1, T2, T3, T4>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3, T4>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3, T4, T5> CompileFast<T1, T2, T3, T4, T5>(
            this Expression<Action<T1, T2, T3, T4, T5>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3, T4, T5>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        /// <summary>Compiles lambda expression to delegate. Use ifFastFailedReturnNull parameter to Not fallback to Expression.Compile, useful for testing.</summary>
        public static Action<T1, T2, T3, T4, T5, T6> CompileFast<T1, T2, T3, T4, T5, T6>(
            this Expression<Action<T1, T2, T3, T4, T5, T6>> lambdaExpr, bool ifFastFailedReturnNull = false) =>
            TryCompile<Action<T1, T2, T3, T4, T5, T6>>(lambdaExpr.Body, lambdaExpr.Parameters, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }, typeof(void))
            ?? (ifFastFailedReturnNull ? null : lambdaExpr.CompileSys());

        #endregion

        /// <summary>Tries to compile lambda expression to <typeparamref name="TDelegate"/></summary>
        public static TDelegate TryCompile<TDelegate>(this LambdaExpression lambdaExpr) where TDelegate : class =>
            TryCompile<TDelegate>(lambdaExpr.Body, lambdaExpr.Parameters, Tools.GetParamTypes(lambdaExpr.Parameters), lambdaExpr.ReturnType);

        /// <summary>Tries to compile lambda expression to <typeparamref name="TDelegate"/> 
        /// with the provided closure object and constant expressions (or lack there of) -
        /// Constant expression should be the in order of Fields in closure object!
        /// Note 1: Use it on your own risk - FEC won't verify the expression is compile-able with passed closure, it is up to you!
        /// Note 2: The expression with NESTED LAMBDA IS NOT SUPPORTED!</summary>
        public static TDelegate TryCompileWithPreCreatedClosure<TDelegate>(this LambdaExpression lambdaExpr,
            object closure, params ConstantExpression[] closureConstantsExprs)
            where TDelegate : class
        {
            var closureInfo = new ClosureInfo(true, closure, closureConstantsExprs);
            var bodyExpr = lambdaExpr.Body;
            var returnType = bodyExpr.Type;
            var paramExprs = lambdaExpr.Parameters;
            return (TDelegate)TryCompile(ref closureInfo, typeof(TDelegate), Tools.GetParamTypes(paramExprs), returnType, bodyExpr, returnType, paramExprs);
        }

        /// <summary>Tries to compile expression to "static" delegate, skipping the step of collecting the closure object.</summary>
        public static TDelegate TryCompileWithoutClosure<TDelegate>(this LambdaExpression lambdaExpr)
            where TDelegate : class => lambdaExpr.TryCompileWithPreCreatedClosure<TDelegate>(null, null);

        /// <summary>Compiles expression to delegate by emitting the IL. 
        /// If sub-expressions are not supported by emitter, then the method returns null.
        /// The usage should be calling the method, if result is null then calling the Expression.Compile.</summary>
        public static TDelegate TryCompile<TDelegate>(
            Expression bodyExpr, IReadOnlyList<ParameterExpression> paramExprs, Type[] paramTypes, Type returnType)
            where TDelegate : class
        {
            var ignored = new ClosureInfo(false);
            return (TDelegate)TryCompile(ref ignored, typeof(TDelegate), 
                paramTypes, returnType, bodyExpr, bodyExpr.Type, paramExprs);
        }

        private static object TryCompile(ref ClosureInfo closureInfo,
            Type delegateType, Type[] paramTypes, Type returnType, Expression expr, Type exprType, 
            IReadOnlyList<ParameterExpression> paramExprs, bool isNestedLambda = false)
        {
            object closure;
            if (closureInfo.IsClosureConstructed)
                closure = closureInfo.Closure;
            else if (TryCollectBoundConstants(ref closureInfo, expr, paramExprs))
                closure = closureInfo.ConstructClosureTypeAndObject(constructTypeOnly: isNestedLambda);
            else
                return null;

            if (closureInfo.LabelCount > 0)
                closureInfo.Labels = new KeyValuePair<object, Label>[closureInfo.LabelCount];
            closureInfo.LabelCount = 0;

            var closureType = closureInfo.ClosureType;
            var methodParamTypes = closureType == null ? paramTypes : GetClosureAndParamTypes(paramTypes, closureType);

            var method = new DynamicMethod(string.Empty, returnType, methodParamTypes,
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = method.GetILGenerator();
            if (!EmittingVisitor.TryEmit(expr, exprType, paramExprs, il, ref closureInfo, ExpressionType.Default))
                return null;

            // user requested delegate without return, but inner lambda returns 
            if (returnType == typeof(void) && exprType != typeof(void) && !IsByReferenceReturn(paramTypes, expr))
                il.Emit(OpCodes.Pop); // discard the return value on stack (#71)

            il.Emit(OpCodes.Ret);

            // include closure as the first parameter, BUT don't bound to it. It will be bound later in EmitNestedLambda.
            if (isNestedLambda)
                delegateType = Tools.GetFuncOrActionType(methodParamTypes, returnType);
            // create a specific delegate if user requested delegate is untyped, otherwise CreateMethod will fail
            else if (delegateType == typeof(Delegate))
                delegateType = Tools.GetFuncOrActionType(paramTypes, returnType);

            return method.CreateDelegate(delegateType, closure);
        }

        // if expression body last statement is assign of by ref, then it gets as return in type, but not on stack
        private static bool IsByReferenceReturn(Type[] paramTypes, Expression expr)
        {
            for (var i = 0; i < paramTypes.Length; i++)
            {
                if (!paramTypes[i].IsByRef)
                    continue;

                var blockExpr = expr as BlockExpression;
                if (blockExpr != null)
                {
                    var blockStatements = blockExpr.Expressions;
                    if (blockStatements.Count != 0) // get the last statement from the block
                        expr = blockStatements[blockStatements.Count - 1];
                }

                var leftVar = (expr as BinaryExpression)?.Left as ParameterExpression;
                if (leftVar == null || !leftVar.IsByRef)
                    return false;

                var nodeType = expr.NodeType;
                return nodeType == ExpressionType.Assign
                       || nodeType == ExpressionType.PostDecrementAssign
                       || nodeType == ExpressionType.PostIncrementAssign
                       || nodeType == ExpressionType.PreDecrementAssign
                       || nodeType == ExpressionType.PreIncrementAssign
                       || Tools.GetArithmeticFromArithmeticAssignOrSelf(nodeType) != nodeType;
            }

            return false;
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

        private sealed class BlockInfo
        {
            public static readonly BlockInfo Empty = new BlockInfo();
            private BlockInfo() { }

            public bool IsEmpty => Parent == null;
            public readonly BlockInfo Parent;
            public readonly Expression ResultExpr;
            public readonly IReadOnlyList<ParameterExpression> VarExprs;
            public readonly LocalBuilder[] LocalVars;

            internal BlockInfo(BlockInfo parent,
                Expression resultExpr, IReadOnlyList<ParameterExpression> varExprs, LocalBuilder[] localVars)
            {
                Parent = parent;
                ResultExpr = resultExpr;
                VarExprs = varExprs;
                LocalVars = localVars;
            }
        }

        // Track the info required to build a closure object + some context information not directly related to closure.
        private struct ClosureInfo
        {
            public bool IsClosureConstructed;

            // Constructed closure object.
            public readonly object Closure;

            // Type of constructed closure, may be available even without closure object (in case of nested lambda)
            public Type ClosureType;

            public bool HasClosure => ClosureType != null;

            // Constant expressions to find an index (by reference) of constant expression from compiled expression.
            public ConstantExpression[] Constants;

            // Parameters not passed through lambda parameter list But used inside lambda body.
            // The top expression should not! contain non passed parameters. 
            public ParameterExpression[] NonPassedParameters;

            // All nested lambdas recursively nested in expression
            public NestedLambdaInfo[] NestedLambdas;

            public int ClosedItemCount => Constants.Length + NonPassedParameters.Length + NestedLambdas.Length;

            // FieldInfos are needed to load field of closure object on stack in emitter.
            // It is also an indicator that we use typed Closure object and not an array.
            public FieldInfo[] ClosureFields;

            // Helper to decide whether we are inside the block or not
            public BlockInfo CurrentBlock;

            public int LabelCount;
            // Dictionary for the used Labels in IL
            public KeyValuePair<object, Label>[] Labels;

            // Populates info directly with provided closure object and constants.
            public ClosureInfo(bool isConstructed, object closure = null, ConstantExpression[] closureConstantExpressions = null)
            {
                IsClosureConstructed = isConstructed;

                NonPassedParameters = Tools.Empty<ParameterExpression>();
                NestedLambdas = Tools.Empty<NestedLambdaInfo>();
                CurrentBlock = BlockInfo.Empty;
                Labels = null;
                LabelCount = 0;

                if (closure == null)
                {
                    Closure = null;
                    Constants = Tools.Empty<ConstantExpression>();
                    ClosureType = null;
                    ClosureFields = null;
                }
                else
                {
                    Closure = closure;
                    Constants = closureConstantExpressions ?? Tools.Empty<ConstantExpression>();
                    ClosureType = closure.GetType();
                    // todo: verify that Fields types are correspond to `closureConstantExpressions`
                    ClosureFields = ClosureType.GetTypeInfo().DeclaredFields.AsArray();
                }
            }

            public void AddConstant(ConstantExpression expr)
            {
                if (Constants.Length == 0 ||
                    Constants.GetFirstIndex(expr) == -1)
                    Constants = Constants.WithLast(expr);
            }

            public void AddNonPassedParam(ParameterExpression expr)
            {
                if (NonPassedParameters.Length == 0 ||
                    NonPassedParameters.GetFirstIndex(expr) == -1)
                    NonPassedParameters = NonPassedParameters.WithLast(expr);
            }

            public void AddNestedLambda(LambdaExpression lambdaExpr, object lambda, ref ClosureInfo closureInfo, bool isAction)
            {
                if (NestedLambdas.Length == 0 ||
                    NestedLambdas.GetFirstIndex(x => x.LambdaExpr == lambdaExpr) == -1)
                    NestedLambdas = NestedLambdas.WithLast(new NestedLambdaInfo(closureInfo, lambdaExpr, lambda, isAction));
            }

            public void AddNestedLambda(NestedLambdaInfo info)
            {
                if (NestedLambdas.Length == 0 ||
                    NestedLambdas.GetFirstIndex(x => x.LambdaExpr == info.LambdaExpr) == -1)
                    NestedLambdas = NestedLambdas.WithLast(info);
            }

            public object ConstructClosureTypeAndObject(bool constructTypeOnly)
            {
                IsClosureConstructed = true;

                var constants = Constants;
                var nonPassedParams = NonPassedParameters;
                var nestedLambdas = NestedLambdas;
                if (constants.Length == 0 && nonPassedParams.Length == 0 && nestedLambdas.Length == 0)
                    return null;

                var constPlusParamCount = constants.Length + nonPassedParams.Length;
                var totalItemCount = constPlusParamCount + nestedLambdas.Length;

                // Construct the array based closure when number of values is bigger than
                // number of fields in biggest supported Closure class.
                var createMethods = ExpressionCompiler.Closure.CreateMethods;
                if (totalItemCount > createMethods.Length)
                {
                    ClosureType = typeof(ArrayClosure);
                    if (constructTypeOnly)
                        return null;

                    var items = new object[totalItemCount];
                    if (constants.Length != 0)
                        for (var i = 0; i < constants.Length; i++)
                            items[i] = constants[i].Value;

                    // skip non passed parameters as it is only for nested lambdas

                    if (nestedLambdas.Length != 0)
                        for (var i = 0; i < nestedLambdas.Length; i++)
                            items[constPlusParamCount + i] = nestedLambdas[i].Lambda;

                    return new ArrayClosure(items);
                }

                // Construct the Closure Type and optionally Closure object with closed values stored as fields:
                object[] fieldValues = null;
                var fieldTypes = new Type[totalItemCount];
                if (constructTypeOnly)
                {
                    if (constants.Length != 0)
                        for (var i = 0; i < constants.Length; i++)
                            fieldTypes[i] = constants[i].Type;

                    if (nonPassedParams.Length != 0)
                        for (var i = 0; i < nonPassedParams.Length; i++)
                            fieldTypes[constants.Length + i] = nonPassedParams[i].Type;

                    if (nestedLambdas.Length != 0)
                        for (var i = 0; i < nestedLambdas.Length; i++)
                            fieldTypes[constPlusParamCount + i] = nestedLambdas[i].Lambda.GetType(); // compiled lambda type
                }
                else
                {
                    fieldValues = new object[totalItemCount];

                    if (constants.Length != 0)
                        for (var i = 0; i < constants.Length; i++)
                        {
                            var constantExpr = constants[i];
                            if (constantExpr != null)
                            {
                                fieldTypes[i] = constantExpr.Type;
                                fieldValues[i] = constantExpr.Value;
                            }
                        }

                    if (nonPassedParams.Length != 0)
                        for (var i = 0; i < nonPassedParams.Length; i++)
                            fieldTypes[constants.Length + i] = nonPassedParams[i].Type;

                    if (nestedLambdas.Length != 0)
                        for (var i = 0; i < nestedLambdas.Length; i++)
                        {
                            var lambda = nestedLambdas[i].Lambda;
                            fieldValues[constPlusParamCount + i] = lambda;
                            fieldTypes[constPlusParamCount + i] = lambda.GetType();
                        }
                }

                var createClosure = createMethods[totalItemCount - 1].MakeGenericMethod(fieldTypes);
                ClosureType = createClosure.ReturnType;
                ClosureFields = ClosureType.GetTypeInfo().DeclaredFields.AsArray();

                return constructTypeOnly ? null : createClosure.Invoke(null, fieldValues);
            }

            public void PushBlock(Expression blockResultExpr, IReadOnlyList<ParameterExpression> blockVarExprs, LocalBuilder[] localVars) =>
                CurrentBlock = new BlockInfo(CurrentBlock, blockResultExpr, blockVarExprs, localVars);

            public void PushBlockAndConstructLocalVars(Expression blockResultExpr, IReadOnlyList<ParameterExpression> blockVarExprs, ILGenerator il)
            {
                var localVars = Tools.Empty<LocalBuilder>();
                if (blockVarExprs.Count != 0)
                {
                    localVars = new LocalBuilder[blockVarExprs.Count];
                    for (var i = 0; i < localVars.Length; i++)
                        localVars[i] = il.DeclareLocal(blockVarExprs[i].Type);
                }

                CurrentBlock = new BlockInfo(CurrentBlock, blockResultExpr, blockVarExprs, localVars);
            }

            public void PopBlock() =>
                CurrentBlock = CurrentBlock.Parent;

            public bool IsLocalVar(object varParamExpr)
            {
                var i = -1;
                for (var block = CurrentBlock; i == -1 && !block.IsEmpty; block = block.Parent)
                    i = block.VarExprs.GetFirstIndex(varParamExpr);
                return i != -1;
            }

            public LocalBuilder GetDefinedLocalVarOrDefault(ParameterExpression varParamExpr)
            {
                for (var block = CurrentBlock; !block.IsEmpty; block = block.Parent)
                {
                    if (block.LocalVars.Length == 0)
                        continue;
                    var varIndex = block.VarExprs.GetFirstIndex(varParamExpr);
                    if (varIndex != -1)
                        return block.LocalVars[varIndex];
                }
                return null;
            }
        }

        #region Closures

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static class Closure
        {
            private static readonly IEnumerable<MethodInfo> _methods = typeof(Closure).GetTypeInfo().DeclaredMethods;
            internal static readonly MethodInfo[] CreateMethods = _methods.AsArray();

            public static Closure<T1> Create<T1>(T1 v1) => new Closure<T1>(v1);

            public static Closure<T1, T2> Create<T1, T2>(T1 v1, T2 v2) => new Closure<T1, T2>(v1, v2);

            public static Closure<T1, T2, T3> Create<T1, T2, T3>(T1 v1, T2 v2, T3 v3) =>
                new Closure<T1, T2, T3>(v1, v2, v3);

            public static Closure<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 v1, T2 v2, T3 v3, T4 v4) =>
                new Closure<T1, T2, T3, T4>(v1, v2, v3, v4);

            public static Closure<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 v1, T2 v2, T3 v3, T4 v4,
                T5 v5) => new Closure<T1, T2, T3, T4, T5>(v1, v2, v3, v4, v5);

            public static Closure<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 v1, T2 v2, T3 v3,
                T4 v4, T5 v5, T6 v6) => new Closure<T1, T2, T3, T4, T5, T6>(v1, v2, v3, v4, v5, v6);

            public static Closure<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 v1, T2 v2,
                T3 v3, T4 v4, T5 v5, T6 v6, T7 v7) =>
                new Closure<T1, T2, T3, T4, T5, T6, T7>(v1, v2, v3, v4, v5, v6, v7);

            public static Closure<T1, T2, T3, T4, T5, T6, T7, T8> Create<T1, T2, T3, T4, T5, T6, T7, T8>(
                T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8) =>
                new Closure<T1, T2, T3, T4, T5, T6, T7, T8>(v1, v2, v3, v4, v5, v6, v7, v8);

            public static Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
                T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9) =>
                new Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9>(v1, v2, v3, v4, v5, v6, v7, v8, v9);

            public static Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
                T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10) =>
                new Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10);
        }

        public sealed class Closure<T1>
        {
            public T1 V1;
            public Closure(T1 v1) { V1 = v1; }
        }

        public sealed class Closure<T1, T2>
        {
            public T1 V1;
            public T2 V2;
            public Closure(T1 v1, T2 v2) { V1 = v1; V2 = v2; }
        }

        public sealed class Closure<T1, T2, T3>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public Closure(T1 v1, T2 v2, T3 v3) { V1 = v1; V2 = v2; V3 = v3; }
        }

        public sealed class Closure<T1, T2, T3, T4>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; }
        }

        public sealed class Closure<T1, T2, T3, T4, T5>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; }
        }

        public sealed class Closure<T1, T2, T3, T4, T5, T6>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; V6 = v6; }
        }

        public sealed class Closure<T1, T2, T3, T4, T5, T6, T7>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public T7 V7;
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; V6 = v6; V7 = v7; }
        }

        public sealed class Closure<T1, T2, T3, T4, T5, T6, T7, T8>
        {
            public T1 V1;
            public T2 V2;
            public T3 V3;
            public T4 V4;
            public T5 V5;
            public T6 V6;
            public T7 V7;
            public T8 V8;
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; V6 = v6; V7 = v7; V8 = v8; }
        }

        public sealed class Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9>
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

            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; V6 = v6; V7 = v7; V8 = v8; V9 = v9; }
        }

        public sealed class Closure<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
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
            public Closure(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10) { V1 = v1; V2 = v2; V3 = v3; V4 = v4; V5 = v5; V6 = v6; V7 = v7; V8 = v8; V9 = v9; V10 = v10; }
        }

        public sealed class ArrayClosure
        {
            public readonly object[] Constants;

            public static FieldInfo ArrayField = typeof(ArrayClosure).GetTypeInfo().GetDeclaredField(nameof(Constants));
            public static ConstructorInfo Constructor = typeof(ArrayClosure).GetTypeInfo().DeclaredConstructors.GetFirst();

            public ArrayClosure(object[] constants) { Constants = constants; }
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion

        #region Nested Lambdas

        private struct NestedLambdaInfo
        {
            public readonly ClosureInfo ClosureInfo;
            public readonly LambdaExpression LambdaExpr; // to find the nested lambda in bigger parent expression
            public readonly object Lambda;
            public readonly bool IsAction;

            public NestedLambdaInfo(ClosureInfo closureInfo, LambdaExpression lambdaExpr, object lambda, bool isAction)
            {
                ClosureInfo = closureInfo;
                Lambda = lambda;
                LambdaExpr = lambdaExpr;
                IsAction = isAction;
            }
        }

        internal static class CurryClosureFuncs
        {
            private static readonly IEnumerable<MethodInfo> _methods =
                typeof(CurryClosureFuncs).GetTypeInfo().DeclaredMethods;

            public static readonly MethodInfo[] Methods = _methods.AsArray();

            public static Func<R> Curry<C, R>(Func<C, R> f, C c) => () => f(c);
            public static Func<T1, R> Curry<C, T1, R>(Func<C, T1, R> f, C c) => t1 => f(c, t1);
            public static Func<T1, T2, R> Curry<C, T1, T2, R>(Func<C, T1, T2, R> f, C c) => (t1, t2) => f(c, t1, t2);
            public static Func<T1, T2, T3, R> Curry<C, T1, T2, T3, R>(Func<C, T1, T2, T3, R> f, C c) => (t1, t2, t3) => f(c, t1, t2, t3);
            public static Func<T1, T2, T3, T4, R> Curry<C, T1, T2, T3, T4, R>(Func<C, T1, T2, T3, T4, R> f, C c) => (t1, t2, t3, t4) => f(c, t1, t2, t3, t4);
            public static Func<T1, T2, T3, T4, T5, R> Curry<C, T1, T2, T3, T4, T5, R>(Func<C, T1, T2, T3, T4, T5, R> f, C c) => (t1, t2, t3, t4, t5) => f(c, t1, t2, t3, t4, t5);
            public static Func<T1, T2, T3, T4, T5, T6, R> Curry<C, T1, T2, T3, T4, T5, T6, R>(Func<C, T1, T2, T3, T4, T5, T6, R> f, C c) => (t1, t2, t3, t4, t5, t6) => f(c, t1, t2, t3, t4, t5, t6);
        }

        internal static class CurryClosureActions
        {
            private static readonly IEnumerable<MethodInfo> _methods =
                typeof(CurryClosureActions).GetTypeInfo().DeclaredMethods;

            public static readonly MethodInfo[] Methods = _methods.AsArray();

            internal static Action Curry<C>(Action<C> a, C c) => () => a(c);
            internal static Action<T1> Curry<C, T1>(Action<C, T1> f, C c) => t1 => f(c, t1);
            internal static Action<T1, T2> Curry<C, T1, T2>(Action<C, T1, T2> f, C c) => (t1, t2) => f(c, t1, t2);
            internal static Action<T1, T2, T3> Curry<C, T1, T2, T3>(Action<C, T1, T2, T3> f, C c) => (t1, t2, t3) => f(c, t1, t2, t3);
            internal static Action<T1, T2, T3, T4> Curry<C, T1, T2, T3, T4>(Action<C, T1, T2, T3, T4> f, C c) => (t1, t2, t3, t4) => f(c, t1, t2, t3, t4);
            internal static Action<T1, T2, T3, T4, T5> Curry<C, T1, T2, T3, T4, T5>(Action<C, T1, T2, T3, T4, T5> f, C c) => (t1, t2, t3, t4, t5) => f(c, t1, t2, t3, t4, t5);
            internal static Action<T1, T2, T3, T4, T5, T6> Curry<C, T1, T2, T3, T4, T5, T6>(Action<C, T1, T2, T3, T4, T5, T6> f, C c) => (t1, t2, t3, t4, t5, t6) => f(c, t1, t2, t3, t4, t5, t6);
        }

        #endregion

        #region Collect Bound Constants

        private static bool IsClosureBoundConstant(object value, TypeInfo type) =>
            value is Delegate ||
            !type.IsPrimitive && !type.IsEnum && !(value is string) && !(value is Type);

        // @paramExprs is required for nested lambda compilation
        private static bool TryCollectBoundConstants(ref ClosureInfo closure, Expression expr, IReadOnlyList<ParameterExpression> paramExprs)
        {
            if (expr == null)
                return false;

            switch (expr.NodeType)
            {
                case ExpressionType.Constant:
                    var constantExpr = (ConstantExpression)expr;
                    var value = constantExpr.Value;
                    if (value != null && IsClosureBoundConstant(value, value.GetType().GetTypeInfo()))
                        closure.AddConstant(constantExpr);
                    return true;

                case ExpressionType.Parameter:
                    // if parameter is used BUT is not in passed parameters and not in local variables,
                    // it means parameter is provided by outer lambda and should be put in closure for current lambda
                    if (paramExprs.GetFirstIndex(expr) == -1 && !closure.IsLocalVar(expr))
                        closure.AddNonPassedParam((ParameterExpression)expr);
                    return true;

                case ExpressionType.Call:
                    var methodCallExpr = (MethodCallExpression)expr;
                    var objExpr = methodCallExpr.Object;
                    return (objExpr == null
                       || TryCollectBoundConstants(ref closure, objExpr, paramExprs))
                       && TryCollectBoundConstants(ref closure, methodCallExpr.Arguments, paramExprs);

                case ExpressionType.MemberAccess:
                    var memberExpr = ((MemberExpression)expr).Expression;
                    return memberExpr == null || TryCollectBoundConstants(ref closure, memberExpr, paramExprs);

                case ExpressionType.New:
                    return TryCollectBoundConstants(ref closure, ((NewExpression)expr).Arguments, paramExprs);

                case ExpressionType.NewArrayBounds:
                case ExpressionType.NewArrayInit:
                    return TryCollectBoundConstants(ref closure, ((NewArrayExpression)expr).Expressions, paramExprs);

                case ExpressionType.MemberInit:
                    return TryCollectMemberInitExprConstants(ref closure, (MemberInitExpression)expr, paramExprs);

                case ExpressionType.Lambda:
                    return TryCompileNestedLambda(ref closure, (LambdaExpression)expr, paramExprs);

                case ExpressionType.Invoke:
                    var invokeExpr = (InvocationExpression)expr;
                    var lambda = invokeExpr.Expression;
                    return TryCollectBoundConstants(ref closure, lambda, paramExprs)
                        && TryCollectBoundConstants(ref closure, invokeExpr.Arguments, paramExprs);

                case ExpressionType.Conditional:
                    var condExpr = (ConditionalExpression)expr;
                    return TryCollectBoundConstants(ref closure, condExpr.Test, paramExprs)
                        && TryCollectBoundConstants(ref closure, condExpr.IfTrue, paramExprs)
                        && TryCollectBoundConstants(ref closure, condExpr.IfFalse, paramExprs);

                case ExpressionType.Block:
                    var blockExpr = (BlockExpression)expr;
                    closure.PushBlock(blockExpr.Result, blockExpr.Variables, Tools.Empty<LocalBuilder>());
                    if (!TryCollectBoundConstants(ref closure, blockExpr.Expressions, paramExprs))
                        return false;
                    closure.PopBlock();
                    return true;

                case ExpressionType.Index:
                    var indexExpr = (IndexExpression)expr;
                    return indexExpr.Object == null
                        || TryCollectBoundConstants(ref closure, indexExpr.Object, paramExprs)
                        && TryCollectBoundConstants(ref closure, indexExpr.Arguments, paramExprs);

                case ExpressionType.Try:
                    return TryCollectTryExprConstants(ref closure, (TryExpression)expr, paramExprs);

                case ExpressionType.Label:
                    closure.LabelCount += 1;
                    var defaultValueExpr = ((LabelExpression)expr).DefaultValue;
                    return defaultValueExpr == null || TryCollectBoundConstants(ref closure, defaultValueExpr, paramExprs);

                case ExpressionType.Goto:
                    var gotoValueExpr = ((GotoExpression)expr).Value;
                    return gotoValueExpr == null || TryCollectBoundConstants(ref closure, gotoValueExpr, paramExprs);

                case ExpressionType.Default:
                    return true;

                default:
                    var unaryExpr = expr as UnaryExpression;
                    if (unaryExpr != null)
                        return TryCollectBoundConstants(ref closure, unaryExpr.Operand, paramExprs);
                    var binaryExpr = expr as BinaryExpression;
                    if (binaryExpr != null)
                        return TryCollectBoundConstants(ref closure, binaryExpr.Left, paramExprs)
                               && TryCollectBoundConstants(ref closure, binaryExpr.Right, paramExprs);
                    return false;
            }
        }

        private static bool TryCompileNestedLambda(ref ClosureInfo closure, 
            LambdaExpression lambdaExpr, IReadOnlyList<ParameterExpression> paramExprs)
        {
            // 1. Try to compile nested lambda in place
            // 2. Check that parameters used in compiled lambda are passed or closed by outer lambda
            // 3. Add the compiled lambda to closure of outer lambda for later invocation

            var nestedClosure = new ClosureInfo(false);
            var lambdaParamExprs = lambdaExpr.Parameters;
            var bodyExpr = lambdaExpr.Body;
            var bodyType = bodyExpr.Type;
            var compiledLambda = TryCompile(ref nestedClosure,
                lambdaExpr.Type, Tools.GetParamTypes(lambdaParamExprs), lambdaExpr.ReturnType, bodyExpr, bodyType,
                lambdaParamExprs, isNestedLambda: true);

            if (compiledLambda == null)
                return false;

            // add the nested lambda into closure
            closure.AddNestedLambda(lambdaExpr, compiledLambda, ref nestedClosure, isAction: bodyType == typeof(void));

            if (!nestedClosure.HasClosure)
                return true; // no closure, we are done

            // if nested non passed parameter is no matched with any outer passed parameter, 
            // then ensure it goes to outer non passed parameter.
            // But check that having a non-passed parameter in root expression is invalid.
            var nestedNonPassedParams = nestedClosure.NonPassedParameters;
            if (nestedNonPassedParams.Length != 0)
                for (var i = 0; i < nestedNonPassedParams.Length; i++)
                {
                    var nestedNonPassedParam = nestedNonPassedParams[i];
                    if (paramExprs.GetFirstIndex(nestedNonPassedParam) == -1)
                        closure.AddNonPassedParam(nestedNonPassedParam);
                }

            // Promote found constants and nested lambdas into outer closure
            var nestedConstants = nestedClosure.Constants;
            if (nestedConstants.Length != 0)
                for (var i = 0; i < nestedConstants.Length; i++)
                    closure.AddConstant(nestedConstants[i]);

            var nestedNestedLambdas = nestedClosure.NestedLambdas;
            if (nestedNestedLambdas.Length != 0)
                for (var i = 0; i < nestedNestedLambdas.Length; i++)
                    closure.AddNestedLambda(nestedNestedLambdas[i]);

            return true;
        }

        private static bool TryCollectMemberInitExprConstants(ref ClosureInfo closure, MemberInitExpression expr, IReadOnlyList<ParameterExpression> paramExprs)
        {
            var newExpr = expr.NewExpression
#if LIGHT_EXPRESSION
                          ?? expr.Expression;
#endif
                ;
            if (!TryCollectBoundConstants(ref closure, newExpr, paramExprs))
                return false;

            var memberBindings = expr.Bindings;
            for (var i = 0; i < memberBindings.Count; ++i)
            {
                var memberBinding = memberBindings[i];
                if (memberBinding.BindingType == MemberBindingType.Assignment &&
                    !TryCollectBoundConstants(ref closure, ((MemberAssignment)memberBinding).Expression, paramExprs))
                    return false;
            }

            return true;
        }

        private static bool TryCollectTryExprConstants(ref ClosureInfo closure, TryExpression tryExpr, IReadOnlyList<ParameterExpression> paramExprs)
        {
            if (!TryCollectBoundConstants(ref closure, tryExpr.Body, paramExprs))
                return false;

            var catchBlocks = tryExpr.Handlers;
            for (var i = 0; i < catchBlocks.Count; i++)
            {
                var catchBlock = catchBlocks[i];
                var catchBody = catchBlock.Body;
                var catchExVar = catchBlock.Variable;
                if (catchExVar != null)
                {
                    closure.PushBlock(catchBody, new[] { catchExVar }, Tools.Empty<LocalBuilder>());
                    if (!TryCollectBoundConstants(ref closure, catchExVar, paramExprs))
                        return false;
                }

                var filterExpr = catchBlock.Filter;
                if (filterExpr != null &&
                    !TryCollectBoundConstants(ref closure, filterExpr, paramExprs) || 
                    !TryCollectBoundConstants(ref closure, catchBody, paramExprs))
                    return false;

                if (catchExVar != null)
                    closure.PopBlock();
            }

            var finallyExpr = tryExpr.Finally;
            return finallyExpr == null || TryCollectBoundConstants(ref closure, finallyExpr, paramExprs);
        }

        private static bool TryCollectBoundConstants(ref ClosureInfo closure, IReadOnlyList<Expression> exprs, IReadOnlyList<ParameterExpression> paramExprs)
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
#if !NETSTANDARD2_0 && !NET45
            private static readonly MethodInfo _getTypeFromHandleMethod = typeof(Type).GetTypeInfo()
                .DeclaredMethods.First(m => m.IsStatic && m.Name == "GetTypeFromHandle");

            private static readonly MethodInfo _objectEqualsMethod = typeof(object).GetTypeInfo()
                .DeclaredMethods.First(m => m.IsStatic && m.Name == "Equals");
#else
            private static readonly MethodInfo _getTypeFromHandleMethod =
                ((Func<RuntimeTypeHandle, Type>)Type.GetTypeFromHandle).Method;

            private static readonly MethodInfo _objectEqualsMethod = ((Func<object, object, bool>)object.Equals).Method;
#endif

            public static bool TryEmit(Expression expr, Type exprType,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType parent, 
                int byRefIndex = -1)
            {
                switch (expr.NodeType)
                {
                    case ExpressionType.Parameter:
                        return TryEmitParameter((ParameterExpression)expr, exprType, paramExprs, il, ref closure, parent, byRefIndex);
                    case ExpressionType.Convert:
                        return TryEmitConvert((UnaryExpression)expr, exprType, paramExprs, il, ref closure);
                    case ExpressionType.ArrayIndex:
                        return TryEmitArrayIndex((BinaryExpression)expr, exprType, paramExprs, il, ref closure);
                    case ExpressionType.Constant:
                        return TryEmitConstant((ConstantExpression)expr, exprType, il, ref closure);
                    case ExpressionType.Call:
                        return TryEmitMethodCall((MethodCallExpression)expr, paramExprs, il, ref closure, parent);
                    case ExpressionType.MemberAccess:
                        return TryEmitMemberAccess((MemberExpression)expr, paramExprs, il, ref closure);
                    case ExpressionType.New:
                        return TryEmitNew((NewExpression)expr, exprType, paramExprs, il, ref closure);
                    case ExpressionType.NewArrayBounds:
                    case ExpressionType.NewArrayInit:
                        return EmitNewArray((NewArrayExpression)expr, exprType, paramExprs, il, ref closure);
                    case ExpressionType.MemberInit:
                        return EmitMemberInit((MemberInitExpression)expr, exprType, paramExprs, il, ref closure, parent);
                    case ExpressionType.Lambda:
                        return TryEmitNestedLambda((LambdaExpression)expr, paramExprs, il, ref closure);

                    case ExpressionType.Invoke:
                        return TryInvokeLambda((InvocationExpression)expr, paramExprs, il, ref closure);

                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                        return TryEmitComparison((BinaryExpression)expr, paramExprs, il, ref closure);

                    case ExpressionType arithmeticExprType when Tools.IsArithmetic(arithmeticExprType):
                        return TryEmitArithmeticOperation((BinaryExpression)expr, arithmeticExprType, exprType, paramExprs, il, ref closure);

                    case ExpressionType.AndAlso:
                    case ExpressionType.OrElse:
                        return TryEmitLogicalOperator((BinaryExpression)expr, paramExprs, il, ref closure);

                    case ExpressionType.Coalesce:
                        return TryEmitCoalesceOperator((BinaryExpression)expr, paramExprs, il, ref closure);

                    case ExpressionType.Conditional:
                        return TryEmitConditional((ConditionalExpression)expr, paramExprs, il, ref closure);

                    case ExpressionType.PostIncrementAssign:
                    case ExpressionType.PreIncrementAssign:
                    case ExpressionType.PostDecrementAssign:
                    case ExpressionType.PreDecrementAssign:
                        return TryEmitIncDecAssign((UnaryExpression)expr, il, ref closure, parent);

                    case ExpressionType arithmeticAssign 
                        when Tools.GetArithmeticFromArithmeticAssignOrSelf(arithmeticAssign) != arithmeticAssign:
                    case ExpressionType.Assign:
                        return TryEmitAssign((BinaryExpression)expr, exprType, paramExprs, il, ref closure);

                    case ExpressionType.Block:
                        return TryEmitBlock((BlockExpression)expr, paramExprs, il, ref closure);

                    case ExpressionType.Try:
                        return TryEmitTryCatchFinallyBlock((TryExpression)expr, exprType, paramExprs, il, ref closure);

                    case ExpressionType.Throw:
                        return TryEmitThrow((UnaryExpression)expr, paramExprs, il, ref closure);

                    case ExpressionType.Default:
                        return exprType == typeof(void) || EmitDefault(exprType, il);

                    case ExpressionType.Index:
                        return TryEmitIndex((IndexExpression)expr, exprType, paramExprs, il, ref closure);

                    case ExpressionType.Goto:
                        return TryEmitGoto((GotoExpression)expr, il, ref closure);

                    case ExpressionType.Label:
                        return TryEmitLabel((LabelExpression)expr, paramExprs, il, ref closure);

                    default:
                        return false;
                }
            }

            private static bool TryEmitLabel(LabelExpression expr,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var lbl = closure.Labels.FirstOrDefault(x => x.Key == expr.Target);
                if (lbl.Key != expr.Target)
                {
                    lbl = new KeyValuePair<object, Label>(expr.Target, il.DefineLabel());
                    closure.Labels[closure.LabelCount++] = lbl;
                }

                il.MarkLabel(lbl.Value);

                var defaultVal = expr.DefaultValue;
                return defaultVal == null
                    || TryEmit(defaultVal, defaultVal.Type, paramExprs, il, ref closure, ExpressionType.Label);
            }

            // todo : GotoExpression.Value 
            private static bool TryEmitGoto(GotoExpression exprObj, ILGenerator il, ref ClosureInfo closure)
            {
                var labels = closure.Labels;
                if (labels == null)
                    throw new InvalidOperationException("Cannot jump, no labels found");

                var lbl = labels.FirstOrDefault(x => x.Key == exprObj.Target);
                if (lbl.Key != exprObj.Target)
                {
                    if(labels.Length == closure.LabelCount - 1)
                        throw new InvalidOperationException("Cannot jump, not all labels found");

                    lbl = new KeyValuePair<object, Label>(exprObj.Target, il.DefineLabel());
                    labels[closure.LabelCount++] = lbl;
                }

                if (exprObj.Kind == GotoExpressionKind.Goto)
                {
                    il.Emit(OpCodes.Br, lbl.Value);
                    return true;
                }

                return false;
            }

            private static bool TryEmitIndex(IndexExpression exprObj, Type elemType,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var obj = exprObj.Object;
                if (obj != null && !TryEmit(obj, obj.Type, paramExprs, il, ref closure, ExpressionType.Index))
                    return false;
                    
                var argLength = exprObj.Arguments.Count;
                for (var i = 0; i < argLength; i++)
                {
                    var arg = exprObj.Arguments[i];
                    if (!TryEmit(arg, arg.Type, paramExprs, il, ref closure, ExpressionType.Index))
                        return false;
                }

                var instType = obj?.Type;
                if (exprObj.Indexer != null)
                {
                    var propGetMethod = TryGetPropertyGetMethod(exprObj.Indexer);
                    return propGetMethod != null && EmitMethodCall(il, propGetMethod);
                }

                if (exprObj.Arguments.Count == 1) // one dimensional array
                {
                    if (elemType.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Ldelem, elemType);
                    else
                        il.Emit(OpCodes.Ldelem_Ref);
                    return true;
                }
                
                // multi dimensional array
                var getMethod = instType?.GetTypeInfo().GetDeclaredMethod("Get");
                return getMethod != null && EmitMethodCall(il, getMethod);
            }

            private static bool TryEmitCoalesceOperator(BinaryExpression exprObj, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var labelFalse = il.DefineLabel();
                var labelDone = il.DefineLabel();

                var left = exprObj.Left;
                var right = exprObj.Right;

                if (!TryEmit(left, left.Type, paramExprs, il, ref closure, ExpressionType.Coalesce))
                    return false;

                il.Emit(OpCodes.Dup); // duplicate left, if it's not null, after the branch this value will be on the top of the stack
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, labelFalse);

                il.Emit(OpCodes.Pop); // left is null, pop its value from the stack

                if (!TryEmit(right, right.Type, paramExprs, il, ref closure, ExpressionType.Coalesce))
                    return false;

                if (right.Type != exprObj.Type)
                    if (right.Type.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Box, right.Type);
                    else
                        il.Emit(OpCodes.Castclass, exprObj.Type);

                il.Emit(OpCodes.Br, labelDone);

                il.MarkLabel(labelFalse);
                if (left.Type != exprObj.Type)
                    il.Emit(OpCodes.Castclass, exprObj.Type);

                il.MarkLabel(labelDone);
                return true;
            }

            private static bool EmitDefault(Type type, ILGenerator il)
            {
                if (type == typeof(string))
                {
                    il.Emit(OpCodes.Ldnull);
                }
                else if (
                    type == typeof(bool) ||
                    type == typeof(byte) ||
                    type == typeof(char) ||
                    type == typeof(sbyte) ||
                    type == typeof(int) ||
                    type == typeof(uint) ||
                    type == typeof(short) ||
                    type == typeof(ushort))
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                }
                else if (
                    type == typeof(long) ||
                    type == typeof(ulong))
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Conv_I8);
                }
                else if (type == typeof(float))
                    il.Emit(OpCodes.Ldc_R4, default(float));
                else if (type == typeof(double))
                    il.Emit(OpCodes.Ldc_R8, default(double));
                else if (type.GetTypeInfo().IsValueType)
                    il.Emit(OpCodes.Ldloc, InitValueTypeVariable(il, type));
                else
                    il.Emit(OpCodes.Ldnull);

                return true;
            }

            private static bool TryEmitBlock(BlockExpression blockExpr, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                closure.PushBlockAndConstructLocalVars(blockExpr.Result, blockExpr.Variables, il);
                var ok = EmitMany(blockExpr.Expressions, paramExprs, il, ref closure, ExpressionType.Block);
                closure.PopBlock();
                return ok;
            }

            private static bool TryEmitTryCatchFinallyBlock(TryExpression tryExpr, Type exprType, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var returnLabel = default(Label);
                var returnResult = default(LocalBuilder);
                var isNonVoid = exprType != typeof(void);
                if (isNonVoid)
                {
                    returnLabel = il.DefineLabel();
                    returnResult = il.DeclareLocal(exprType);
                }

                il.BeginExceptionBlock();
                var tryBodyExpr = tryExpr.Body;
                if (!TryEmit(tryBodyExpr, tryBodyExpr.Type, paramExprs, il, ref closure, ExpressionType.Try))
                    return false;

                if (isNonVoid)
                {
                    il.Emit(OpCodes.Stloc_S, returnResult);
                    il.Emit(OpCodes.Leave_S, returnLabel);
                }

                var catchBlocks = tryExpr.Handlers;
                for (var i = 0; i < catchBlocks.Count; i++)
                {
                    var catchBlock = catchBlocks[i];
                    if (catchBlock.Filter != null)
                        return false; // todo: Add support for filters on catch expression

                    il.BeginCatchBlock(catchBlock.Test);

                    // at the beginning of catch the Exception value is on the stack,
                    // we will store into local variable.
                    var catchBodyExpr = catchBlock.Body;
                    var exVarExpr = catchBlock.Variable;
                    if (exVarExpr != null)
                    {
                        var exVar = il.DeclareLocal(exVarExpr.Type);
                        closure.PushBlock(catchBodyExpr, new[] { exVarExpr }, new[] { exVar });
                        il.Emit(OpCodes.Stloc_S, exVar);
                    }

                    if (!TryEmit(catchBodyExpr, catchBodyExpr.Type, paramExprs, il, ref closure, ExpressionType.Try))
                        return false;

                    if (exVarExpr != null)
                        closure.PopBlock();

                    if (isNonVoid)
                    {
                        il.Emit(OpCodes.Stloc_S, returnResult);
                        il.Emit(OpCodes.Leave_S, returnLabel);
                    }
                    else
                    {
                        if (catchBodyExpr.Type != typeof(void))
                            il.Emit(OpCodes.Pop);
                    }
                }

                var finallyExpr = tryExpr.Finally;
                if (finallyExpr != null)
                {
                    il.BeginFinallyBlock();
                    if (!TryEmit(finallyExpr, finallyExpr.Type, paramExprs, il, ref closure, ExpressionType.Try))
                        return false;
                }

                il.EndExceptionBlock();
                if (isNonVoid)
                {
                    il.MarkLabel(returnLabel);
                    il.Emit(OpCodes.Ldloc, returnResult);
                }

                return true;
            }

            private static bool TryEmitThrow(UnaryExpression exprObj,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var ex = exprObj.Operand;
                var ok = TryEmit(ex, ex.Type, paramExprs, il, ref closure, ExpressionType.Throw);
                il.ThrowException(ex.Type);
                return ok;
            }

            private static bool TryEmitParameter(ParameterExpression paramExpr, Type paramType, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType parent, int byRefIndex = -1)
            {
                // if parameter is passed through, then just load it on stack
                var paramIndex = paramExprs.GetFirstIndex(paramExpr);
                if (paramIndex != -1)
                {
                    if (closure.HasClosure)
                        paramIndex += 1; // shift parameter indices by one, because the first one will be closure

                    var asAddress = 
                        parent == ExpressionType.Call && paramType.GetTypeInfo().IsValueType && !paramExpr.IsByRef;

                    EmitLoadParamArg(il, paramIndex, asAddress);

                    if (paramExpr.IsByRef)
                    {
                        if (parent == ExpressionType.Coalesce)
                            il.Emit(OpCodes.Ldind_Ref); // Coalesce on for ref types
                        else if (Tools.IsArithmetic(parent))
                            EmitDereference(il, paramType);
                    }

                    return true;
                }

                // if parameter isn't passed, then it is passed into some outer lambda or it is a local variable,
                // so it should be loaded from closure or from the locals. Then the closure is null will be an invalid state.
                if (!closure.IsClosureConstructed)
                    return false;

                // parameter may represent a variable, so first look if this is the case
                var variable = closure.GetDefinedLocalVarOrDefault(paramExpr);
                if (variable != null)
                {
                    il.Emit(OpCodes.Ldloc, variable);
                    return true;
                }

                if (paramExpr.IsByRef)
                {
                    il.Emit(OpCodes.Ldloca_S, byRefIndex);
                    return true;
                }

                // the only possibility that we are here is because we are in nested lambda,
                // and it uses some parameter or variable from the outer lambda
                var nonPassedParamIndex = closure.NonPassedParameters.GetFirstIndex(paramExpr);
                if (nonPassedParamIndex == -1)
                    return false;  // what??? no chance

                var closureItemIndex = closure.Constants.Length + nonPassedParamIndex;
                return LoadClosureFieldOrItem(ref closure, il, closureItemIndex, paramType);
            }

            private static void EmitDereference(ILGenerator il, Type type)
            {
                if (type == typeof(Int32))
                    il.Emit(OpCodes.Ldind_I4);
                else if (type == typeof(Int64))
                    il.Emit(OpCodes.Ldind_I8);
                else if (type == typeof(Int16))
                    il.Emit(OpCodes.Ldind_I2);
                else if (type == typeof(SByte))
                    il.Emit(OpCodes.Ldind_I1);
                else if (type == typeof(Single))
                    il.Emit(OpCodes.Ldind_R4);
                else if (type == typeof(Double))
                    il.Emit(OpCodes.Ldind_R8);
                else if (type == typeof(IntPtr))
                    il.Emit(OpCodes.Ldind_I);
                else if (type == typeof(UIntPtr))
                    il.Emit(OpCodes.Ldind_I);
                else if (type == typeof(Byte))
                    il.Emit(OpCodes.Ldind_U1);
                else if (type == typeof(UInt16))
                    il.Emit(OpCodes.Ldind_U2);
                else if (type == typeof(UInt32))
                    il.Emit(OpCodes.Ldind_U4);
                else
                    il.Emit(OpCodes.Ldobj, type);
                //todo: UInt64 as there is no OpCodes? Ldind_Ref?
            }

            // loads argument at paramIndex onto evaluation stack
            private static void EmitLoadParamArg(ILGenerator il, int paramIndex, bool asAddress)
            {
                if (asAddress)
                {
                    if (paramIndex <= byte.MaxValue)
                        il.Emit(OpCodes.Ldarga_S, (byte)paramIndex);
                    else
                        il.Emit(OpCodes.Ldarga, paramIndex);
                    return;
                }

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

            private static bool EmitBinary(BinaryExpression expr, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType parent) => 
                TryEmit(expr.Left, expr.Left.Type, paramExprs, il, ref closure, parent) &&
                TryEmit(expr.Right, expr.Right.Type, paramExprs, il, ref closure, parent);

            private static bool EmitMany(IReadOnlyList<Expression> exprs, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType parent)
            {
                for (int i = 0, n = exprs.Count; i < n; i++)
                {
                    var expr = exprs[i];
                    if (parent != ExpressionType.Block || 
                        (expr.NodeType != ExpressionType.Constant && expr.NodeType != ExpressionType.Parameter) || closure.CurrentBlock.ResultExpr == expr) // In a Block, Constants or Paramters are only compiled to IL if they are the last Expression in it. 
                        if (!TryEmit(expr, expr.Type, paramExprs, il, ref closure, parent, i))
                            return false;
                }
                return true;
            }

            private static bool TryEmitConvert(UnaryExpression expr, Type targetType,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var opExpr = expr.Operand;
                var method = expr.Method;
                if (method != null && method.Name != "op_Implicit" && method.Name != "op_Explicit")
                    return TryEmit(opExpr, opExpr.Type, paramExprs, il, ref closure, ExpressionType.Call, 0) 
                        && EmitMethodCall(il, method);

                if (!TryEmit(opExpr, opExpr.Type, paramExprs, il, ref closure, ExpressionType.Convert))
                    return false;

                var sourceType = opExpr.Type;
                if (targetType == sourceType)
                    return true; // do nothing, no conversion needed

                if (targetType == typeof(object))
                {
                    var nullableType = Nullable.GetUnderlyingType(sourceType);
                    if (nullableType != null)
                    {
                        il.Emit(OpCodes.Newobj, sourceType.GetTypeInfo().DeclaredConstructors.First());
                        il.Emit(OpCodes.Box, sourceType);
                        return true;
                    }
                    // for value type to object, just box a value, otherwise do nothing - everything is object anyway
                    if (sourceType.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Box, sourceType);
                    return true;
                }

                // check implicit / explicit conversion operators on source and target types - #73
                var sourceTypeInfo = sourceType.GetTypeInfo();
                if (!sourceTypeInfo.IsPrimitive)
                {
                    var convertOpMethod = FirstConvertOperatorOrDefault(sourceTypeInfo, targetType, sourceType);
                    if (convertOpMethod != null)
                        return EmitMethodCall(il, convertOpMethod);
                }

                var targetTypeInfo = targetType.GetTypeInfo();
                if (!targetTypeInfo.IsPrimitive)
                {
                    var convertOpMethod = FirstConvertOperatorOrDefault(targetTypeInfo, targetType, sourceType);
                    if (convertOpMethod != null)
                        return EmitMethodCall(il, convertOpMethod);
                }

                if (sourceType == typeof(object) && targetTypeInfo.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, targetType);
                
                // Conversion to Nullable: new Nullable<T>(T val);
                else if (targetTypeInfo.IsGenericType && targetTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>))
                    il.Emit(OpCodes.Newobj, targetType.GetConstructorByArgs(targetTypeInfo.GenericTypeArguments[0]));
                else
                {
                    if (targetType.GetTypeInfo().IsEnum)
                        targetType = Enum.GetUnderlyingType(targetType);

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

                    else // cast as the last resort and let's it fail if unlucky
                        il.Emit(OpCodes.Castclass, targetType);
                }

                return true;
            }

            private static MethodInfo FirstConvertOperatorOrDefault(TypeInfo typeInfo, Type targetType, Type sourceType) =>
                typeInfo.DeclaredMethods.GetFirst(m =>
                    m.IsStatic && m.ReturnType == targetType && 
                    (m.Name == "op_Implicit" || m.Name == "op_Explicit") &&
                    m.GetParameters()[0].ParameterType == sourceType);

            private static bool TryEmitConstant(ConstantExpression expr, Type exprType, ILGenerator il, 
                ref ClosureInfo closure)
            {
                var constantValue = expr.Value;
                if (constantValue == null)
                {
                    if (exprType.GetTypeInfo().IsValueType) // handles the conversion of null to Nullable<T>
                        il.Emit(OpCodes.Ldloc, InitValueTypeVariable(il, exprType));
                    else
                        il.Emit(OpCodes.Ldnull);
                    return true;
                }

                var constantType = constantValue.GetType();
                if (IsClosureBoundConstant(constantValue, constantType.GetTypeInfo()))
                {
                    var constIndex = closure.Constants.GetFirstIndex(expr);
                    if (constIndex == -1 || !LoadClosureFieldOrItem(ref closure, il, constIndex, exprType))
                        return false;
                }
                else
                {
                    // get raw enum type to light
                    if (constantType.GetTypeInfo().IsEnum)
                        constantType = Enum.GetUnderlyingType(constantType);

                    if (constantType == typeof(int))
                    {
                        EmitLoadConstantInt(il, (int)constantValue);
                    }
                    else if (constantType == typeof(char))
                    {
                        EmitLoadConstantInt(il, (char)constantValue);
                    }
                    else if (constantType == typeof(short))
                    {
                        EmitLoadConstantInt(il, (short)constantValue);
                    }
                    else if (constantType == typeof(byte))
                    {
                        EmitLoadConstantInt(il, (byte)constantValue);
                    }
                    else if (constantType == typeof(ushort))
                    {
                        EmitLoadConstantInt(il, (ushort)constantValue);
                    }
                    else if (constantType == typeof(sbyte))
                    {
                        EmitLoadConstantInt(il, (sbyte)constantValue);
                    }
                    else if (constantType == typeof(uint))
                    {
                        unchecked
                        {
                            EmitLoadConstantInt(il, (int)(uint)constantValue);
                        }
                    }
                    else if (constantType == typeof(long))
                    {
                        il.Emit(OpCodes.Ldc_I8, (long)constantValue);
                    }
                    else if (constantType == typeof(ulong))
                    {
                        unchecked
                        {
                            il.Emit(OpCodes.Ldc_I8, (long)(ulong)constantValue);
                        }
                    }
                    else if (constantType == typeof(float))
                    {
                        il.Emit(OpCodes.Ldc_R4, (float)constantValue);
                    }
                    else if (constantType == typeof(double))
                    {
                        il.Emit(OpCodes.Ldc_R8, (double)constantValue);
                    }
                    else if (constantType == typeof(bool))
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
                    else if (constantType == typeof(IntPtr))
                    {
                        il.Emit(OpCodes.Ldc_I8, ((IntPtr)constantValue).ToInt64());
                    }
                    else if (constantType == typeof(UIntPtr))
                    {
                        unchecked
                        {
                            il.Emit(OpCodes.Ldc_I8, (long)((UIntPtr)constantValue).ToUInt64());
                        }
                    }
                    else return false;
                }

                // todo: consider how to remove boxing where it is not required
                // boxing the value type, otherwise we can get a strange result when 0 is treated as Null.
                if (exprType == typeof(object) && constantType.GetTypeInfo().IsValueType)
                    il.Emit(OpCodes.Box, constantValue.GetType()); // using normal type for Enum instead of underlying type

                return true;
            }

            private static LocalBuilder InitValueTypeVariable(ILGenerator il, Type exprType, LocalBuilder existingVar = null)
            {
                var valVar = existingVar ?? il.DeclareLocal(exprType);
                il.Emit(OpCodes.Ldloca, valVar);
                il.Emit(OpCodes.Initobj, exprType);
                return valVar;
            }

            private static bool LoadClosureFieldOrItem(ref ClosureInfo closure, ILGenerator il, int itemIndex, 
                Type itemType, Expression itemExprObj = null)
            {
                il.Emit(OpCodes.Ldarg_0); // closure is always a first argument

                var closureFields = closure.ClosureFields;
                if (closureFields != null)
                    il.Emit(OpCodes.Ldfld, closureFields[itemIndex]);
                else
                {
                    // for ArrayClosure load an array field
                    il.Emit(OpCodes.Ldfld, ArrayClosure.ArrayField);

                    // load array item index
                    EmitLoadConstantInt(il, itemIndex);

                    // load item from index
                    il.Emit(OpCodes.Ldelem_Ref);
                    itemType = itemType ?? itemExprObj?.Type;
                    if (itemType == null)
                        return false;

                    il.Emit(itemType.GetTypeInfo().IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, itemType);
                }

                return true;
            }

            // todo: Replace resultValueVar with a closureInfo block
            private static bool TryEmitNew(NewExpression expr, Type exprType,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure,
                LocalBuilder resultValueVar = null)
            {
                if (!EmitMany(expr.Arguments, paramExprs, il, ref closure, ExpressionType.New))
                    return false;

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (expr.Constructor != null)
                    il.Emit(OpCodes.Newobj, expr.Constructor);
                else
                    // ReSharper disable once HeuristicUnreachableCode
                {
                    if (!exprType.GetTypeInfo().IsValueType)
                        return false; // null constructor and not a value type, better fallback

                    var valueVar = InitValueTypeVariable(il, exprType, resultValueVar);
                    if (resultValueVar == null)
                        il.Emit(OpCodes.Ldloc, valueVar);
                }

                return true;
            }

            private static bool EmitNewArray(NewArrayExpression expr, Type arrayType, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var elems = expr.Expressions;
                var elemType = arrayType.GetElementType();
                if (elemType == null)
                    return false;

                var arrVar = il.DeclareLocal(arrayType);

                var rank = arrayType.GetArrayRank();
                if (rank == 1) // one dimensional
                {
                    EmitLoadConstantInt(il, elems.Count);
                }
                else // multi dimensional
                {
                    var boundsLength = elems.Count;
                    for (var i = 0; i < boundsLength; i++)
                    {
                        var bound = elems[i];
                        if (!TryEmit(bound, bound.Type, paramExprs, il, ref closure, ExpressionType.NewArrayInit))
                            return false;
                    }

                    var ctor = arrayType.GetTypeInfo().DeclaredConstructors.GetFirst();
                    if (ctor == null) 
                        return false;
                    il.Emit(OpCodes.Newobj, ctor);

                    return true;
                }

                il.Emit(OpCodes.Newarr, elemType);
                il.Emit(OpCodes.Stloc, arrVar);

                var isElemOfValueType = elemType.GetTypeInfo().IsValueType;

                for (int i = 0, n = elems.Count; i < n; i++)
                {
                    il.Emit(OpCodes.Ldloc, arrVar);
                    EmitLoadConstantInt(il, i);

                    // loading element address for later copying of value into it.
                    if (isElemOfValueType)
                        il.Emit(OpCodes.Ldelema, elemType);

                    var elemExpr = elems[i];
                    if (!TryEmit(elemExpr, elemExpr.Type, paramExprs, il, ref closure, ExpressionType.NewArrayInit))
                        return false;

                    if (isElemOfValueType)
                        il.Emit(OpCodes.Stobj, elemType); // store element of value type by array element address
                    else
                        il.Emit(OpCodes.Stelem_Ref);
                }

                il.Emit(OpCodes.Ldloc, arrVar);
                return true;
            }

            private static bool TryEmitArrayIndex(BinaryExpression expr, Type exprType, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                if (!EmitBinary(expr, paramExprs, il, ref closure, ExpressionType.ArrayIndex))
                    return false;

                if (exprType.GetTypeInfo().IsValueType)
                    il.Emit(OpCodes.Ldelem, exprType);
                else
                    il.Emit(OpCodes.Ldelem_Ref);
                return true;
            }

            private static bool EmitMemberInit(MemberInitExpression expr, Type exprType, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType stack)
            {
                // todo: Use closureInfo Block to track the variable instead
                LocalBuilder valueVar = null;
                if (exprType.GetTypeInfo().IsValueType)
                    valueVar = il.DeclareLocal(exprType);

                var newExpr = expr.NewExpression;
#if LIGHT_EXPRESSION
                if (newExpr == null)
                {
                    if (!TryEmit(expr.Expression, exprType, paramExprs, il, ref closure, ExpressionType.MemberInit/*, valueVar*/)) // todo: fix me
                        return false;
                }
                else
#endif
                if (!TryEmitNew(newExpr, exprType, paramExprs, il, ref closure, valueVar))
                    return false;

                var bindings = expr.Bindings;
                for (var i = 0; i < bindings.Count; i++)
                {
                    var binding = bindings[i];
                    if (binding.BindingType != MemberBindingType.Assignment)
                        return false;

                    if (valueVar != null) // load local value address, to set its members
                        il.Emit(OpCodes.Ldloca, valueVar);
                    else
                        il.Emit(OpCodes.Dup); // duplicate member owner on stack

                    var bindingExpr = ((MemberAssignment)binding).Expression;
                    if (!TryEmit(bindingExpr, bindingExpr.Type, paramExprs, il, ref closure, stack) ||
                        !EmitMemberAssign(il, binding.Member))
                        return false;
                }

                if (valueVar != null)
                    il.Emit(OpCodes.Ldloc, valueVar);

                return true;
            }

            private static bool EmitMemberAssign(ILGenerator il, MemberInfo member)
            {
                var prop = member as PropertyInfo;
                if (prop != null)
                {
                    var setMethod = prop.DeclaringType.GetTypeInfo().GetDeclaredMethod("set_" + prop.Name);
                    return setMethod != null && EmitMethodCall(il, setMethod);
                }

                var field = member as FieldInfo;
                if (field == null)
                    return false;
                il.Emit(OpCodes.Stfld, field);
                return true;
            }

            private static bool TryEmitIncDecAssign(UnaryExpression expr, ILGenerator il, ref ClosureInfo closure, ExpressionType parent)
            {
                var left = expr.Operand;
                var nodeType = expr.NodeType;

                var leftParamExpr = (ParameterExpression)left;

                var varIdx = closure.CurrentBlock.VarExprs.GetFirstIndex(leftParamExpr);
                if (varIdx == -1)
                {
                    return false;
                }

                il.Emit(OpCodes.Ldloc, closure.CurrentBlock.LocalVars[varIdx]);

                if (nodeType == ExpressionType.PreIncrementAssign)
                {
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Add);
                    if (parent != ExpressionType.Block || closure.CurrentBlock.ResultExpr == expr)
                        il.Emit(OpCodes.Dup);
                }
                else if (nodeType == ExpressionType.PostIncrementAssign)
                {
                    if (parent != ExpressionType.Block || closure.CurrentBlock.ResultExpr == expr)
                        il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Add);
                }
                else if (nodeType == ExpressionType.PreDecrementAssign)
                {
                    il.Emit(OpCodes.Ldc_I4_M1);
                    il.Emit(OpCodes.Add);
                    if (parent != ExpressionType.Block || closure.CurrentBlock.ResultExpr == expr)
                        il.Emit(OpCodes.Dup);
                }
                else if (nodeType == ExpressionType.PostDecrementAssign)
                {
                    if (parent != ExpressionType.Block || closure.CurrentBlock.ResultExpr == expr)
                        il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4_M1);
                    il.Emit(OpCodes.Add);
                }

                il.Emit(OpCodes.Stloc, closure.CurrentBlock.LocalVars[varIdx]);

                return true;
            }

            private static bool TryEmitAssign(BinaryExpression expr, Type exprType,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var left = expr.Left;
                var right = expr.Right;
                var leftNodeType = expr.Left.NodeType;
                var nodeType = expr.NodeType;

                // if this assignment is part of a single body-less expression or the result of a block
                // we should put its result to the evaluation stack before the return, otherwise we are
                // somewhere inside the block, so we shouldn't return with the result
                var shouldPushResult = closure.CurrentBlock.IsEmpty || closure.CurrentBlock.ResultExpr == expr;
                switch (leftNodeType)
                {
                    case ExpressionType.Parameter:
                        var leftParamExpr = (ParameterExpression)left;
                        var paramIndex = paramExprs.GetFirstIndex(leftParamExpr);

                        if (paramIndex != -1)
                        {
                            if (closure.HasClosure)
                                paramIndex += 1; // shift parameter indices by one, because the first one will be closure

                            if (paramIndex >= byte.MaxValue)
                                return false;

                            if (leftParamExpr.IsByRef)
                            {
                                EmitLoadParamArg(il, paramIndex, false);

                                var arithmeticNodeType = Tools.GetArithmeticFromArithmeticAssignOrSelf(nodeType);
                                if (arithmeticNodeType != nodeType)
                                {
                                    if (!TryEmitArithmeticOperation(expr, arithmeticNodeType, exprType, paramExprs, il, ref closure))
                                        return false;
                                }
                                else if (!TryEmit(right, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                                    return false;

                                EmitByRefStore(il, leftParamExpr.Type);
                            }
                            else
                            {
                                if (!TryEmit(right, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                                    return false;

                                if (shouldPushResult)
                                    il.Emit(OpCodes.Dup); // dup value to assign and return

                                il.Emit(OpCodes.Starg_S, paramIndex);
                            }

                            return true;
                        }
                        else
                        {
                            var arithmeticNodeType = Tools.GetArithmeticFromArithmeticAssignOrSelf(nodeType);
                            if (arithmeticNodeType != nodeType)
                            {
                                var varIdx = closure.CurrentBlock.VarExprs.GetFirstIndex(leftParamExpr);
                                if (varIdx != -1)
                                {
                                    if (!TryEmitArithmeticOperation(expr, arithmeticNodeType, exprType, paramExprs, il, ref closure))
                                        return false;

                                    il.Emit(OpCodes.Stloc, closure.CurrentBlock.LocalVars[varIdx]);

                                    return true;
                                }
                            }
                        }

                        // if parameter isn't passed, then it is passed into some outer lambda or it is a local variable,
                        // so it should be loaded from closure or from the locals. Then the closure is null will be an invalid state.
                        if (!closure.IsClosureConstructed)
                            return false;

                        // if it's a local variable, then store the right value in it
                        var localVariable = closure.GetDefinedLocalVarOrDefault(leftParamExpr);
                        if (localVariable != null)
                        {
                            if (!TryEmit(right, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                                return false;

                            if ((right as ParameterExpression)?.IsByRef == true)
                                il.Emit(OpCodes.Ldind_I4);

                            if (shouldPushResult) // if we have to push the result back, dup the right value
                                il.Emit(OpCodes.Dup);

                            il.Emit(OpCodes.Stloc, localVariable);
                            return true;
                        }

                        // check that it's a captured parameter by closure
                        var nonPassedParamIndex = closure.NonPassedParameters.GetFirstIndex(leftParamExpr);
                        if (nonPassedParamIndex == -1)
                            return false; // what??? no chance

                        var paramInClosureIndex = closure.Constants.Length + nonPassedParamIndex;

                        il.Emit(OpCodes.Ldarg_0); // closure is always a first argument

                        if (shouldPushResult)
                        {
                            if (!TryEmit(right, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                                return false;

                            var valueVar = il.DeclareLocal(exprType); // store left value in variable
                            if (closure.ClosureFields != null)
                            {
                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Stloc, valueVar);
                                il.Emit(OpCodes.Stfld, closure.ClosureFields[paramInClosureIndex]);
                                il.Emit(OpCodes.Ldloc, valueVar);
                            }
                            else
                            {
                                il.Emit(OpCodes.Stloc, valueVar);
                                il.Emit(OpCodes.Ldfld, ArrayClosure.ArrayField); // load array field
                                EmitLoadConstantInt(il, paramInClosureIndex); // load array item index
                                il.Emit(OpCodes.Ldloc, valueVar);
                                if (exprType.GetTypeInfo().IsValueType)
                                    il.Emit(OpCodes.Box, exprType);
                                il.Emit(OpCodes.Stelem_Ref); // put the variable into array
                                il.Emit(OpCodes.Ldloc, valueVar);
                            }
                        }
                        else
                        {
                            var isArrayClosure = closure.ClosureFields == null;
                            if (isArrayClosure)
                            {
                                il.Emit(OpCodes.Ldfld, ArrayClosure.ArrayField); // load array field
                                EmitLoadConstantInt(il, paramInClosureIndex); // load array item index
                            }

                            if (!TryEmit(right, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                                return false;

                            if (isArrayClosure)
                            {
                                if (exprType.GetTypeInfo().IsValueType)
                                    il.Emit(OpCodes.Box, exprType);
                                il.Emit(OpCodes.Stelem_Ref); // put the variable into array
                            }
                            else
                                il.Emit(OpCodes.Stfld, closure.ClosureFields[paramInClosureIndex]);
                        }

                        return true;

                    case ExpressionType.MemberAccess:
                        var memberExpr = (MemberExpression)left;
                        var member = memberExpr.Member;

                        var objExpr = memberExpr.Expression;
                        if (objExpr != null && 
                            !TryEmit(objExpr, objExpr.Type, paramExprs, il, ref closure, ExpressionType.Assign))
                            return false;

                        if (!TryEmit(right, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                            return false;

                        if (!shouldPushResult)
                            return EmitMemberAssign(il, member);

                        il.Emit(OpCodes.Dup);

                        var rightVar = il.DeclareLocal(exprType); // store right value in variable
                        il.Emit(OpCodes.Stloc, rightVar);

                        if (!EmitMemberAssign(il, member))
                            return false;

                        il.Emit(OpCodes.Ldloc, rightVar);
                        return true;

                    case ExpressionType.Index:
                        var indexExpr = (IndexExpression)left;

                        var obj = indexExpr.Object;
                        if (obj != null && 
                            !TryEmit(obj, obj.Type, paramExprs, il, ref closure, ExpressionType.Assign))
                            return false;

                        var argLength = indexExpr.Arguments.Count;
                        for (var i = 0; i < argLength; i++)
                        {
                            var arg = indexExpr.Arguments[i];
                            if (!TryEmit(arg, arg.Type, paramExprs, il, ref closure, ExpressionType.Assign))
                                return false;
                        }

                        if (!TryEmit(right, exprType, paramExprs, il, ref closure, ExpressionType.Assign))
                            return false;

                        if (!shouldPushResult)
                            return TryEmitIndexAssign(indexExpr, obj?.Type, exprType, il);

                        var variable = il.DeclareLocal(exprType); // store value in variable to return
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Stloc, variable);

                        if (!TryEmitIndexAssign(indexExpr, obj?.Type, exprType, il))
                            return false;

                        il.Emit(OpCodes.Ldloc, variable);
                        return true;

                    default: // todo: not yet support assignment targets
                        return false;
                }
            }

            private static void EmitByRefStore(ILGenerator il, Type type)
            {
                if (type == typeof(int) || type == typeof(uint))
                    il.Emit(OpCodes.Stind_I4);
                else if (type == typeof(byte))
                    il.Emit(OpCodes.Stind_I1);
                else if (type == typeof(short) || type == typeof(ushort))
                    il.Emit(OpCodes.Stind_I2);
                else if (type == typeof(long) || type == typeof(ulong))
                    il.Emit(OpCodes.Stind_I8);
                else if (type == typeof(float))
                    il.Emit(OpCodes.Stind_R4);
                else if (type == typeof(double))
                    il.Emit(OpCodes.Stind_R8);
                else if (type == typeof(object))
                    il.Emit(OpCodes.Stind_Ref);
                else if (type == typeof(IntPtr) || type == typeof(UIntPtr))
                    il.Emit(OpCodes.Stind_I);
                else  
                    il.Emit(OpCodes.Stobj, type);
            }

            private static bool TryEmitIndexAssign(IndexExpression indexExpr, Type instType, Type elementType, ILGenerator il)
            {
                if (indexExpr.Indexer != null)
                    return EmitMemberAssign(il, indexExpr.Indexer);

                if (indexExpr.Arguments.Count == 1) // one dimensional array
                {
                    if (elementType.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Stelem, elementType);
                    else
                        il.Emit(OpCodes.Stelem_Ref);
                    return true;
                }

                // multi dimensional array
                var setMethod = instType?.GetTypeInfo().GetDeclaredMethod("Set");
                return setMethod != null && EmitMethodCall(il, setMethod);
            }

            private static bool TryEmitMethodCall(MethodCallExpression expr,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure, ExpressionType parent)
            {
                var isValueTypeObj = false;
                Type objType = null;
                var objExpr = expr.Object;
                if (objExpr != null)
                {
                    objType = objExpr.Type;
                    if (!TryEmit(objExpr, objType, paramExprs, il, ref closure, ExpressionType.Call))
                        return false;

                    isValueTypeObj = objType.GetTypeInfo().IsValueType;
                    if (isValueTypeObj && objExpr.NodeType != ExpressionType.Parameter)
                        StoreAsVarAndLoadItsAddress(il, objType);
                }

                if (expr.Arguments.Count != 0 &&
                    !EmitMany(MakeByRefParameters(expr), paramExprs, il, ref closure, ExpressionType.Call))
                    return false;

                var method = expr.Method;
                if (isValueTypeObj && method.IsVirtual)
                    il.Emit(OpCodes.Constrained, objType);

                if (!EmitMethodCall(il, method))
                    return false;

                if (parent == ExpressionType.Block && closure.CurrentBlock.ResultExpr != expr && method.ReturnType != typeof(void))
                    il.Emit(OpCodes.Pop);

                return true;
            }

            // if call is done into byref method parameters there is no indicators in tree, so grab that from method
            // current approach is to copy into new list only if there are by ref with by ref parameters,
            // possible approach to store hit map of small size (possible 256 bit #89) to check if parameter is by ref
            // https://stackoverflow.com/questions/12658883/what-is-the-maximum-number-of-parameters-that-a-c-sharp-method-can-be-defined-as
            private static IReadOnlyList<Expression> MakeByRefParameters(MethodCallExpression expr)
            {
                List<Expression> refed = null;
                var receivingParameters = expr.Method.GetParameters();
                var exprParameters = expr.Method.GetParameters();
                for (var i = 0; i < exprParameters.Length; i++)
                {
                    if (receivingParameters[i].ParameterType.IsByRef)
                    {
                        if (refed == null)
                            refed = new List<Expression>(expr.Arguments);

                        var passed = expr.Arguments[i] as ParameterExpression;
                        if (passed != null && !passed.IsByRef)
                            refed[i] = Expression.Parameter(passed.Type.MakeByRefType(), passed.Name);
                    }
                }
                return (IReadOnlyList<Expression>)refed ?? expr.Arguments;
            }

            private static void StoreAsVarAndLoadItsAddress(ILGenerator il, Type varType)
            {
                var theVar = il.DeclareLocal(varType);
                il.Emit(OpCodes.Stloc, theVar);
                il.Emit(OpCodes.Ldloca, theVar);
            }

            private static bool TryEmitMemberAccess(MemberExpression expr, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var objType = default(Type);
                var objNodeType = ExpressionType.Default;

                var member = expr.Member;
                var prop = member as PropertyInfo;

                var objExpr = expr.Expression;
                if (objExpr != null)
                {
                    objType = objExpr.Type;
                    objNodeType = objExpr.NodeType;
                    if (!TryEmit(objExpr, objType, paramExprs, il, ref closure,
                        prop != null ? ExpressionType.Call : ExpressionType.MemberAccess))
                        return false;
                }

                // Value type special treatment to load address of value instance in order to access a field or call a method.
                // Parameter should be excluded because it already loads an address via Ldarga, and you don't need to.
                // And for field access no need to load address, cause the field stored on stack nearby
                if (objType != null && objNodeType != ExpressionType.Parameter && prop != null &&
                    objType.GetTypeInfo().IsValueType)
                    StoreAsVarAndLoadItsAddress(il, objType);

                if (prop != null)
                {
                    var propGetMethod = TryGetPropertyGetMethod(prop);
                    return propGetMethod != null && EmitMethodCall(il, propGetMethod);
                }

                var field = member as FieldInfo;
                if (field != null)
                {
                    il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
                    return true;
                }

                return false;
            }

            private static MethodInfo TryGetPropertyGetMethod(PropertyInfo prop) => 
                prop.DeclaringType.GetTypeInfo().GetDeclaredMethod("get_" + prop.Name);

            // ReSharper disable once FunctionComplexityOverflow
            private static bool TryEmitNestedLambda(LambdaExpression lambdaExpr,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                // First, find in closed compiled lambdas the one corresponding to the current lambda expression.
                // Situation with not found lambda is not possible/exceptional,
                // it means that we somehow skipped the lambda expression while collecting closure info.
                var outerNestedLambdas = closure.NestedLambdas;
                var outerNestedLambdaIndex = outerNestedLambdas.GetFirstIndex(it => it.LambdaExpr == lambdaExpr);
                if (outerNestedLambdaIndex == -1)
                    return false;

                var nestedLambdaInfo = outerNestedLambdas[outerNestedLambdaIndex];
                var nestedLambda = nestedLambdaInfo.Lambda;

                var outerConstants = closure.Constants;
                var outerNonPassedParams = closure.NonPassedParameters;

                // Load compiled lambda on stack counting the offset
                outerNestedLambdaIndex += outerConstants.Length + outerNonPassedParams.Length;

                if (!LoadClosureFieldOrItem(ref closure, il, outerNestedLambdaIndex, nestedLambda.GetType()))
                    return false;

                // If lambda does not use any outer parameters to be set in closure, then we're done
                var nestedClosureInfo = nestedLambdaInfo.ClosureInfo;
                if (!nestedClosureInfo.HasClosure)
                    return true;

                // If closure is array-based, the create a new array to represent closure for the nested lambda
                var isNestedArrayClosure = nestedClosureInfo.ClosureFields == null;
                if (isNestedArrayClosure)
                {
                    EmitLoadConstantInt(il, nestedClosureInfo.ClosedItemCount); // size of array
                    il.Emit(OpCodes.Newarr, typeof(object));
                }

                // Load constants on stack
                var nestedConstants = nestedClosureInfo.Constants;
                if (nestedConstants.Length != 0)
                {
                    for (var nestedConstIndex = 0; nestedConstIndex < nestedConstants.Length; nestedConstIndex++)
                    {
                        var nestedConstant = nestedConstants[nestedConstIndex];

                        // Find constant index in the outer closure
                        var outerConstIndex = outerConstants.GetFirstIndex(nestedConstant);
                        if (outerConstIndex == -1)
                            return false; // some error is here

                        if (isNestedArrayClosure)
                        {
                            // Duplicate nested array on stack to store the item, and load index to where to store
                            il.Emit(OpCodes.Dup);
                            EmitLoadConstantInt(il, nestedConstIndex);
                        }

                        if (!LoadClosureFieldOrItem(ref closure, il, outerConstIndex, nestedConstant.Type))
                            return false;

                        if (isNestedArrayClosure)
                        {
                            if (nestedConstant.Type.GetTypeInfo().IsValueType)
                                il.Emit(OpCodes.Box, nestedConstant.Type);
                            il.Emit(OpCodes.Stelem_Ref); // store the item in array
                        }
                    }
                }

                // Load used and closed parameter values on stack
                var nestedNonPassedParams = nestedClosureInfo.NonPassedParameters;
                for (var nestedParamIndex = 0; nestedParamIndex < nestedNonPassedParams.Length; nestedParamIndex++)
                {
                    var nestedUsedParam = nestedNonPassedParams[nestedParamIndex];

                    Type nestedUsedParamType = null;
                    if (isNestedArrayClosure)
                    {
                        // get a param type for the later
                        nestedUsedParamType = nestedUsedParam.Type;

                        // Duplicate nested array on stack to store the item, and load index to where to store
                        il.Emit(OpCodes.Dup);
                        EmitLoadConstantInt(il, nestedConstants.Length + nestedParamIndex);
                    }

                    var paramIndex = paramExprs.GetFirstIndex(nestedUsedParam);
                    if (paramIndex != -1) // load param from input params
                    {
                        // +1 is set cause of added first closure argument
                        EmitLoadParamArg(il, 1 + paramIndex, false);
                    }
                    else // load parameter from outer closure or from the locals
                    {
                        if (outerNonPassedParams.Length == 0)
                            return false; // impossible, better to throw?

                        var variable = closure.GetDefinedLocalVarOrDefault(nestedUsedParam);
                        if (variable != null) // it's a local variable
                        {
                            il.Emit(OpCodes.Ldloc, variable);
                        }
                        else // it's a parameter from outer closure
                        {
                            var outerParamIndex = outerNonPassedParams.GetFirstIndex(nestedUsedParam);
                            if (outerParamIndex == -1 ||
                                !LoadClosureFieldOrItem(ref closure, il, outerConstants.Length + outerParamIndex,
                                nestedUsedParamType, nestedUsedParam))
                                return false;
                        }
                    }

                    if (isNestedArrayClosure)
                    {
                        if (nestedUsedParamType.GetTypeInfo().IsValueType)
                            il.Emit(OpCodes.Box, nestedUsedParamType);

                        il.Emit(OpCodes.Stelem_Ref); // store the item in array
                    }
                }

                // Load nested lambdas on stack
                var nestedNestedLambdas = nestedClosureInfo.NestedLambdas;
                if (nestedNestedLambdas.Length != 0)
                {
                    for (var nestedLambdaIndex = 0; nestedLambdaIndex < nestedNestedLambdas.Length; nestedLambdaIndex++)
                    {
                        var nestedNestedLambda = nestedNestedLambdas[nestedLambdaIndex];

                        // Find constant index in the outer closure
                        var outerLambdaIndex = outerNestedLambdas.GetFirstIndex(it => it.LambdaExpr == nestedNestedLambda.LambdaExpr);
                        if (outerLambdaIndex == -1)
                            return false; // some error is here

                        // Duplicate nested array on stack to store the item, and load index to where to store
                        if (isNestedArrayClosure)
                        {
                            il.Emit(OpCodes.Dup);
                            EmitLoadConstantInt(il, nestedConstants.Length + nestedNonPassedParams.Length + nestedLambdaIndex);
                        }

                        outerLambdaIndex += outerConstants.Length + outerNonPassedParams.Length;

                        var nestedNestedLambdaType = nestedNestedLambda.Lambda.GetType();
                        if (!LoadClosureFieldOrItem(ref closure, il, outerLambdaIndex, nestedNestedLambdaType))
                            return false;

                        if (isNestedArrayClosure)
                            il.Emit(OpCodes.Stelem_Ref); // store the item in array
                    }
                }

                // Create nested closure object composed of all constants, params, lambdas loaded on stack
                if (isNestedArrayClosure)
                    il.Emit(OpCodes.Newobj, ArrayClosure.Constructor);
                else
                    il.Emit(OpCodes.Newobj, nestedClosureInfo.ClosureType.GetTypeInfo().DeclaredConstructors.GetFirst());

                return EmitMethodCall(il, GetCurryClosureMethod(nestedLambda, nestedLambdaInfo.IsAction));
            }

            private static MethodInfo GetCurryClosureMethod(object lambda, bool isAction)
            {
                var lambdaTypeArgs = lambda.GetType().GetTypeInfo().GenericTypeArguments;
                return isAction
                    ? CurryClosureActions.Methods[lambdaTypeArgs.Length - 1].MakeGenericMethod(lambdaTypeArgs)
                    : CurryClosureFuncs.Methods[lambdaTypeArgs.Length - 2].MakeGenericMethod(lambdaTypeArgs);
            }

            private static bool TryInvokeLambda(InvocationExpression expr,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                if (!TryEmit(expr.Expression, expr.Expression.Type, paramExprs, il, ref closure, ExpressionType.Invoke) ||
                    !EmitMany(expr.Arguments, paramExprs, il, ref closure, ExpressionType.Invoke))
                    return false;

                var invokeMethod = expr.Expression.Type.GetTypeInfo().GetDeclaredMethod("Invoke");
                return invokeMethod != null && EmitMethodCall(il, invokeMethod);
            }

            private static bool TryEmitInvertedNullComparison(Expression expr,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                if (expr is BinaryExpression b)
                {
                    if (expr.NodeType != ExpressionType.Equal && expr.NodeType != ExpressionType.NotEqual)
                        return false;
                    if (b.Right is ConstantExpression r && r.Value == null)
                    {
                        if (!TryEmit(b.Left, b.Left.Type, paramExprs, il, ref closure, ExpressionType.Default))
                            return false;
                    }
                    else if (b.Left is ConstantExpression l && l.Value == null)
                    {
                        if (!TryEmit(b.Right, b.Right.Type, paramExprs, il, ref closure, ExpressionType.Default))
                            return false;
                    }
                    else
                        return false;

                    return true;
                }

                return false;
            }

            private static bool TryEmitComparison(BinaryExpression expr,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                // todo: for now, handling only parameters of the same type
                // todo: for now, Nullable is not supported
                var leftOpType = expr.Left.Type;
                var leftIsNull = leftOpType.IsNullable();
                var rightOpType = expr.Right.Type;
                if (expr.Right is ConstantExpression c && c.Value == null && expr.Right.Type == typeof(object))
                    rightOpType = leftOpType;

                if (leftOpType != rightOpType)
                    return false;

                LocalBuilder lVar = null, rVar = null;
                if (!TryEmit(expr.Left, expr.Left.Type, paramExprs, il, ref closure, ExpressionType.Default))
                    return false;
                if (leftIsNull)
                {
                    lVar = il.DeclareLocal(leftOpType);
                    il.Emit(OpCodes.Stloc_S, lVar);
                    il.Emit(OpCodes.Ldloca_S, lVar);
                    var mthValue = leftOpType.GetTypeInfo().GetDeclaredMethods("GetValueOrDefault").First(x => x.GetParameters().Length == 0);
                    if (!EmitMethodCall(il, mthValue))
                        return false;
                    leftOpType = Nullable.GetUnderlyingType(leftOpType);
                }

                if (!TryEmit(expr.Right, expr.Right.Type, paramExprs, il, ref closure, ExpressionType.Default))
                    return false;
                if (rightOpType.IsNullable())
                {
                    rVar = il.DeclareLocal(rightOpType);
                    il.Emit(OpCodes.Stloc_S, rVar);
                    il.Emit(OpCodes.Ldloca_S, rVar);
                    var mthValue = rightOpType.GetTypeInfo().GetDeclaredMethods("GetValueOrDefault").First(x => x.GetParameters().Length == 0);
                    if (!EmitMethodCall(il, mthValue))
                        return false;
                }


                var exprNodeType = expr.NodeType;
                var leftOpTypeInfo = leftOpType.GetTypeInfo();
                if (!leftOpTypeInfo.IsPrimitive && !leftOpTypeInfo.IsEnum)
                {
                    var methodName
                        = exprNodeType == ExpressionType.Equal ? "op_Equality"
                        : exprNodeType == ExpressionType.NotEqual ? "op_Inequality"
                        : exprNodeType == ExpressionType.GreaterThan ? "op_GreaterThan"
                        : exprNodeType == ExpressionType.GreaterThanOrEqual ? "op_GreaterThanOrEqual"
                        : exprNodeType == ExpressionType.LessThan ? "op_LessThan"
                        : exprNodeType == ExpressionType.LessThanOrEqual ? "op_LessThanOrEqual" : null;

                    if (methodName == null)
                        return false;

                    // todo: for now handling only parameters of the same type
                    var method = leftOpTypeInfo.DeclaredMethods.GetFirst(m =>
                        m.IsStatic && m.Name == methodName &&
                        m.GetParameters().All(p => p.ParameterType == leftOpType));

                    if (method != null)
                        return EmitMethodCall(il, method);
                    
                    if (exprNodeType != ExpressionType.Equal && exprNodeType != ExpressionType.NotEqual)
                        return false;

                    EmitMethodCall(il, _objectEqualsMethod);
                    if (exprNodeType == ExpressionType.NotEqual) // invert result for not equal
                    {
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                    }

                    if (leftIsNull)
                        goto nullCheck;

                    return true;
                }

                // handle primitives comparison
                switch (exprNodeType)
                {
                    case ExpressionType.Equal:
                        il.Emit(OpCodes.Ceq);
                        break;

                    case ExpressionType.NotEqual:
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                        break;

                    case ExpressionType.LessThan:
                        il.Emit(OpCodes.Clt);
                        break;

                    case ExpressionType.GreaterThan:
                        il.Emit(OpCodes.Cgt);
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

                    default:
                        return false;
                }

nullCheck:
                if (leftIsNull)
                {
                    il.Emit(OpCodes.Ldloca_S, lVar);
                    var mth = expr.Left.Type.GetTypeInfo().GetDeclaredMethod("get_HasValue");
                    if (!EmitMethodCall(il, mth))
                        return false;
                    il.Emit(OpCodes.Ldloca_S, rVar);
                    if (!EmitMethodCall(il, mth))
                        return false;

                    switch (exprNodeType)
                    {
                        case ExpressionType.Equal:
                            il.Emit(OpCodes.Ceq); // compare both HasValue calls
                            il.Emit(OpCodes.And); // both results need to be true
                            break;

                        case ExpressionType.NotEqual:
                            il.Emit(OpCodes.Ceq);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ceq);
                            il.Emit(OpCodes.Or);
                            break;

                        case ExpressionType.LessThan:
                        case ExpressionType.GreaterThan:
                        case ExpressionType.LessThanOrEqual:
                        case ExpressionType.GreaterThanOrEqual:
                            il.Emit(OpCodes.Ceq);
                            il.Emit(OpCodes.Ldc_I4_1);
                            il.Emit(OpCodes.Ceq);
                            il.Emit(OpCodes.And);
                            break;

                        default:
                            return false;
                    }
                }
                return true;
            }

            private static bool TryEmitArithmeticOperation(
                BinaryExpression expr, ExpressionType exprNodeType, Type exprType, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                if (!EmitBinary(expr, paramExprs, il, ref closure, parent: exprNodeType))
                    return false;

                var exprTypeInfo = exprType.GetTypeInfo();
                if (!exprTypeInfo.IsPrimitive)
                {
                    var methodName
                        = exprNodeType == ExpressionType.Add ? "op_Addition"
                        : exprNodeType == ExpressionType.AddChecked ? "op_Addition"
                        : exprNodeType == ExpressionType.Subtract ? "op_Subtraction"
                        : exprNodeType == ExpressionType.SubtractChecked ? "op_Subtraction"
                        : exprNodeType == ExpressionType.Multiply ? "op_Multiply"
                        : exprNodeType == ExpressionType.MultiplyChecked ? "op_Multiply"
                        : exprNodeType == ExpressionType.Divide ? "op_Division"
                        : exprNodeType == ExpressionType.Modulo ? "op_Modulus"
                        : null;

                    if (methodName == null)
                        return false;

                    var method = exprTypeInfo.GetDeclaredMethod(methodName);
                    return method != null && EmitMethodCall(il, method);
                }

                switch (exprNodeType)
                {
                    case ExpressionType.Add:
                    case ExpressionType.AddAssign:
                        il.Emit(OpCodes.Add);
                        return true;

                    case ExpressionType.AddChecked:
                    case ExpressionType.AddAssignChecked:
                        il.Emit(IsUnsigned(exprType) ? OpCodes.Add_Ovf_Un : OpCodes.Add_Ovf);
                        return true;

                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractAssign:
                        il.Emit(OpCodes.Sub);
                        return true;

                    case ExpressionType.SubtractChecked:
                    case ExpressionType.SubtractAssignChecked:
                        il.Emit(IsUnsigned(exprType) ? OpCodes.Sub_Ovf_Un : OpCodes.Sub_Ovf);
                        return true;

                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyAssign:
                        il.Emit(OpCodes.Mul);
                        return true;

                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.MultiplyAssignChecked:
                        il.Emit(IsUnsigned(exprType) ? OpCodes.Mul_Ovf_Un : OpCodes.Mul_Ovf);
                        return true;

                    case ExpressionType.Divide:
                    case ExpressionType.DivideAssign:
                        il.Emit(OpCodes.Div);
                        return true;

                    case ExpressionType.Modulo:
                    case ExpressionType.ModuloAssign:
                        il.Emit(OpCodes.Rem);
                        return true;

                    case ExpressionType.And:
                    case ExpressionType.AndAssign:
                        il.Emit(OpCodes.And);
                        return true;

                    case ExpressionType.Or:
                    case ExpressionType.OrAssign:
                        il.Emit(OpCodes.Or);
                        return true;

                    case ExpressionType.ExclusiveOr:
                    case ExpressionType.ExclusiveOrAssign:
                        il.Emit(OpCodes.Xor);
                        return true;

                    case ExpressionType.LeftShift:
                    case ExpressionType.LeftShiftAssign:
                        il.Emit(OpCodes.Shl);
                        return true;

                    case ExpressionType.RightShift:
                    case ExpressionType.RightShiftAssign:
                        il.Emit(OpCodes.Shr);
                        return true;

                    case ExpressionType.Power:
                        return EmitMethodCall(il, typeof(Math).GetTypeInfo().GetDeclaredMethod("Pow"));
                }

                return false;
            }

            private static bool IsUnsigned(Type type) =>
                type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);

            private static bool TryEmitLogicalOperator(BinaryExpression expr,
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var leftExpr = expr.Left;
                if (!TryEmit(leftExpr, leftExpr.Type, paramExprs, il, ref closure, ExpressionType.Default))
                    return false;

                var labelSkipRight = il.DefineLabel();
                var isAnd = expr.NodeType == ExpressionType.AndAlso;
                il.Emit(isAnd ? OpCodes.Brfalse : OpCodes.Brtrue, labelSkipRight);

                var rightExpr = expr.Right;
                if (!TryEmit(rightExpr, rightExpr.Type, paramExprs, il, ref closure, ExpressionType.Default))
                    return false;

                var labelDone = il.DefineLabel();
                il.Emit(OpCodes.Br, labelDone);

                il.MarkLabel(labelSkipRight); // label the second branch
                il.Emit(isAnd ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
                il.MarkLabel(labelDone);

                return true;
            }

            private static bool TryEmitConditional(ConditionalExpression expr, 
                IReadOnlyList<ParameterExpression> paramExprs, ILGenerator il, ref ClosureInfo closure)
            {
                var testExpr = expr.Test;
                var usedInverted = false;

                if (TryEmitInvertedNullComparison(testExpr, paramExprs, il, ref closure))
                    usedInverted = true;
                else if (!TryEmit(testExpr, testExpr.Type, paramExprs, il, ref closure, ExpressionType.Conditional))
                    return false;

                var labelIfFalse = il.DefineLabel();
                il.Emit(usedInverted && testExpr.NodeType == ExpressionType.Equal ? OpCodes.Brtrue : OpCodes.Brfalse, labelIfFalse);

                var ifTrueExpr = expr.IfTrue;
                if (!TryEmit(ifTrueExpr, ifTrueExpr.Type, paramExprs, il, ref closure, ExpressionType.Conditional))
                    return false;

                var labelDone = il.DefineLabel();
                il.Emit(OpCodes.Br, labelDone);

                il.MarkLabel(labelIfFalse);
                var ifFalseExpr = expr.IfFalse;
                if (!TryEmit(ifFalseExpr, ifFalseExpr.Type, paramExprs, il, ref closure, ExpressionType.Conditional))
                    return false;

                il.MarkLabel(labelDone);
                return true;
            }

            private static bool EmitMethodCall(ILGenerator il, MethodInfo method)
            {
                il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
                return true;
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
                    case int n when (n > -129 && n < 128):
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)i);
                        break;
                    default:
                        il.Emit(OpCodes.Ldc_I4, i);
                        break;
                }
            }
        }
    }

    // Helpers targeting the performance. Extensions method names may be a bit funny (non standard), 
    // in order to prevent conflicts with YOUR helpers with standard names
    internal static class Tools
    {
        public static bool IsValueType(this Type type) => type.GetTypeInfo().IsValueType;

        public static bool IsNullable(this Type type) =>
            type.GetTypeInfo().IsGenericType && type.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>);

        public static ConstructorInfo GetConstructorByArgs(this Type type, params Type[] args) =>
            type.GetTypeInfo().DeclaredConstructors.GetFirst(c => c.GetParameters().Map(p => p.ParameterType).SequenceEqual(args));

        //todo: test what is faster? Copy and inline switch? Switch in method? Ors in method?
        internal static bool IsArithmetic(ExpressionType arithmetic) 
            => arithmetic == ExpressionType.Add
            || arithmetic == ExpressionType.AddChecked
            || arithmetic == ExpressionType.Subtract
            || arithmetic == ExpressionType.SubtractChecked
            || arithmetic == ExpressionType.Multiply
            || arithmetic == ExpressionType.MultiplyChecked
            || arithmetic == ExpressionType.Divide
            || arithmetic == ExpressionType.Modulo
            || arithmetic == ExpressionType.Power
            || arithmetic == ExpressionType.And
            || arithmetic == ExpressionType.Or
            || arithmetic == ExpressionType.ExclusiveOr
            || arithmetic == ExpressionType.LeftShift
            || arithmetic == ExpressionType.RightShift;

        internal static ExpressionType GetArithmeticFromArithmeticAssignOrSelf(ExpressionType arithmetic)
        {
            switch (arithmetic)
            {
                case ExpressionType.AddAssign:             return ExpressionType.Add;
                case ExpressionType.AddAssignChecked:      return ExpressionType.AddChecked;
                case ExpressionType.SubtractAssign:        return ExpressionType.Subtract;
                case ExpressionType.SubtractAssignChecked: return ExpressionType.SubtractChecked;
                case ExpressionType.MultiplyAssign:        return ExpressionType.Multiply;
                case ExpressionType.MultiplyAssignChecked: return ExpressionType.MultiplyChecked;
                case ExpressionType.DivideAssign:          return ExpressionType.Divide;
                case ExpressionType.ModuloAssign:          return ExpressionType.Modulo;
                case ExpressionType.PowerAssign:           return ExpressionType.Power;
                case ExpressionType.AndAssign:             return ExpressionType.And;
                case ExpressionType.OrAssign:              return ExpressionType.Or;
                case ExpressionType.ExclusiveOrAssign:     return ExpressionType.ExclusiveOr;
                case ExpressionType.LeftShiftAssign:       return ExpressionType.LeftShift;
                case ExpressionType.RightShiftAssign:      return ExpressionType.RightShift;
                default: return arithmetic;
            }
        }

        public static T[] AsArray<T>(this IEnumerable<T> xs) => xs as T[] ?? xs.ToArray();

        public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> xs) => xs as IReadOnlyList<T> ?? xs.ToArray();

        private static class EmptyArray<T>
        {
            public static readonly T[] Value = new T[0];
        }

        public static T[] Empty<T>() => EmptyArray<T>.Value;

        public static T[] WithLast<T>(this T[] source, T value)
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

        public static Type[] GetParamTypes(IReadOnlyList<ParameterExpression> paramExprs)
        {
            if (paramExprs == null || paramExprs.Count == 0)
                return Empty<Type>();

            if (paramExprs.Count == 1)
                return new[] { paramExprs[0].IsByRef ? paramExprs[0].Type.MakeByRefType() : paramExprs[0].Type };

            var paramTypes = new Type[paramExprs.Count];
            for (var i = 0; i < paramTypes.Length; i++)
            {
                var parameterExpr = paramExprs[i];
                paramTypes[i] = parameterExpr.IsByRef ? parameterExpr.Type.MakeByRefType() : parameterExpr.Type;
            }

            return paramTypes;
        }

        public static Type GetFuncOrActionType(Type[] paramTypes, Type returnType)
        {
            if (returnType == typeof(void))
            {
                switch (paramTypes.Length)
                {
                    case 0: return typeof(Action);
                    case 1: return typeof(Action<>).MakeGenericType(paramTypes);
                    case 2: return typeof(Action<,>).MakeGenericType(paramTypes);
                    case 3: return typeof(Action<,,>).MakeGenericType(paramTypes);
                    case 4: return typeof(Action<,,,>).MakeGenericType(paramTypes);
                    case 5: return typeof(Action<,,,,>).MakeGenericType(paramTypes);
                    case 6: return typeof(Action<,,,,,>).MakeGenericType(paramTypes);
                    case 7: return typeof(Action<,,,,,,>).MakeGenericType(paramTypes);
                    default:
                        throw new NotSupportedException(
                            string.Format("Action with so many ({0}) parameters is not supported!", paramTypes.Length));
                }
            }

            paramTypes = paramTypes.WithLast(returnType);
            switch (paramTypes.Length)
            {
                case 1: return typeof(Func<>).MakeGenericType(paramTypes);
                case 2: return typeof(Func<,>).MakeGenericType(paramTypes);
                case 3: return typeof(Func<,,>).MakeGenericType(paramTypes);
                case 4: return typeof(Func<,,,>).MakeGenericType(paramTypes);
                case 5: return typeof(Func<,,,,>).MakeGenericType(paramTypes);
                case 6: return typeof(Func<,,,,,>).MakeGenericType(paramTypes);
                case 7: return typeof(Func<,,,,,,>).MakeGenericType(paramTypes);
                case 8: return typeof(Func<,,,,,,,>).MakeGenericType(paramTypes);
                default:
                    throw new NotSupportedException(
                        string.Format("Func with so many ({0}) parameters is not supported!", paramTypes.Length));
            }
        }

        public static int GetFirstIndex<T>(this IReadOnlyList<T> source, object item)
        {
            if (source == null || source.Count == 0)
                return -1;
            var count = source.Count;
            if (count == 1)
                return ReferenceEquals(source[0], item) ? 0 : -1;
            for (var i = 0; i < count; ++i)
                if (ReferenceEquals(source[i], item))
                    return i;
            return -1;
        }

        public static int GetFirstIndex<T>(this T[] source, Func<T, bool> predicate)
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

        public static T GetFirst<T>(this IEnumerable<T> source)
        {
            var list = source as IReadOnlyList<T>;
            return list == null ? source.FirstOrDefault() : list.Count != 0 ? list[0] : default(T);
        }

        public static T GetFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var arr = source as T[];
            if (arr == null)
                return source.FirstOrDefault(predicate);
            var index = arr.GetFirstIndex(predicate);
            return index == -1 ? default(T) : arr[index];
        }

        public static R[] Map<T, R>(this IReadOnlyList<T> source, Func<T, R> project)
        {
            if (source == null || source.Count == 0)
                return Empty<R>();

            if (source.Count == 1)
                return new[] { project(source[0]) };

            var result = new R[source.Count];
            for (var i = 0; i < result.Length; ++i)
                result[i] = project(source[i]);
            return result;
        }
    }
}

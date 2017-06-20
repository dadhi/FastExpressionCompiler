using System;
using System.Collections.Generic;
using System.Linq;

namespace FastExpressionCompiler.UnitTests
{
    public class DryExpressionCompiler
    {
        /// <summary>Tries to compile lambda expression to <typeparamref name="TDelegate"/>.</summary>
        /// <typeparam name="TDelegate">The compatible delegate type, otherwise case will throw.</typeparam>
        /// <param name="lambdaExpr">Lambda expression to compile.</param>
        /// <returns>Compiled delegate.</returns>
        public static TDelegate TryCompile<TDelegate>(LambdaDryExpression lambdaExpr)
            where TDelegate : class
        {
            var paramExprs = lambdaExpr.Parameters;
            var paramTypes = paramExprs.Select(p => p.Type).ToArray();
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
            DryExpression bodyExpr,
            IList<ParameterDryExpression> paramExprs,
            Type[] paramTypes, 
            Type returnType) where TDelegate : class
        {
            //ExpressionCompiler.ClosureInfo ignored = null;
            //return (TDelegate)TryCompile(ref ignored,
            //    typeof(TDelegate), paramTypes, returnType, bodyExpr, paramExprs);
            return null;
        }
    }
}

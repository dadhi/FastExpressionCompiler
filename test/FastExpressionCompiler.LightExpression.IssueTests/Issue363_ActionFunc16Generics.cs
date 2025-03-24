using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using static FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.LightExpression.IssueTests
{

    public class Issue363_ActionFunc16Generics : ITest
    {
        public static readonly object[] TestCases = Enumerable.Range(0, 16).Cast<object>().ToArray();

        public int Run()
        {
            foreach (var testCase in TestCases)
            {
                Can_Create_Func((int)testCase);
                Can_Create_Action((int)testCase);
            }
            Supports_16_Func_Params();
            Supports_16_Action_Params();
            return 4;
        }


        public void Supports_16_Func_Params()
        {
            LambdaExpression lambda = Lambda(
                Constant(0L),
                Enumerable.Range(0, 16)
                .Select(i => Parameter(typeof(int), $"p{i}")));

            var compiled = lambda.CompileFast<
                Func<
                int, int, int, int,
                int, int, int, int,
                int, int, int, int,
                int, int, int, int,
                long>>(flags: CompilerFlags.ThrowOnNotSupportedExpression);

            Asserts.IsNotNull(compiled);
            Asserts.AreEqual(0L, compiled(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15));
        }


        public void Supports_16_Action_Params()
        {
            LambdaExpression lambda = Lambda(
                Empty(),
                Enumerable.Range(0, 16)
                .Select(i => Parameter(typeof(int), $"p{i}")));

            var compiled = lambda.CompileFast<
                Action<
                int, int, int, int,
                int, int, int, int,
                int, int, int, int,
                int, int, int, int
                >>(flags: CompilerFlags.ThrowOnNotSupportedExpression);

            Asserts.IsNotNull(compiled);
        }

        public void Can_Create_Func(int paramCount)
        {
            var parameters = Enumerable
                .Range(0, paramCount)
                .Select(i => Parameter(typeof(int), $"p{i}"))
                .ToArray();

            var arrayInit = NewArrayInit(typeof(int), parameters);

            var expr = Lambda(arrayInit, parameters);
            expr.PrintCSharp();

#if LIGHT_EXPRESSION
            var sysExpr = expr.ToLambdaExpression();
            var restoredExpr = sysExpr.ToLightExpression();
            restoredExpr.PrintCSharp();
            Asserts.AreEqual(expr.ToCSharpString(), restoredExpr.ToCSharpString());
#endif

            // (a, b, c, ...) => new[] { a, b, c, ... }
            var fs = expr.CompileFast(flags: CompilerFlags.ThrowOnNotSupportedExpression);

            var result = (int[])fs.DynamicInvoke(parameters.Select(_ => (object)1).ToArray());

            Asserts.AreEqual(paramCount, result.Length);
        }

        public void Can_Create_Action(int paramCount)
        {
            ParameterExpression[] parameters = Enumerable
                .Range(0, paramCount)
                .Select(i => Parameter(typeof(int), $"p{i}"))
                .ToArray();

            List<int> list = new();

            MethodInfo addMethod = typeof(List<int>).GetMethod(nameof(List<int>.Add));
            ConstantExpression listExp = Constant(list);
            BlockExpression body = Block(
                typeof(void),
                parameters.Select((p) => (Expression)Call(listExp, addMethod, p)).ToArray());

            LambdaExpression lambda = Lambda(body, parameters);

            // (a, b, c, ...) => list.Add(a); list.Add(b); list.Add(c); ...
            Delegate compiled = lambda.CompileFast(flags: CompilerFlags.ThrowOnNotSupportedExpression);

            compiled.DynamicInvoke(parameters.Select(_ => (object)1).ToArray());

            Asserts.AreEqual(paramCount, list.Sum());
        }
    }
}

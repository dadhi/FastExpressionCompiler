using System;
using NUnit.Framework;
using System.Reflection.Emit;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class ConvertOperatorsTests : ITest
    {
        public int Run()
        {
            Convert_Func_to_Custom_delegate_should_work();
            Target_type_explicit_operator_in_action();
            Generic_converter_should_work();

#if !LIGHT_EXPRESSION
            Target_type_implicit_operator();
            Source_type_implicit_operator();
            Target_type_explicit_operator();
            return 5;
#else
            return 2;
#endif
        }

#if !LIGHT_EXPRESSION
        [Test]
        public void Target_type_implicit_operator()
        {
            Expression<Func<string, X>> expr = s => s;

            var f = expr.CompileFast(true);
            var x = f("hey");

            Assert.AreEqual("X:hey", x.S);
        }

        [Test]
        public void Source_type_implicit_operator()
        {
            Expression<Func<Z, X>> expr = z => z;

            var f = expr.CompileFast(true);
            var x = f(new Z("a"));

            Assert.AreEqual("a", x.S);
        }

        [Test]
        public void Target_type_explicit_operator()
        {
            Expression<Func<string, Y>> expr = s => (Y)s;

            var f = expr.CompileFast(true);
            var y = f("hey");

            Assert.AreEqual("X:hey", y.S);
        }
#endif

        public delegate string GetString();

#if LIGHT_EXPRESSION

        [Test]
        public void Convert_Func_to_Custom_delegate_should_work()
        {
            var p = Parameter(typeof(Func<string>), "fs");
            var e = Lambda<Func<Func<string>, GetString>>(
                TryConvertDelegateIntrinsic<GetString>(p),
                p);
            e.PrintCSharp();
            var @cs = (Func<Func<string>, GetString>)((Func<string> fs) =>
                (GetString)fs.Invoke);
            var getString = @cs(() => "hey");
            Assert.AreEqual("hey", getString());

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldftn, // Func`1.Invoke
                OpCodes.Newobj, // GetString
                OpCodes.Ret
            );

            getString = ff(() => "hey");
            Assert.AreEqual("hey", getString());
        }
#else
        [Test]
        public void Convert_Func_to_Custom_delegate_should_work()
        {
            Expression<Func<Func<string>, GetString>> e = p => (GetString)p.Invoke;

            e.PrintCSharp();
            var @cs = (Func<Func<string>, GetString>)((Func<string> fs) =>
                (GetString)fs.Invoke);
            var getString = @cs(() => "hey");
            Assert.AreEqual("hey", getString());

            var ff = e.CompileFast(true);
            ff.PrintIL();
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldfld,      // ArrayClosure.ConstantsAndNestedLambdas
                OpCodes.Stloc_0,
                OpCodes.Ldloc_0,
                OpCodes.Ldc_I4_0,
                OpCodes.Ldelem_Ref,
                OpCodes.Ldtoken,    // GetString
                OpCodes.Call,       // Type.GetTypeFromHandle
                OpCodes.Ldarg_1,
                OpCodes.Callvirt,   // MethodInfo.CreateDelegate
                OpCodes.Castclass,  // GetString
                OpCodes.Ret
            );

            getString = ff(() => "hey");
            Assert.AreEqual("hey", getString());
        }
#endif

        [Test]
        public void Target_type_explicit_operator_in_action()
        {
            var sExpr = Parameter(typeof(string), "s");
            var expr = Lambda<Action<string>>(
                Convert(sExpr, typeof(Y)),
                sExpr);

            var f = expr.CompileFast(true);
            f("hey");
        }

        [Test]
        public void Generic_converter_should_work()
        {
            var expr = GetGenericConverter<int, XEnum>();
            var fs = expr.CompileSys();
            Assert.AreEqual(XEnum.C, fs(2));

            var f = expr.CompileFast(true);
            Assert.AreEqual(XEnum.C, f(2));

            var f1 = expr.TryCompileWithoutClosure<Func<int, XEnum>>();
            Assert.AreEqual(XEnum.C, f1(2));
        }

        public static Expression<Func<TFrom, TTo>> GetGenericConverter<TFrom, TTo>()
        {
            var fromParam = Parameter(typeof(TFrom));
            return Lambda<Func<TFrom, TTo>>(Expression.Convert(fromParam, typeof(TTo)), fromParam);
        }

        public enum XEnum { A, B, C };

        public struct X
        {
            public static implicit operator X(string s) => new X("X:" + s);
            public readonly string S;
            public X(string s)
            {
                S = s;
            }
        }

        public struct Y
        {
            public static explicit operator Y(string s) => new Y("X:" + s);
            public readonly string S;
            public Y(string s)
            {
                S = s;
            }
        }

        public struct Z
        {
            public static implicit operator X(Z z) => new X(z.S);
            public readonly string S;
            public Z(string s)
            {
                S = s;
            }
        }
    }
}

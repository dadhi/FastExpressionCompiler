using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FastExpressionCompiler.FlatExpression;

using static FastExpressionCompiler.LightExpression.Expression;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.LightExpression.UnitTests
{

    public class LightExpressionTests : ITest
    {
        public int Run()
        {
            Can_compile_lambda_without_converting_to_expression();
            Can_compile_lambda_with_property();
            Can_compile_lambda_with_call_and_property();
            Nested_Func_using_outer_parameter();
            Nested_Action_using_outer_parameter_and_closed_value();
            Can_compile_complex_expr_with_Arrays_and_Casts();
            Can_compile_complex_expr_with_perf_tricks_with_Arrays_and_Casts();
            Can_embed_normal_Expression_into_LightExpression_eg_as_Constructor_argument();
            Should_output_the_System_and_LightExpression_to_the_identical_construction_syntax();
            Should_output_the_System_and_LightExpression_to_the_identical_CSharp_syntax();
            Expression_produced_by_ToExpressionString_should_compile();
            Multiple_methods_in_block_should_be_aligned_when_output_to_csharp();
            Can_roundtrip_light_expression_through_flat_expression();
            Flat_expression_preserves_parameter_and_label_identity_and_collects_closure_constants();
            Can_convert_dynamic_runtime_variables_and_debug_info_to_light_expression_and_flat_expression();
            return 14;
        }


        public void Can_compile_lambda_without_converting_to_expression()
        {
            var funcExpr = Lambda(
                    New(typeof(X).GetTypeInfo().GetConstructors()[0],
                        New(typeof(Y).GetTypeInfo().GetConstructors()[0])));

            var func = funcExpr.CompileFast<Func<X>>(true);
            Asserts.IsNotNull(func);

            var x = func();
            Asserts.IsInstanceOf<X>(x);
        }

        public class Y { }
        public class X
        {
            public Y Y { get; }
            public X(Y y)
            {
                Y = y;
            }
        }


        public void Can_compile_lambda_with_property()
        {
            var thisType = GetType().GetTypeInfo();
            var funcExpr = Lambda(Property(thisType.GetProperty(nameof(PropX))));

            var func = funcExpr.CompileFast<Func<X>>(true);
            Asserts.IsNotNull(func);

            var x = func();
            Asserts.IsInstanceOf<X>(x);
        }


        public void Can_compile_lambda_with_call_and_property()
        {
            var thisType = GetType().GetTypeInfo();
            var funcExpr =
                Lambda(Call(thisType.GetMethod(nameof(GetX)),
                    Property(thisType.GetProperty(nameof(PropX)))));

            var func = funcExpr.CompileFast<Func<X>>(true);
            Asserts.IsNotNull(func);

            var x = func();
            Asserts.IsInstanceOf<X>(x);
        }

        public static X PropX => new X(new Y());
        public static X GetX(X x) => x;


        public void Nested_Func_using_outer_parameter()
        {
            // The same hoisted expression: 
            //Expression<Func<string, string>> expr = a => GetS(() => a);

            var aParam = Parameter(typeof(string), "a");
            var expr = Lambda(
                Call(GetType().GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(GetS)),
                    Lambda(aParam)),
                aParam);

            var f = expr.CompileFast<Func<string, string>>();

            Asserts.AreEqual("a", f("a"));
        }

        public static string GetS(Func<string> getS)
        {
            return getS();
        }


        public void Nested_Action_using_outer_parameter_and_closed_value()
        {
            //Expression<Func<Action<string>>> expr = () => a => s.SetValue(a);

            var s = new S();
            var aParam = Parameter(typeof(string), "a");
            var expr = Lambda(
                Lambda(
                    Call(
                        Constant(s),
                        typeof(S).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(S.SetValue)),
                        aParam),
                    aParam)
                );

            var f = expr.CompileFast<Func<Action<string>>>();

            f()("a");
            Asserts.AreEqual("a", s.Value);
        }

        public class S
        {
            public string Value;

            public void SetValue(string s)
            {
                Value = s;
            }
        }

        // Result delegate to be created by CreateComplexExpression
        public object CreateA(object[] state)
        {
            return new A(
                new B(),
                (string)state[11],
                new ID[] { new D1(), new D2() }
            )
            {
                Prop = new P(new B()),
                Bop = new B()
            };
        }

        private static ConstructorInfo _ctorOfA = typeof(A).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfB = typeof(B).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfP = typeof(P).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfD1 = typeof(D1).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfD2 = typeof(D2).GetTypeInfo().DeclaredConstructors.First();
        private static PropertyInfo _propAProp = typeof(A).GetTypeInfo().DeclaredProperties.First(p => p.Name == "Prop");
        private static FieldInfo _fieldABop = typeof(A).GetTypeInfo().DeclaredFields.First(p => p.Name == "Bop");

        public static System.Linq.Expressions.Expression<Func<object[], object>> CreateComplexExpression(string p = null)
        {
            var stateParamExpr = SysExpr.Parameter(typeof(object[]), p);

            var expr = SysExpr.Lambda<Func<object[], object>>(
                SysExpr.MemberInit(
                    SysExpr.New(_ctorOfA,
                        SysExpr.New(_ctorOfB),
                        SysExpr.Convert(SysExpr.ArrayIndex(stateParamExpr, SysExpr.Constant(11)), typeof(string)),
                        SysExpr.NewArrayInit(typeof(ID),
                            SysExpr.New(_ctorOfD1),
                            SysExpr.New(_ctorOfD2))),
                    SysExpr.Bind(_propAProp,
                        SysExpr.New(_ctorOfP,
                            SysExpr.New(_ctorOfB))),
                    SysExpr.Bind(_fieldABop,
                        SysExpr.New(_ctorOfB))),
                stateParamExpr);

            return expr;
        }

        public static Expression<Func<object[], object>> CreateComplexLightExpression(string p = null)
        {
            var stateParamExpr = ParameterOf<object[]>(p);

            var expr = Lambda<Func<object[], object>>(
                MemberInit(
                    New(_ctorOfA,
                        New(_ctorOfB),
                        Convert(
                            ArrayIndex(stateParamExpr, ConstantInt(11)),
                            typeof(string)),
                        NewArrayInit(typeof(ID),
                            New(_ctorOfD1),
                            New(_ctorOfD2))),
                    Bind(_propAProp,
                        New(_ctorOfP,
                            New(_ctorOfB))),
                    Bind(_fieldABop,
                        New(_ctorOfB))),
                stateParamExpr);

            return expr;
        }

        public static Expression<Func<object[], object>> CreateComplexLightExpression_with_intrinsics(string p = null)
        {
            var stateParamExpr = ParameterOf<object[]>(p);

            var expr = Lambda<Func<object[], object>>(
                MemberInit(
                    NewNoByRefArgs(_ctorOfA,
                        New(_ctorOfB),
                        TryConvertIntrinsic<string>(
                            ArrayIndex(stateParamExpr, ConstantInt(11))),
                        NewArrayInit(typeof(ID),
                            New(_ctorOfD1),
                            New(_ctorOfD2))),
                    Bind(_propAProp,
                        NewNoByRefArgs(_ctorOfP,
                            New(_ctorOfB))),
                    Bind(_fieldABop,
                        New(_ctorOfB))),
                stateParamExpr);

            return expr;
        }


        public void Can_compile_complex_expr_with_Arrays_and_Casts()
        {
            var expr = CreateComplexLightExpression();

            var func = expr.CompileFast(true);

            var input = new object[12];
            for (int i = 0; i < input.Length; i++)
                input[i] = i + "";
            var x = func(input);

            Asserts.AreEqual("11", ((A)x).Sop);
        }


        public void Can_compile_complex_expr_with_perf_tricks_with_Arrays_and_Casts()
        {
            var expr = CreateComplexLightExpression_with_intrinsics();

            var func = expr.CompileFast(true);

            var input = new object[12];
            for (int i = 0; i < input.Length; i++)
                input[i] = i + "";
            var x = func(input);

            Asserts.AreEqual("11", ((A)x).Sop);
        }


        public void Should_output_the_System_and_LightExpression_to_the_identical_construction_syntax()
        {
            var se = CreateComplexExpression("p");
            var le = CreateComplexLightExpression("p");

            var ses = se.FromSysExpression().ToExpressionString();
            var les = le.ToExpressionString();

            Asserts.Contains("MemberInit", ses);
            Asserts.AreEqual(ses, les);
        }


        public void Should_output_the_System_and_LightExpression_to_the_identical_CSharp_syntax()
        {
            var se = CreateComplexExpression("p");
            var le = CreateComplexLightExpression("p");

            var ses = se.FromSysExpression().ToCSharpString();
            var les = le.ToCSharpString();

            Asserts.Contains("Prop = new LightExpressionTests.P(new LightExpressionTests.B())", ses);
            Asserts.AreEqual(ses, les);
        }


        public void Expression_produced_by_ToExpressionString_should_compile()
        {
            var p = new ParameterExpression[1]; // the parameter expressions 
            var e = new Expression[12]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda( // $
            typeof(System.Func<object[], object>),
            e[0] = MemberInit(
                e[1] = New(/*3 args*/
                typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.A).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                e[2] = New(/*0 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.B).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]),
                e[3] = Convert(
                    e[4] = MakeBinary(ExpressionType.ArrayIndex,
                    p[0] = Parameter(typeof(object[])),
                    e[5] = Constant((int)11)),
                    typeof(string)),
                e[6] = NewArrayInit(
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.ID),
                    e[7] = New(/*0 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.D1).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]),
                    e[8] = New(/*0 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.D2).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))),
                Bind(
                typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.A).GetTypeInfo().GetDeclaredProperty("Prop"),
                e[9] = New(/*1 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.P).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                    e[10] = New(/*0 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.B).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))),
                Bind(
                typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.A).GetTypeInfo().GetDeclaredField("Bop"),
                e[11] = New(/*0 args*/
                    typeof(FastExpressionCompiler.LightExpression.UnitTests.LightExpressionTests.B).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))),
            p[0 // (object[] object_arr__38113962)
                ]);

            var f = (System.Func<object[], object>)expr.CompileFast();
            f(new object[22]);
        }


        public void Multiple_methods_in_block_should_be_aligned_when_output_to_csharp()
        {
            var sayHi = GetType().GetMethod(nameof(SayHi));
            var p = Parameter(typeof(int), "i");
            var e = Lambda<Action<int>>(Block(Call(sayHi, p, p), Call(sayHi, p, p), Call(sayHi, p, p)), p);

            var s = e.ToCSharpString();
            Asserts.Contains("SayHi", s);
        }

        public static void SayHi(int i, int j) { }

        public void Can_roundtrip_light_expression_through_flat_expression()
        {
            var expr = CreateComplexLightExpression("state");

            var flat = expr.ToFlatExpression();

            Asserts.IsTrue(flat.Nodes.Count > 0);
            Asserts.AreEqual(0, flat.ClosureConstants.Count);

            var roundtrip = (LambdaExpression)flat.ToLightExpression();
            var func = roundtrip.CompileFast<Func<object[], object>>(true);
            var a = (A)func(new object[12] { null, null, null, null, null, null, null, null, null, null, null, "flat" });

            Asserts.AreEqual("flat", a.Sop);
            Asserts.IsInstanceOf<P>(a.Prop);
            Asserts.AreEqual(2, a.Dop.Count());
        }

        public void Flat_expression_preserves_parameter_and_label_identity_and_collects_closure_constants()
        {
            var valueHolder = new S();
            var valueField = typeof(S).GetField(nameof(S.Value));
            var constExpr = Lambda<Func<string>>(Field(Constant(valueHolder), valueField));
            var constFlat = constExpr.ToFlatExpression();

            Asserts.AreEqual(1, constFlat.ClosureConstants.Count);
            Asserts.AreSame(valueHolder, constFlat.ClosureConstants[0]);
            Asserts.AreEqual(null, ((LambdaExpression)constFlat.ToLightExpression()).CompileFast<Func<string>>(true)());

            var p = SysExpr.Parameter(typeof(int), "p");
            var target = SysExpr.Label(typeof(int), "done");
            var sysLambda = SysExpr.Lambda<Func<int, int>>(
                SysExpr.Block(
                    SysExpr.Goto(target, p, typeof(int)),
                    SysExpr.Label(target, SysExpr.Constant(0))),
                p);

            var sysRoundtrip = (System.Linq.Expressions.LambdaExpression)sysLambda
                .ToFlatExpression()
                .ToExpression();

            var block = (System.Linq.Expressions.BlockExpression)sysRoundtrip.Body;
            var @goto = (System.Linq.Expressions.GotoExpression)block.Expressions[0];
            var label = (System.Linq.Expressions.LabelExpression)block.Expressions[1];

            Asserts.AreSame(sysRoundtrip.Parameters[0], @goto.Value);
            Asserts.AreSame(@goto.Target, label.Target);
        }

        public void Can_convert_dynamic_runtime_variables_and_debug_info_to_light_expression_and_flat_expression()
        {
            var runtimeParameter = SysExpr.Parameter(typeof(int), "runtime");
            var runtimeVariables = SysExpr.RuntimeVariables(runtimeParameter);
            var runtimeVariablesLight = runtimeVariables.ToLightExpression();
            var runtimeVariablesRoundtrip = runtimeVariablesLight.ToFlatExpression().ToLightExpression();

            Asserts.AreEqual(ExpressionType.RuntimeVariables, runtimeVariablesLight.NodeType);
            Asserts.AreEqual(ExpressionType.RuntimeVariables, runtimeVariablesRoundtrip.NodeType);

            var document = SysExpr.SymbolDocument("flat-expression.cs");
            var debugInfo = SysExpr.DebugInfo(document, 1, 1, 1, 10);
            var debugInfoLight = debugInfo.ToLightExpression();
            var debugInfoRoundtrip = debugInfoLight.ToFlatExpression().ToLightExpression();

            Asserts.AreEqual(ExpressionType.DebugInfo, debugInfoLight.NodeType);
            Asserts.AreEqual(ExpressionType.DebugInfo, debugInfoRoundtrip.NodeType);

            var dynamicArgument = SysExpr.Parameter(typeof(object), "arg");
            var binder = Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags.None, "Length", typeof(LightExpressionTests),
                new[] { Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.None, null) });
            var dynamicExpression = SysExpr.MakeDynamic(typeof(Func<CallSite, object, object>), binder, new[] { dynamicArgument });

            var dynamicLight = dynamicExpression.ToLightExpression();
            var dynamicRoundtrip = dynamicLight.ToFlatExpression().ToLightExpression();

            Asserts.AreEqual(ExpressionType.Dynamic, dynamicLight.NodeType);
            Asserts.AreEqual(ExpressionType.Dynamic, dynamicRoundtrip.NodeType);
            Asserts.AreEqual(ExpressionType.Dynamic, dynamicLight.ToFlatExpression().ToExpression().NodeType);
        }

        public class A
        {
            public P Prop { get; set; }
            public B Bop;
            public string Sop;
            public IEnumerable<ID> Dop;

            public A(B b, string s, IEnumerable<ID> ds)
            {
                Bop = b;
                Sop = s;
                Dop = ds;
            }
        }

        public class B { }

        public class P { public P(B b) { } }

        public interface ID { }
        public class D1 : ID { }
        public class D2 : ID { }


        public void Can_embed_normal_Expression_into_LightExpression_eg_as_Constructor_argument()
        {
            var func = Lambda(New(_ctorOfP, New(_ctorOfB))).CompileFast<Func<P>>();

            Asserts.IsInstanceOf<P>(func());
        }
    }
}

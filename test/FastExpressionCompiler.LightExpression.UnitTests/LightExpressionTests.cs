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

    public partial class LightExpressionTests : ITest
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
            Can_build_flat_expression_directly_with_light_expression_like_api();
            Can_build_flat_expression_control_flow_directly();
            Can_property_test_generated_flat_expression_roundtrip_structurally();
            Flat_lambda_parameter_ref_before_decl_preserves_identity();
            Flat_lambda_multiple_parameter_refs_all_yield_same_identity();
            Flat_block_variables_and_refs_yield_same_identity();
            Flat_nested_lambda_captures_outer_parameter_identity();
            Flat_out_of_order_decl_block_in_lambda_compiles_correctly();
            Flat_enum_constant_stored_inline_roundtrip();
            Flat_lambda_nodes_tracks_all_lambdas_during_direct_construction();
            Flat_lambda_nodes_tracks_deeply_nested_lambdas_during_direct_construction();
            Flat_lambda_nodes_tracks_lambdas_from_expression_conversion();
            Flat_lambda_nodes_has_single_entry_for_root_only_lambda();
            Flat_blocks_with_variables_tracked_during_direct_construction();
            Flat_goto_and_label_nodes_tracked_during_direct_construction();
            Flat_try_catch_nodes_tracked_during_direct_construction();
            Flat_blocks_with_variables_tracked_from_expression_conversion();
            Flat_goto_and_label_nodes_tracked_from_expression_conversion();
            Flat_try_catch_nodes_tracked_from_expression_conversion();
            return 33;
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

        public static ExprTree CreateComplexFlatExpression(string parameterName = null)
        {
            var fe = default(ExprTree);
            var stateParamExpr = fe.ParameterOf<object[]>(parameterName);
            var body = fe.MemberInit(
                fe.New(_ctorOfA,
                    fe.New(_ctorOfB),
                    fe.Convert(
                        fe.ArrayIndex(stateParamExpr, fe.ConstantInt(11)),
                        typeof(string)),
                    fe.NewArrayInit(typeof(ID),
                        fe.New(_ctorOfD1),
                        fe.New(_ctorOfD2))),
                fe.Bind(_propAProp,
                    fe.New(_ctorOfP,
                        fe.New(_ctorOfB))),
                fe.Bind(_fieldABop,
                    fe.New(_ctorOfB)));
            fe.RootIndex = fe.Lambda<Func<object[], object>>(body, stateParamExpr);
            return fe;
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
            var state = new object[12];
            state[11] = "flat";
            var a = (A)func(state);

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

        public void Can_build_flat_expression_directly_with_light_expression_like_api()
        {
            var fe = CreateComplexFlatExpression("state");
            var lambda = (LambdaExpression)fe.ToLightExpression();
            var func = lambda.CompileFast<Func<object[], object>>(true);
            var runtimeState = new object[12];
            runtimeState[11] = "direct";

            var a = (A)func(runtimeState);

            Asserts.AreEqual("direct", a.Sop);
            Asserts.IsInstanceOf<P>(a.Prop);
            Asserts.AreEqual(2, a.Dop.Count());
        }

        public void Can_build_flat_expression_control_flow_directly()
        {
            var fe = default(ExprTree);
            var p = fe.Parameter(typeof(int), "p");
            var target = fe.Label(typeof(int), "done");
            fe.RootIndex = fe.Lambda<Func<int, int>>(
                fe.Block(
                    fe.Goto(target, p, typeof(int)),
                    fe.Label(target, fe.ConstantInt(0))),
                p);

            var sysLambda = (System.Linq.Expressions.LambdaExpression)fe.ToExpression();
            var block = (System.Linq.Expressions.BlockExpression)sysLambda.Body;
            var gotoExpr = (System.Linq.Expressions.GotoExpression)block.Expressions[0];
            var label = (System.Linq.Expressions.LabelExpression)block.Expressions[1];

            Asserts.AreSame(sysLambda.Parameters[0], gotoExpr.Value);
            Asserts.AreSame(gotoExpr.Target, label.Target);
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

        // Tests for decl vs ref nodes and out-of-order decl in lambdas/blocks

        /// <summary>
        /// In the flat encoding, a lambda stores body first then parameters.
        /// So when reading, parameter refs in the body are encountered BEFORE
        /// the parameter decl node in the parameter list (out-of-order decl).
        /// Both should resolve to the exact same SysParameterExpression.
        /// </summary>
        public void Flat_lambda_parameter_ref_before_decl_preserves_identity()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            // body uses p: ref nodes come first when the lambda is encoded/read
            fe.RootIndex = fe.Lambda<Func<int, int>>(fe.Add(p, fe.ConstantInt(1)), p);

            var sysLambda = (System.Linq.Expressions.LambdaExpression)fe.ToExpression();
            var add = (System.Linq.Expressions.BinaryExpression)sysLambda.Body;

            // The parameter in the params list and its ref in the body must be the same object
            Asserts.AreSame(sysLambda.Parameters[0], add.Left);
        }

        /// <summary>
        /// A parameter referenced more than once in a lambda body (all refs are
        /// out-of-order relative to the single decl at the end of the child list)
        /// must all resolve to the same SysParameterExpression.
        /// </summary>
        public void Flat_lambda_multiple_parameter_refs_all_yield_same_identity()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            // p * p + p: three independent refs to the same parameter
            fe.RootIndex = fe.Lambda<Func<int, int>>(
                fe.Add(fe.MakeBinary(System.Linq.Expressions.ExpressionType.Multiply, p, p), p),
                p);

            var sysLambda = (System.Linq.Expressions.LambdaExpression)fe.ToExpression();
            var add = (System.Linq.Expressions.BinaryExpression)sysLambda.Body;
            var mul = (System.Linq.Expressions.BinaryExpression)add.Left;
            var paramDecl = sysLambda.Parameters[0];

            Asserts.AreSame(paramDecl, mul.Left);
            Asserts.AreSame(paramDecl, mul.Right);
            Asserts.AreSame(paramDecl, add.Right);
        }

        /// <summary>
        /// Block variables are read before body expressions (normal order),
        /// but each variable index is cloned whenever it appears as a child.
        /// All clones must resolve to the same SysParameterExpression.
        /// </summary>
        public void Flat_block_variables_and_refs_yield_same_identity()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            var v1 = fe.Variable(typeof(int), "v1");
            var v2 = fe.Variable(typeof(int), "v2");
            // { int v1, v2; v1 = p; v2 = v1 + 1; v2 }
            var block = fe.Block(typeof(int),
                new[] { v1, v2 },
                fe.Assign(v1, p),
                fe.Assign(v2, fe.Add(v1, fe.ConstantInt(1))),
                v2);
            fe.RootIndex = fe.Lambda<Func<int, int>>(block, p);

            var sysLambda = (System.Linq.Expressions.LambdaExpression)fe.ToExpression();
            var sysBlock = (System.Linq.Expressions.BlockExpression)sysLambda.Body;
            var assign1 = (System.Linq.Expressions.BinaryExpression)sysBlock.Expressions[0]; // v1 = p
            var assign2 = (System.Linq.Expressions.BinaryExpression)sysBlock.Expressions[1]; // v2 = v1 + 1
            var addExpr = (System.Linq.Expressions.BinaryExpression)assign2.Right;            // v1 + 1

            // v1 decl and its ref on the left of assign1 are the same object
            Asserts.AreSame(sysBlock.Variables[0], assign1.Left);
            // v1 decl and its ref inside the add expression are the same object
            Asserts.AreSame(sysBlock.Variables[0], addExpr.Left);
            // v2 decl and its ref on the left of assign2 are the same object
            Asserts.AreSame(sysBlock.Variables[1], assign2.Left);
            // v2 decl and the final block result expression are the same object
            Asserts.AreSame(sysBlock.Variables[1], sysBlock.Expressions[2]);
        }

        /// <summary>
        /// An outer lambda parameter captured in a nested lambda body creates
        /// a ref node in the nested lambda scope. All three occurrences —
        /// the outer params list, the inner body, and any outer body usage —
        /// must resolve to the exact same SysParameterExpression.
        /// </summary>
        public void Flat_nested_lambda_captures_outer_parameter_identity()
        {
            var fe = default(ExprTree);
            var x = fe.ParameterOf<int>("x");
            // outer: x => () => x  (inner lambda closes over outer param)
            var inner = fe.Lambda<Func<int>>(x);
            fe.RootIndex = fe.Lambda<Func<int, Func<int>>>(inner, x);

            var sysOuter = (System.Linq.Expressions.LambdaExpression)fe.ToExpression();
            var sysInner = (System.Linq.Expressions.LambdaExpression)sysOuter.Body;

            // The inner lambda body (the x ref) must be the same object as the outer param decl
            Asserts.AreSame(sysOuter.Parameters[0], sysInner.Body);
        }

        /// <summary>
        /// End-to-end compile-and-run test with a block containing two variables,
        /// verifying that out-of-order parameter decls and variable refs produce
        /// a correctly executing delegate.
        /// </summary>
        public void Flat_out_of_order_decl_block_in_lambda_compiles_correctly()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            var v1 = fe.Variable(typeof(int), "v1");
            var v2 = fe.Variable(typeof(int), "v2");
            // (int p) => { int v1 = p * 2; int v2 = v1 + p; v2 }
            var block = fe.Block(typeof(int),
                new[] { v1, v2 },
                fe.Assign(v1, fe.MakeBinary(System.Linq.Expressions.ExpressionType.Multiply, p, fe.ConstantInt(2))),
                fe.Assign(v2, fe.Add(v1, p)),
                v2);
            fe.RootIndex = fe.Lambda<Func<int, int>>(block, p);

            var func = (Func<int, int>)((System.Linq.Expressions.LambdaExpression)fe.ToExpression()).Compile();
            // p=3 → v1 = 3*2=6, v2 = 6+3=9
            Asserts.AreEqual(9, func(3));
            // p=0 → v1 = 0, v2 = 0
            Asserts.AreEqual(0, func(0));
        }

        enum ByteEnum : byte { A = 1, B = 200 }
        enum SByteEnum : sbyte { A = -1, B = 50 }
        enum ShortEnum : short { A = -1000, B = 30000 }
        enum UShortEnum : ushort { A = 0, B = 60000 }
        enum IntEnum : int { A = int.MinValue, B = 42 }
        enum UIntEnum : uint { A = 0, B = uint.MaxValue }

        public void Flat_enum_constant_stored_inline_roundtrip()
        {
            // Verify that enum constants with ≤32-bit underlying types are stored inline
            // (no ClosureConstants entry, no boxing) and round-trip correctly.
            void Check<TEnum>(TEnum enumValue) where TEnum : Enum
            {
                var fe = default(ExprTree);
                var idx = fe.Constant(enumValue, typeof(TEnum));
                Asserts.AreEqual(0, fe.ClosureConstants.Count,
                    $"{typeof(TEnum).Name}.{enumValue} should be inline (no ClosureConstants), but got {fe.ClosureConstants.Count}");
                fe.RootIndex = fe.Lambda<Func<TEnum>>(idx);
                var result = (TEnum)((System.Linq.Expressions.LambdaExpression)fe.ToExpression()).Compile().DynamicInvoke()!;
                Asserts.AreEqual(enumValue, result, $"Round-trip failed for {typeof(TEnum).Name}.{enumValue}");
            }

            Check(ByteEnum.A);
            Check(ByteEnum.B);
            Check(SByteEnum.A);
            Check(SByteEnum.B);
            Check(ShortEnum.A);
            Check(ShortEnum.B);
            Check(UShortEnum.A);
            Check(UShortEnum.B);
            Check(IntEnum.A);
            Check(IntEnum.B);
            Check(UIntEnum.A);
            Check(UIntEnum.B);
        }

        /// <summary>
        /// When building a flat expression directly, calling Lambda() for a nested lambda
        /// and then for the root lambda should result in both indices recorded in LambdaNodes.
        /// The root is identified by RootIndex; all others are nested.
        /// </summary>
        public void Flat_lambda_nodes_tracks_all_lambdas_during_direct_construction()
        {
            var fe = default(ExprTree);
            var x = fe.ParameterOf<int>("x");

            // Build: outer: x => () => x
            var inner = fe.Lambda<Func<int>>(x);
            fe.RootIndex = fe.Lambda<Func<int, Func<int>>>(inner, x);

            // Both the root and nested lambda indices should be recorded
            Asserts.AreEqual(2, fe.LambdaNodes.Count);

            // Check that inner and root are both in LambdaNodes
            var foundInner = false;
            var foundRoot = false;
            for (var i = 0; i < fe.LambdaNodes.Count; i++)
            {
                if (fe.LambdaNodes[i] == inner) foundInner = true;
                if (fe.LambdaNodes[i] == fe.RootIndex) foundRoot = true;
            }
            Asserts.IsTrue(foundInner);
            Asserts.IsTrue(foundRoot);

            // Nested lambdas are all LambdaNodes entries that are not the root
            var nestedCount = 0;
            for (var i = 0; i < fe.LambdaNodes.Count; i++)
                if (fe.LambdaNodes[i] != fe.RootIndex)
                    ++nestedCount;
            Asserts.AreEqual(1, nestedCount);
        }

        /// <summary>
        /// When building a flat expression with multiple levels of nesting,
        /// all lambda node indices are captured in LambdaNodes.
        /// </summary>
        public void Flat_lambda_nodes_tracks_deeply_nested_lambdas_during_direct_construction()
        {
            var fe = default(ExprTree);
            var x = fe.ParameterOf<int>("x");

            // Build: outer: x => (() => (() => x))
            var innermost = fe.Lambda<Func<int>>(x);
            var middle = fe.Lambda<Func<Func<int>>>(innermost);
            fe.RootIndex = fe.Lambda<Func<int, Func<Func<int>>>>(middle, x);

            // All three lambda nodes should be recorded
            Asserts.AreEqual(3, fe.LambdaNodes.Count);

            // Count nested (non-root) lambdas
            var nestedCount = 0;
            for (var i = 0; i < fe.LambdaNodes.Count; i++)
                if (fe.LambdaNodes[i] != fe.RootIndex)
                    ++nestedCount;
            Asserts.AreEqual(2, nestedCount);
        }

        /// <summary>
        /// When converting a System.Linq expression tree with nested lambdas via FromExpression,
        /// the resulting ExprTree should have all lambda indices populated in LambdaNodes.
        /// </summary>
        public void Flat_lambda_nodes_tracks_lambdas_from_expression_conversion()
        {
            var p = SysExpr.Parameter(typeof(int), "p");
            // Build: p => () => p  using System.Linq.Expressions
            var sysLambda = SysExpr.Lambda<Func<int, Func<int>>>(
                SysExpr.Lambda<Func<int>>(p),
                p);

            var fe = sysLambda.ToFlatExpression();

            // Both root and nested lambda indices should be recorded
            Asserts.AreEqual(2, fe.LambdaNodes.Count);

            // The root lambda must be in the list
            var foundRoot = false;
            for (var i = 0; i < fe.LambdaNodes.Count; i++)
                if (fe.LambdaNodes[i] == fe.RootIndex) { foundRoot = true; break; }
            Asserts.IsTrue(foundRoot);

            // Exactly one nested lambda
            var nestedCount = 0;
            for (var i = 0; i < fe.LambdaNodes.Count; i++)
                if (fe.LambdaNodes[i] != fe.RootIndex)
                    ++nestedCount;
            Asserts.AreEqual(1, nestedCount);
        }

        /// <summary>
        /// A flat expression with no nested lambdas (root-only) should have exactly one
        /// entry in LambdaNodes (the root itself).
        /// </summary>
        public void Flat_lambda_nodes_has_single_entry_for_root_only_lambda()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            fe.RootIndex = fe.Lambda<Func<int, int>>(fe.Add(p, fe.ConstantInt(1)), p);

            Asserts.AreEqual(1, fe.LambdaNodes.Count);
            Asserts.AreEqual(fe.RootIndex, fe.LambdaNodes[0]);
        }

        /// <summary>
        /// Block nodes with explicit variable declarations are recorded in BlocksWithVariables;
        /// blocks without variables produce no entry.
        /// </summary>
        public void Flat_blocks_with_variables_tracked_during_direct_construction()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            var v = fe.Variable(typeof(int), "v");

            // Block with one variable: should be tracked
            var blockWithVar = fe.Block(typeof(int), new[] { v }, fe.Assign(v, p), v);
            // Block without variables: should NOT be tracked
            var blockNoVar = fe.Block(fe.Add(p, fe.ConstantInt(1)));

            fe.RootIndex = fe.Lambda<Func<int, int>>(fe.Block(blockWithVar, blockNoVar), p);

            Asserts.AreEqual(1, fe.BlocksWithVariables.Count);
            Asserts.AreEqual(blockWithVar, fe.BlocksWithVariables[0]);
        }

        /// <summary>
        /// Goto and label expression nodes are recorded in GotoNodes and LabelNodes respectively.
        /// </summary>
        public void Flat_goto_and_label_nodes_tracked_during_direct_construction()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");
            var target = fe.Label(typeof(int), "done");

            var gotoNode = fe.Goto(target, p, typeof(int));
            var labelNode = fe.Label(target, fe.ConstantInt(0));

            fe.RootIndex = fe.Lambda<Func<int, int>>(fe.Block(gotoNode, labelNode), p);

            Asserts.AreEqual(1, fe.GotoNodes.Count);
            Asserts.AreEqual(gotoNode, fe.GotoNodes[0]);

            Asserts.AreEqual(1, fe.LabelNodes.Count);
            Asserts.AreEqual(labelNode, fe.LabelNodes[0]);
        }

        /// <summary>
        /// Try/catch, try/finally and try/fault node indices are all recorded in TryCatchNodes.
        /// </summary>
        public void Flat_try_catch_nodes_tracked_during_direct_construction()
        {
            var fe = default(ExprTree);
            var p = fe.ParameterOf<int>("p");

            var tryCatchNode = fe.TryCatch(
                fe.Add(p, fe.ConstantInt(1)),
                fe.Catch(typeof(Exception), fe.ConstantInt(-1)));

            var tryFinallyNode = fe.TryFinally(
                fe.Add(p, fe.ConstantInt(2)),
                fe.Default(typeof(void)));

            fe.RootIndex = fe.Lambda<Func<int, int>>(
                fe.Block(tryCatchNode, tryFinallyNode), p);

            Asserts.AreEqual(2, fe.TryCatchNodes.Count);

            var foundTryCatch = false;
            var foundTryFinally = false;
            for (var i = 0; i < fe.TryCatchNodes.Count; i++)
            {
                if (fe.TryCatchNodes[i] == tryCatchNode) foundTryCatch = true;
                if (fe.TryCatchNodes[i] == tryFinallyNode) foundTryFinally = true;
            }
            Asserts.IsTrue(foundTryCatch);
            Asserts.IsTrue(foundTryFinally);
        }

        /// <summary>
        /// When converting a System.Linq expression tree, blocks with variables are
        /// recorded in BlocksWithVariables; plain blocks are not.
        /// </summary>
        public void Flat_blocks_with_variables_tracked_from_expression_conversion()
        {
            var p = SysExpr.Parameter(typeof(int), "p");
            var v = SysExpr.Variable(typeof(int), "v");
            // block with variable
            var sysBlock = SysExpr.Block(new[] { v }, SysExpr.Assign(v, p), v);
            var sysLambda = SysExpr.Lambda<Func<int, int>>(sysBlock, p);

            var fe = sysLambda.ToFlatExpression();

            Asserts.AreEqual(1, fe.BlocksWithVariables.Count);
        }

        /// <summary>
        /// When converting a System.Linq expression tree with goto/label, both
        /// GotoNodes and LabelNodes are populated.
        /// </summary>
        public void Flat_goto_and_label_nodes_tracked_from_expression_conversion()
        {
            var p = SysExpr.Parameter(typeof(int), "p");
            var target = SysExpr.Label(typeof(int), "done");
            var sysLambda = SysExpr.Lambda<Func<int, int>>(
                SysExpr.Block(
                    SysExpr.Goto(target, p, typeof(int)),
                    SysExpr.Label(target, SysExpr.Constant(0))),
                p);

            var fe = sysLambda.ToFlatExpression();

            Asserts.AreEqual(1, fe.GotoNodes.Count);
            Asserts.AreEqual(1, fe.LabelNodes.Count);
        }

        /// <summary>
        /// When converting a System.Linq expression tree with a try/catch,
        /// TryCatchNodes is populated.
        /// </summary>
        public void Flat_try_catch_nodes_tracked_from_expression_conversion()
        {
            var p = SysExpr.Parameter(typeof(int), "p");
            var sysLambda = SysExpr.Lambda<Func<int, int>>(
                SysExpr.TryCatch(
                    SysExpr.Add(p, SysExpr.Constant(1)),
                    SysExpr.Catch(typeof(Exception), SysExpr.Constant(-1))),
                p);

            var fe = sysLambda.ToFlatExpression();

            Asserts.AreEqual(1, fe.TryCatchNodes.Count);
        }
    }
}

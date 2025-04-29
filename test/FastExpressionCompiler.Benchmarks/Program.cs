﻿using BenchmarkDotNet.Running;
using FastExpressionCompiler.LightExpression;

namespace FastExpressionCompiler.Benchmarks;

public class Program
{
    public static void Main()
    {
        // todo: README.md Benchmarks
        // --------------------------------------------
        // BenchmarkRunner.Run<HoistedLambdaBenchmark.Compilation>();
        // BenchmarkRunner.Run<HoistedLambdaBenchmark.Invocation>();

        // BenchmarkRunner.Run<HoistedLambdaWithNestedLambdaBenchmark.Compilation>();
        // BenchmarkRunner.Run<HoistedLambdaWithNestedLambdaBenchmark.Invocation>();

        // BenchmarkRunner.Run<ManuallyComposedLambdaBenchmark.Compilation>();
        // BenchmarkRunner.Run<ManuallyComposedLambdaBenchmark.Invocation>();
        // BenchmarkRunner.Run<ManuallyComposedLambdaBenchmark.Create>(); // not included in README.md, may be it needs to
        // BenchmarkRunner.Run<ManuallyComposedLambdaBenchmark.Create_and_Compile>(); // not included in README.md, may be it needs to

        // BenchmarkRunner.Run<LightExprVsExpr_Create_ComplexExpr>();
        // BenchmarkRunner.Run<LightExprVsExpr_CreateAndCompile_ComplexExpr>();

        //--------------------------------------------

        BenchmarkRunner.Run<Issue468_Compile_vs_FastCompile>();

        // BenchmarkRunner.Run<AccessByRef_vs_ByIGetRefStructImpl>();

        // BenchmarkRunner.Run<ApexSerialization_SerializeDictionary.Compile>();

        // BenchmarkRunner.Run<EmitHacks.MethodStaticNoArgsEmit>();

        // BenchmarkRunner.Run<ExprLinqAnyOfNotNullDecimal.Compile>();
        // BenchmarkRunner.Run<ExprLinqAnyOfNotNullDecimal.Invoke>();

        // BenchmarkRunner.Run<RepoDb_ListInit.Compile>();

        // BenchmarkRunner.Run<ReflectionInvoke_vs_CallWithObjectArgsAndNestedLambda>();

        //BenchmarkRunner.Run<GenericConverter.Compilation>();
        //BenchmarkRunner.Run<GenericConverter.Invocation>();

        //BenchmarkRunner.Run<ClosureConstantsBenchmark.Compilation>();
        //BenchmarkRunner.Run<ClosureConstantsBenchmark.Invocation>();

        // BenchmarkRunner.Run<ArrayCopy_vs_ArrayResize_vs_ForLoop>();

        //var a = new NestedLambdasVsVars();
        //a.Init();
        //a.LightExpression_with_sub_expressions_CompiledFast();
        //a.Expression_with_sub_expressions_CompiledFast();
        //a.Expression_with_sub_expressions_Compiled();

        // BenchmarkRunner.Run<NestedLambdasVsVars>();

        // BenchmarkRunner.Run<AutoMapper_Map_Dictionary_of_Obj_with_One_prop.Compile_only>();
        // BenchmarkRunner.Run<AutoMapper_Map_Dictionary_of_Obj_with_One_prop.Invoke_compiled_delegate>();

        // BenchmarkRunner.Run<AutoMapper_UseCase_Simplified_OneProperty.Compile_only>();
        // BenchmarkRunner.Run<AutoMapper_UseCase_Simplified_OneProperty.Create_and_Compile>();
        // BenchmarkRunner.Run<AutoMapper_UseCase_Simplified_OneProperty.Invoke_compiled_delegate>();

        //BenchmarkRunner.Run<NestedLambdaOverhead>();

        //BenchmarkRunner.Run<FEC_vs_ManualEmit_vs_Activator>();

        // BenchmarkRunner.Run<MultipleNestedLambdaExprVsExprSharing>();

        //BenchmarkRunner.Run<ObjectExecutor_SyncMethod_Compile>();
        //BenchmarkRunner.Run<ObjectExecutor_SyncMethod_Execute>();

        //BenchmarkRunner.Run<ObjectExecutor_AsyncMethod_CreateExecutor>();
        //BenchmarkRunner.Run<ObjectExecutor_AsyncMethod_ExecuteAsync>();
        //BenchmarkRunner.Run<ObjectExecutor_AsyncMethod_Execute>();
        //BenchmarkRunner.Run<StaticTypeOfSwitch>();

        //BenchmarkRunner.Run<ExprInfoVsExpr_TryCatchExpr.Compilation>();
        //BenchmarkRunner.Run<ExprInfoVsExpr_TryCatchExpr.Invocation>();

        //BenchmarkRunner.Run<SimpleExpr_ParamPlusParam>();

        // BenchmarkRunner.Run<Deserialize_Simple>();

        // BenchmarkRunner.Run<LightExprVsExpr_CreateAndCompile_NestedLambdaExpr>();
        // BenchmarkRunner.Run<LightExprVsExpr_CreateAndCompile_SimpleExpr>();

        // BenchmarkRunner.Run<HoistedLambdaBenchmark_LogicalOps.Invoke>();
        // BenchmarkRunner.Run<HoistedLambdaBenchmark_LogicalOps.Compile>();
        // BenchmarkRunner.Run<HoistedLambdaBenchmark.Invoke>();
    }
}

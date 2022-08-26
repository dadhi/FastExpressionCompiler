using System;
using System.Reflection;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;
using FastExpressionCompiler.IssueTests;

namespace FastExpressionCompiler.Benchmarks
{
    /*
    ## Initial result - 4000x slower

    |                    Method |         Mean |        Error |       StdDev | Ratio |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
    |-------------------------- |-------------:|-------------:|-------------:|------:|-------:|-------:|------:|----------:|
    | DynamicMethod_Emit_Newobj | 51,156.76 ns | 1,014.771 ns | 2,095.677 ns | 1.000 | 0.3662 | 0.1831 |     - |    1215 B |
    |  Activator_CreateInstance |     13.20 ns |     0.338 ns |     0.506 ns | 0.000 | 0.0076 |      - |     - |      24 B |
    */

    [MemoryDiagnoser(displayGenColumns: false)]
    public class SimpleConstructorEmit
    {
        [Benchmark(Baseline = true)]
        public EmitSimpleConstructorTest.A DynamicMethod_Emit()
        {
            var f = EmitSimpleConstructorTest.Get_DynamicMethod_Emit_Newobj();
            return f();
        }

        [Benchmark]
        public EmitSimpleConstructorTest.A Activator_CreateInstance() =>
            (EmitSimpleConstructorTest.A)Activator.CreateInstance(typeof(EmitSimpleConstructorTest.A));

        // public class A 
        // {
        //     public A() {}
        // }

        // private static readonly ConstructorInfo _aCtor = typeof(A).GetConstructor(Type.EmptyTypes);
    }

    [MemoryDiagnoser]
    public class MethodStaticNoArgsEmit
    {
        [Benchmark(Baseline = true)]
        public int DynamicMethod_Emit_OpCodes_Call()
        {
            var f = EmitSimpleConstructorTest.Get_DynamicMethod_Emit_OpCodes_Call();
            return f();
        }

        [Benchmark]
        public int MethodInfo_Invoke() =>
            (int)EmitSimpleConstructorTest.MethodStaticNoArgs.Invoke(null, null);
    }
}
using System;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    public class StaticTypeOfSwitch
    {
        private static OpCode OpCodeForTypeByTypeOf(Type type)
        {
            if (type == typeof(int))
                return OpCodes.Stind_I4;
            else if (type == typeof(byte))
                return OpCodes.Stind_I1;
            else if (type == typeof(short))
                return OpCodes.Stind_I2;
            else if (type == typeof(long))
                return OpCodes.Stind_I8;
            else if (type == typeof(float))
                return OpCodes.Stind_R4;
            else
                throw new NotImplementedException();
        }

        private readonly static Type intT = typeof(int);
        private readonly static Type byteT = typeof(byte);
        private readonly static Type shortT = typeof(short);
        private readonly static Type longT = typeof(long);
        private readonly static Type floatT = typeof(float);

        private static OpCode OpCodeForTypeByStatitcType(Type type)
        {
            if (type == intT)
                return OpCodes.Stind_I4;
            else if (type == byteT)
                return OpCodes.Stind_I1;
            else if (type == shortT)
                return OpCodes.Stind_I2;
            else if (type == longT)
                return OpCodes.Stind_I8;
            else if (type == floatT)
                return OpCodes.Stind_R4;
            else
                throw new NotImplementedException();
        }

        [Benchmark(Baseline = true)]
        public object OpCodeForTypeByTypeOfTest() => OpCodeForTypeByTypeOf(floatT);

        [Benchmark]
        public object OpCodeForTypeByStatitcTypeTest() => OpCodeForTypeByStatitcType(floatT);
    }
}

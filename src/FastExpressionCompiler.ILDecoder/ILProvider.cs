using System;
using System.Reflection;

namespace FastExpressionCompiler.ILDecoder
{
    public interface IILProvider
    {
        byte[] GetByteArray();
    }

    public class MethodBaseILProvider : IILProvider
    {
        private static readonly Type s_runtimeMethodInfoType = Type.GetType("System.Reflection.RuntimeMethodInfo");
        private static readonly Type s_runtimeConstructorInfoType = Type.GetType("System.Reflection.RuntimeConstructorInfo");

        private readonly MethodBase m_method;
        private byte[] m_byteArray;

        public MethodBaseILProvider(MethodBase method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            var methodType = method.GetType();

            if (methodType != s_runtimeMethodInfoType && methodType != s_runtimeConstructorInfoType)
                throw new ArgumentException("Must have type RuntimeMethodInfo or RuntimeConstructorInfo.", nameof(method));

            m_method = method;
        }

        public byte[] GetByteArray()
        {
            return m_byteArray ?? (m_byteArray = m_method.GetMethodBody()?.GetILAsByteArray() ?? new byte[0]);
        }
    }
}
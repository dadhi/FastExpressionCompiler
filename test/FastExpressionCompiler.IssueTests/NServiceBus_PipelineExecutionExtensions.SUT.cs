using System;
using System.Reflection;

namespace NServiceBus.Pipeline
{
    internal interface IBehavior { }

    internal interface IBehaviorContext { }

    internal class BehaviorMethods
    {
        public MethodInfo[] TargetMethods => new MethodInfo[0];
    }

    internal interface IPipelineTerminator { }

    internal class PipelineTerminator<T> : IPipelineTerminator
    {
        internal interface ITerminatingContext { }
    }

    internal static class Ext
    {
        internal static Type GetBehaviorInterface(this Type type) => typeof(IBehavior);

        internal static BehaviorMethods GetInterfaceMap(this Type type, Type type2) => new BehaviorMethods();
    }
}
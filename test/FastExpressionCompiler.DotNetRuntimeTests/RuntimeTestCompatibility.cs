using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace System
{
    internal static class PlatformDetection
    {
        public static bool IsCoreCLR => true;
        public static bool IsWasm => false;
        public static bool IsNotMonoRuntime => true;
        public static bool IsNotNativeAot => true;
        public static bool IsReflectionEmitSupported => true;
        public static bool IsMultithreadingSupported => true;
        public static bool IsNonZeroLowerBoundArraySupported => true;
        public static bool IsRareEnumsSupported => true;
        public static bool IsNotBuiltWithAggressiveTrimming => true;
        public static bool IsLinqExpressionsBuiltWithIsInterpretingOnly => false;
        public static bool IsNotLinqExpressionsBuiltWithIsInterpretingOnly => true;
    }
    // The only AssertExtensions APIs used by this imported suite.
    internal static class AssertExtensions
    {
        public static T Throws<T>(string name, Func<object> action) where T : ArgumentException => Throws<T>(name, () => { action(); });
        public static T Throws<T>(string name, Action action) where T : ArgumentException
        { var ex = Assert.Throws<T>(action); Assert.Equal(name, ex.ParamName); return ex; }
        public static T Throws<T>(string coreName, string frameworkName, Action action) where T : ArgumentException => Throws<T>(coreName, action);
        public static void ThrowsOnAot<T>(Action action) where T : Exception => action();
    }
}
namespace Xunit
{
    internal static class RuntimeConditions
    {
        public static string SkipReason(Type type, string[] names)
        {
            foreach (var name in names)
            {
                var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                object value = property != null ? property.GetValue(null) : field != null ? field.GetValue(null) : method?.Invoke(null, null);
                if (value is not bool supported) throw new InvalidOperationException("Unknown runtime condition: " + type + "." + name);
                if (!supported) return "Upstream condition is false: " + type.Name + "." + name;
            }
            return null;
        }
    }
    public sealed class ConditionalFactAttribute : FactAttribute
    { public ConditionalFactAttribute() { } public ConditionalFactAttribute(Type type, params string[] names) { Skip = RuntimeConditions.SkipReason(type, names); } }
    // Only mark compiler-layout tests after their expressions have semantic coverage under strict FEC.
    public sealed class VerifiedCompilerLayoutFactAttribute : FactAttribute
    {
        public VerifiedCompilerLayoutFactAttribute(string behaviorCoverage)
        {
            if (Environment.GetEnvironmentVariable("FEC_TEST_COMPILER") != "system")
                Skip = "Asserts the system compiler's exact IL layout; strict FEC behavior verified by " + behaviorCoverage;
        }
    }
    public sealed class ConditionalTheoryAttribute : TheoryAttribute
    { public ConditionalTheoryAttribute() { } public ConditionalTheoryAttribute(Type type, params string[] names) { Skip = RuntimeConditions.SkipReason(type, names); } }
    // Metadata applicable to other runtimes/platforms; preserved for provenance.
    [Flags] public enum TestRuntimes { Mono = 1 }
    [Flags] public enum TestPlatforms { tvOS = 1, MacCatalyst = 2, iOS = 4 }
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class ActiveIssueAttribute : Attribute
    {
        public ActiveIssueAttribute(string issue) { }
        public ActiveIssueAttribute(string issue, TestRuntimes runtimes) { }
        public ActiveIssueAttribute(string issue, Type type, params string[] names) { }
        public ActiveIssueAttribute(string issue, TestPlatforms platforms = 0, TestRuntimes runtimes = 0) { }
    }
    [AttributeUsage(AttributeTargets.All)] public sealed class OuterLoopAttribute : Attribute
    { public OuterLoopAttribute(string reason) { } }
}
namespace Microsoft.DotNet.RemoteExecutor
{
    // Explicitly unavailable; the ConditionalFact remains discoverable and skipped.
    public static class RemoteExecutor
    {
        public static bool IsSupported => false;
        public static RemoteInvokeHandle Invoke(Action action, RemoteInvokeOptions options) => throw new NotSupportedException("dotnet/runtime remote executor is not part of this standalone port.");
    }
    public sealed class RemoteInvokeOptions
    { public Dictionary<string, string> RuntimeConfigurationOptions { get; } = new(); }
    public sealed class RemoteInvokeHandle : IDisposable { public void Dispose() { } }
}

namespace Microsoft.DotNet.XUnitExtensions
{
    // xUnit v2 cannot dynamically skip using the runtime's custom exception.
    // Missing private debugger metadata is reported as a baseline failure.
    public sealed class SkipTestException : System.Exception
    { public SkipTestException(string message) : base(message) { } }
}
namespace System
{
    internal static partial class PortAssertExtensionsExtra { }
}
namespace System.Linq.Expressions.Tests
{
    internal static class RuntimePrivateAccess
    {
        internal sealed class ClosureData { public object[] Constants; }
        internal static ClosureData ReadClosure(object target)
        {
            if (target?.GetType().FullName != "System.Runtime.CompilerServices.Closure") return null;
            return new ClosureData { Constants = (object[])target.GetType().GetField("Constants").GetValue(target) };
        }
        private static Type ArgumentProviderType(object expression) => expression.GetType().GetInterfaces().FirstOrDefault(t => t.Name == "IArgumentProvider") ?? expression.GetType();
        public static int GetArgumentCountForPort(this object expression) => (int)ArgumentProviderType(expression).GetProperty("ArgumentCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(expression);
        public static Expression GetArgument(this object expression, int index)
        {
            try { return (Expression)ArgumentProviderType(expression).GetMethod("GetArgument", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(expression, new object[] { index }); }
            catch (TargetInvocationException ex) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
        }
    }
}

namespace System.Linq.Expressions.Tests
{
    public static class RuntimeCallSiteOps
    {
        private static Type Ops => typeof(Expression).Assembly.GetType("System.Runtime.CompilerServices.CallSiteOps", true);
        public static T[] GetRules<T>(System.Runtime.CompilerServices.CallSite<T> site) where T : class =>
            (T[])Ops.GetMethod("GetRules").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { site });
        public static object GetRuleCache<T>(System.Runtime.CompilerServices.CallSite<T> site) where T : class =>
            Ops.GetMethod("GetRuleCache").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { site });
        public static Array GetCachedRules(object cache) =>
            (Array)Ops.GetMethod("GetCachedRules").MakeGenericMethod(cache.GetType().GetGenericArguments()).Invoke(null, new[] { cache });
    }
}

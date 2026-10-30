using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

#if GRANT_PRIVATE_ACCESS && !GENERATED_ACCESS_ATTRIBUTE
[assembly: IgnoresAccessChecksTo("System.Private.CoreLib")]
#endif

var actual = typeof(object).Assembly.Location;
var expected = Environment.GetEnvironmentVariable("EXPECTED_CORELIB");
if (expected != null && !string.Equals(Path.GetFullPath(actual), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase))
    throw new Exception("Consumer executed a different runtime than the selected reference: " + actual);
if (!actual.StartsWith(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), StringComparison.OrdinalIgnoreCase))
    throw new Exception("Unexpected runtime CoreLib: " + actual);
Console.WriteLine("Executing installed CoreLib: " + actual);
var method = new DynamicMethod("direct_private_access", typeof(int), Type.EmptyTypes, typeof(Program), true);
var il = (RuntimeILGenerator)method.GetILGenerator(16);
Console.WriteLine($"Original visibility: stream={typeof(RuntimeILGenerator).GetField("m_ILStream", BindingFlags.Instance | BindingFlags.NonPublic)!.Attributes}; increase={typeof(RuntimeILGenerator).GetMethod("IncreaseCapacity", BindingFlags.Instance | BindingFlags.NonPublic)!.Attributes}; nested={typeof(GC.StartNoGCRegionStatus).Attributes}");
if (!typeof(RuntimeILGenerator).GetField("m_ILStream", BindingFlags.Instance | BindingFlags.NonPublic)!.IsPrivate ||
    !typeof(RuntimeILGenerator).GetMethod("IncreaseCapacity", BindingFlags.Instance | BindingFlags.NonPublic)!.IsPrivate ||
    !typeof(GC.StartNoGCRegionStatus).IsNestedPrivate)
    throw new Exception("The probe must exercise genuinely private members/types in the installed runtime");
#if MISSING_MEMBER
Console.WriteLine(il.FieldThatDoesNotExist);
#endif
#if WRONG_SIGNATURE
string wrong = il.m_localCount;
#endif
var oldSize = il.m_ILStream.Length;
il.IncreaseCapacity(4096); // Private method, not just an internal member.
if (il.m_ILStream.Length <= oldSize) throw new Exception("Private method did not grow the stream");
il.m_localSignature.AddArgument(typeof(int), false);
++il.m_localCount;
var scope = ((DynamicILGenerator)il).m_scope;
scope.m_tokens.Add(typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) })!.MethodHandle);
var token = (scope.m_tokens.Count - 1) | 0x06000000;
il.m_ILStream[il.m_length++] = (byte)OpCodes.Ldc_I4_S.Value;
il.m_ILStream[il.m_length++] = unchecked((byte)-42);
il.UpdateStackSize(OpCodes.Ldc_I4_S, 1);
il.m_ILStream[il.m_length++] = (byte)OpCodes.Call.Value;
for (var shift = 0; shift != 32; shift += 8)
    il.m_ILStream[il.m_length++] = (byte)(token >> shift);
il.UpdateStackSize(OpCodes.Call, 0);
il.Emit(OpCodes.Stloc_0);
il.Emit(OpCodes.Ldloc_0);
il.Emit(OpCodes.Ret);
var result = ((Func<int>)method.CreateDelegate(typeof(Func<int>)))();
if (result != 42) throw new Exception("Unexpected result: " + result);
Console.WriteLine("Direct private fields + private method + internal types: result=" + result);
Console.WriteLine("Private nested type: " + typeof(GC.StartNoGCRegionStatus).FullName);

#if !GENERATED_ACCESS_ATTRIBUTE
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class IgnoresAccessChecksToAttribute(string assemblyName) : Attribute
    {
        public string AssemblyName { get; } = assemblyName;
    }
}
#endif

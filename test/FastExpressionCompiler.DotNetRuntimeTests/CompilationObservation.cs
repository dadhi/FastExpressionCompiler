using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Xunit.Sdk;
[assembly: CompilationObservation]

// CollectionBehavior disables parallel execution. Child threads contribute to
// the active test's counter. Open streams avoid a file open for every test.
public sealed class CompilationObservationAttribute : BeforeAfterTestAttribute
{
    private static long requests;
    private static StreamWriter observations;
    private static FileStream current;
    public static void Record() => Interlocked.Increment(ref requests);
    public override void Before(MethodInfo methodUnderTest)
    {
        Interlocked.Exchange(ref requests, 0);
        string path = Environment.GetEnvironmentVariable("FEC_TEST_CURRENT");
        if (string.IsNullOrEmpty(path)) return;
        current ??= new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        current.Position = 0;
        byte[] data = System.Text.Encoding.UTF8.GetBytes(methodUnderTest.DeclaringType.FullName + "." + methodUnderTest.Name);
        current.Write(data);
        current.SetLength(data.Length);
        current.Flush();
    }
    public override void After(MethodInfo methodUnderTest)
    {
        string path = Environment.GetEnvironmentVariable("FEC_TEST_OBSERVATIONS");
        if (string.IsNullOrEmpty(path)) return;
        observations ??= new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
        observations.WriteLine(JsonSerializer.Serialize(new {
            method = methodUnderTest.DeclaringType.FullName + "." + methodUnderTest.Name,
            compileRequests = Interlocked.Read(ref requests)
        }));
        observations.Flush();
    }
}

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace IgnoresAccessChecksToGenerator.Tasks;

// Experimental replacement backend for the upstream task, using only System.Reflection.Metadata.
// Unlike upstream this prototype is .NET-10-MSBuild/CoreLib-only and returns the matching reference universe.
public sealed class PublicizeInternals : Microsoft.Build.Utilities.Task
{
    [Required] public string ImplementationAssembly { get; set; } = "";
    [Required] public string IntermediateOutputPath { get; set; } = "";
    [Required] public string GeneratedCodeFilePath { get; set; } = "";
    public bool IncludePrivateMembers { get; set; }
    [Output] public ITaskItem[] ReferencePaths { get; set; } = [];

    public override bool Execute()
    {
        try
        {
            var output = Path.Combine(Path.GetFullPath(IntermediateOutputPath), "System.Private.CoreLib.dll");
            ReferencePublicizer.Generate(ImplementationAssembly, output, IncludePrivateMembers);
            ReferenceVerifier.Verify(ImplementationAssembly, output, IncludePrivateMembers);
            ReferencePaths = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(output)!, "runtime-references.txt"))
                .Select(path => (ITaskItem)new TaskItem(path)).ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(GeneratedCodeFilePath))!);
            File.WriteAllText(GeneratedCodeFilePath, """
                [assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("System.Private.CoreLib")]
                namespace System.Runtime.CompilerServices
                {
                    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
                    internal sealed class IgnoresAccessChecksToAttribute(string assemblyName) : System.Attribute
                    {
                        public string AssemblyName { get; } = assemblyName;
                    }
                }
                """);
            return true;
        }
        catch (Exception error)
        {
            Log.LogErrorFromException(error, true);
            return false;
        }
    }
}

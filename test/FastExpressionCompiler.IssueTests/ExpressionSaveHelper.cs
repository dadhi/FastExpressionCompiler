using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace FastExpressionCompiler.IssueTests
{
#if NET46
    public static class ExpressionSaveHelper
    {
        public static void SaveToAssembly<T>(this Expression<T> lambda, string file)
        {
            AssemblyName assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(file));
            AssemblyBuilder dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save, Path.GetDirectoryName(file));
            var dynamicModule = dynamicAssembly.DefineDynamicModule(assemblyName + "_module", assemblyName + ".dll");
            var dynamicType = dynamicModule.DefineType(assemblyName + "_type");

            lambda.CompileToMethod(dynamicType.DefineMethod(assemblyName.Name+"_method", MethodAttributes.Public | MethodAttributes.Static));
            dynamicType.CreateType();
            dynamicAssembly.Save(Path.GetFileName(file));
        }
    }
#endif
}

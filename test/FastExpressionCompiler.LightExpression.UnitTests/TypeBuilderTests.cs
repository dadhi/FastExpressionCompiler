using NUnit.Framework;
using System;
using System.Reflection;
using System.Reflection.Emit;
using static FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.LightExpression.UnitTests
{
    [TestFixture]
    public class TypeBuilderTests
    {
        [Test]
        public void Should_compile_typebuilder()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("FastExpressionCompiler.RuntimeTypes"),
                AssemblyBuilderAccess.Run);

            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            TypeBuilder typeBuilder = moduleBuilder.DefineType("SimpleRuntimeType",
                    TypeAttributes.Public |
                    TypeAttributes.Class);

            PropertyInfo valueProp = typeBuilder.DefineAutoProperty("Value", typeof(int));

            ParameterExpression thisParam = Parameter(typeBuilder, "this");

            typeBuilder.DefineMethod("DuplicateValue",
                            Lambda(
                                Assign(Property(thisParam, valueProp),
                                    Multiply(Property(thisParam, valueProp), Constant(2))),
                                thisParam)
                        );

            Type createdType = typeBuilder.CreateType();

            object instance = Activator.CreateInstance(createdType);

            PropertyInfo createdValueProp = createdType.GetProperty("Value");

            createdValueProp.SetValue(instance, 2);

            MethodInfo duplicateMethod = createdType.GetMethod("DuplicateValue");

            duplicateMethod.Invoke(instance, null);

            int result = (int)createdValueProp.GetValue(instance);

            Assert.AreEqual(4, result);
        }
    }


    public static class TypeBuilderExtensions
    {
        public static MethodBuilder DefineMethod(
            this TypeBuilder typeBuilder,
            string methodName,
            LambdaExpression expression,
            MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual,
            CallingConventions callingConventions = CallingConventions.Standard | CallingConventions.HasThis)
        {
            int start;
            if (callingConventions.HasFlag(CallingConventions.HasThis))
            {
                start = 1;
                if (expression.Parameters.Count <= 0 || !expression.Parameters[0].Type.IsAssignableFrom(typeBuilder))
                {
                    throw new ArgumentException("Define the first parameter as the conaining type, or remove HasThis flag from MethodAttributes.");
                }
            }
            else
            {
                start = 0;
            }

            Type[] paramTypes = new Type[expression.Parameters.Count - start];
            string[] paramNames = new string[expression.Parameters.Count - start];

            for (int i = start; i < expression.Parameters.Count; i++)
            {
                paramTypes[i - 1] = expression.Parameters[i].Type;
            }

            MethodBuilder newMethod = typeBuilder.DefineMethod(
                methodName,
                methodAttributes,
                callingConventions,
                expression.ReturnType,
                paramTypes);

            for (int i = 0; i < paramNames.Length; i++)
            {
                newMethod.DefineParameter(i, ParameterAttributes.None, paramNames[i]);
            }

            bool compile = FastExpressionCompiler.LightExpression.ExpressionCompiler.CompileFastToIL(expression, newMethod.GetILGenerator(), true);

            if (!compile)
                throw new InvalidOperationException("CompileFastToIL returned an error.");

            return newMethod;
        }
 
        public static PropertyInfo DefineAutoProperty(this TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

            // setter
            MethodBuilder getPropMthdBldr = typeBuilder.DefineMethod(
                "get_" + propertyName,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.CheckAccessOnOverride,
                propertyType,
                Type.EmptyTypes);

            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                typeBuilder.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.Virtual |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            // getter
            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);

            return propertyBuilder;
        } 
    }
}

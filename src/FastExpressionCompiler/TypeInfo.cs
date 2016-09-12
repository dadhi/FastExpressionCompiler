/*
The MIT License (MIT)

Copyright (c) 2013 Maksim Volkau

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FastExpressionCompiler
{
    /// <summary>Provides <see cref="GetTypeInfo"/> for the type.</summary>
    public static class TypeInfoTools
    {
        /// <summary>Wraps input type into <see cref="TypeInfo"/> structure.</summary>
        /// <param name="type">Input type.</param> <returns>Type info wrapper.</returns>
        public static TypeInfo GetTypeInfo(this Type type)
        {
            return new TypeInfo(type);
        }
    }

    /// <summary>Partial analog of TypeInfo existing in .NET 4.5 and higher.</summary>
    public struct TypeInfo
    {
        /// <summary>Creates type info by wrapping input type.</summary> <param name="type">Type to wrap.</param>
        public TypeInfo(Type type)
        {
            _type = type;
        }
#pragma warning disable 1591 // "Missing XML-comment"
        public Type AsType() { return _type; }

        public IEnumerable<ConstructorInfo> DeclaredConstructors
        {
            get { return _type.GetConstructors(ALL_DECLARED ^ BindingFlags.Static); }
        }

        public IEnumerable<MethodInfo> DeclaredMethods
        {
            get { return _type.GetMethods(ALL_DECLARED); }
        }

        public IEnumerable<FieldInfo> DeclaredFields
        {
            get { return _type.GetFields(ALL_DECLARED); }
        }

        public IEnumerable<PropertyInfo> DeclaredProperties
        {
            get { return _type.GetProperties(ALL_DECLARED); }
        }

        public IEnumerable<Type> ImplementedInterfaces { get { return _type.GetInterfaces(); } }

        public IEnumerable<Attribute> GetCustomAttributes(Type attributeType, bool inherit)
        {
            return _type.GetCustomAttributes(attributeType, inherit).Cast<Attribute>();
        }

        public Type BaseType { get { return _type.BaseType; } }
        public bool IsGenericType { get { return _type.IsGenericType; } }
        public bool IsGenericTypeDefinition { get { return _type.IsGenericTypeDefinition; } }
        public bool ContainsGenericParameters { get { return _type.ContainsGenericParameters; } }
        public Type[] GenericTypeParameters { get { return _type.GetGenericArguments(); } }
        public Type[] GenericTypeArguments { get { return _type.GetGenericArguments(); } }
        public Type[] GetGenericParameterConstraints() { return _type.GetGenericParameterConstraints(); }
        public bool IsClass { get { return _type.IsClass; } }
        public bool IsInterface { get { return _type.IsInterface; } }
        public bool IsValueType { get { return _type.IsValueType; } }
        public bool IsPrimitive { get { return _type.IsPrimitive; } }
        public bool IsArray { get { return _type.IsArray; } }
        public bool IsPublic { get { return _type.IsPublic; } }
        public bool IsNestedPublic { get { return _type.IsNestedPublic; } }
        public Type DeclaringType { get { return _type.DeclaringType; } }
        public bool IsAbstract { get { return _type.IsAbstract; } }
        public bool IsSealed { get { return _type.IsSealed; } }
        public bool IsEnum { get { return _type.IsEnum; } }

        public Type GetElementType() { return _type.GetElementType(); }

        public bool IsAssignableFrom(TypeInfo typeInfo) { return _type.IsAssignableFrom(typeInfo.AsType()); }
#pragma warning restore 1591 // "Missing XML-comment"

        private readonly Type _type;

        private const BindingFlags ALL_DECLARED =
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;
    }
}

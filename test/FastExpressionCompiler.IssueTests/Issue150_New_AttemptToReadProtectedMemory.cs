using System.Collections.Generic;
using NUnit.Framework;
#pragma warning disable 659

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
 using static System.Linq.Expressions.Expression;
 namespace FastExpressionCompiler.IssueTests
#endif
{
    using System;
    using System.Linq;
    using System.Reflection;

    [TestFixture]
    public class Issue150_New_AttemptToReadProtectedMemory : ITest
    {
        public int Run() 
        {
            Nested_Assignments_Should_Work();
            return 1;
        }

        [Test]
        public void Nested_Assignments_Should_Work()
        {
            // Builds:
            // 
            // dsosToPpssData =>
            // {
            //     var publicPropertyStruct_String = dsosToPpssData.Target;
            //     string valueKey;
            //     object value;
            //     publicPropertyStruct_String.Value = ((valueKey = dsosToPpssData.Source.Keys.FirstOrDefault(key => key.MatchesKey("Value"))) != null)
            //         ? ((value = dsosToPpssData.Source[valueKey]) != null) ? value.ToString() : null
            //         : null;

            //     return publicPropertyStruct_String;
            // }

            var mappingDataParameter = Parameter(
                typeof(MappingData<Dictionary<string, int>, PublicPropertyStruct<string>>),
                "dsosToPpssData");

            var structVariable = Variable(typeof(PublicPropertyStruct<string>), "publicPropertyStruct_String");

            var structVariableAssignment = Assign(structVariable, Property(mappingDataParameter, "Target"));

            var sourceDictionary = Property(mappingDataParameter, "Source");
            var nullString = Default(typeof(string));
            var valueKeyVariable = Variable(typeof(string), "valueKey");

            var linqFirstOrDefaultMethod = typeof(Enumerable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(string));

            var dictionaryKeys = Property(sourceDictionary, "Keys");

            var keyParameter = Parameter(typeof(string), "key");
            var matchesKeyMethod = typeof(MyIssueExtensions).GetMethod("MatchesKey");
            var matchesKeyCall = Call(matchesKeyMethod, keyParameter, Constant("Value"));
            var matchesKeyLambda = Lambda<Func<string, bool>>(matchesKeyCall, keyParameter);

            var firstOrDefaultKeyCall = Call(linqFirstOrDefaultMethod, dictionaryKeys, matchesKeyLambda);

            var valueKeyAssignment = Assign(valueKeyVariable, firstOrDefaultKeyCall);
            var valueKeyNotNull = NotEqual(valueKeyAssignment, nullString);

            var valueVariable = Variable(typeof(object), "value");

            var dictionaryIndexer = sourceDictionary.Type.GetProperties().First(p => p.GetIndexParameters().Length != 0);
            var dictionaryIndexAccess = MakeIndex(sourceDictionary, dictionaryIndexer, new[] { valueKeyVariable });
            var dictionaryValueAsObject = Convert(dictionaryIndexAccess, typeof(object));

            var valueAssignment = Assign(valueVariable, dictionaryValueAsObject);
            var valueNotNull = NotEqual(valueAssignment, Default(valueVariable.Type));

            var objectToStringMethod = valueVariable.Type.GetMethod("ToString");
            var valueToString = Call(valueVariable, objectToStringMethod);

            var valueToStringOrNull = Condition(valueNotNull, valueToString, nullString);

            var dictionaryEntryOrNull = Condition(valueKeyNotNull, valueToStringOrNull, nullString);

            var structValueProperty = Property(structVariable, "Value");

            var structValueAssignment = Assign(structValueProperty, dictionaryEntryOrNull);

            var structPopulation = Block(
                new[] { valueKeyVariable, valueVariable },
                structValueAssignment);

            var structMapping = Block(
                new[] { structVariable },
                structVariableAssignment,
                structPopulation,
                structVariable);

            var populationLambda = Lambda<Func<MappingData<Dictionary<string, int>, PublicPropertyStruct<string>>, PublicPropertyStruct<string>>>(
                structMapping,
                mappingDataParameter);

            var populationFunc = populationLambda.CompileFast(true);
            populationFunc.Invoke(new MappingData<Dictionary<string, int>, PublicPropertyStruct<string>>
            {
                Source = new Dictionary<string, int> { ["Value"] = 123 },
                Target = new PublicPropertyStruct<string>()
            });
        }

        public class MappingData<TSource, TTarget>
        {
            public TSource Source { get; set; }

            public TTarget Target { get; set; }
        }

        public struct PublicPropertyStruct<T>
        {
            public T Value { get; set; }
        }
    }

    public static class MyIssueExtensions
    {
        public static bool MatchesKey(this string value, string other) => value == other;
    }
}
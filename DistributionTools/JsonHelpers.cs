using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;

namespace DistributionTools
{
    public static class JsonHelpers
    {
        /// <summary>
        /// Serializes an entire object as a single string in JSON format.
        /// The object must have two methods defined:
        /// - A constructor which takes a single string as its input argument
        /// - an implicit or explicit conversion operator to type string
        /// </summary>
        public class ObjectToStringConverter<ObjectType> : JsonConverter
        {
            /// <summary>
            /// Constructs a new object of the destination type, passing the source object as constructor argument.
            /// If no suitable constructor is available, returns default value.
            /// </summary>
            private static DestType Construct<DestType>(object source)
            {
                Type srcType = source.GetType();
                if (srcType == typeof(DestType)) { return (DestType)source; }

                ConstructorInfo constructorInfo = typeof(DestType).GetConstructor(new[] { srcType });
                if (constructorInfo != null)
                    return (DestType)constructorInfo.Invoke(new object[] { source });
                else
                    return default;
            }

            /// <summary>
            /// Calls explicit or implicit conversion operator to convert the source object to the destination type.
            /// If no conversion operator is available, returns default value.
            /// </summary>
            private static DestType Convert<DestType>(object source)
            {
                Type srcType = source.GetType();
                if (srcType == typeof(DestType)) { return (DestType)source; }

                BindingFlags bf = BindingFlags.Static | BindingFlags.Public;
                MethodInfo castOperator = typeof(DestType).GetMethods(bf)
                                            .Union(srcType.GetMethods(bf))
                                            .Where(mi => mi.Name == "op_Explicit" || mi.Name == "op_Implicit")
                                            .Where(mi =>
                                            {
                                                var pars = mi.GetParameters();
                                                return pars.Length == 1 && pars[0].ParameterType == srcType;
                                            })
                                            .Where(mi => mi.ReturnType == typeof(DestType))
                                            .FirstOrDefault();
                if (castOperator != null) return (DestType)castOperator.Invoke(null, new object[] { source });
                else return default;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(Convert<string>(value));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return Construct<ObjectType>(reader.Value);
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(ObjectType);
            }
        }
    }
}

using Microsoft.AspNetCore.Components;
using Piral.Blazor.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Piral.Blazor.Core
{
    static class Extensions
    {
        private static readonly IDictionary<Type, PropertyDesc[]> allowedArgs = new Dictionary<Type, PropertyDesc[]>();
        private static readonly JsonElement JsonNull = JsonDocument.Parse("null").RootElement;

        class PropertyDesc
        {
            public PropertyInfo Property;
            public string OriginalName;
            public string[] ParentPath;
        }

        private class Match
        {
            public IDictionary<string, object> @params { get; set; }
        }

        public static IDictionary<string, object> AdjustArguments(this Type type, IDictionary<string, JsonElement> args)
        {
            if (!allowedArgs.TryGetValue(type, out var allowed))
            {
                allowed = type.GetProperties()
                    .Where(m => m.GetCustomAttributes<ParameterAttribute>(true).Any())
                    .SelectMany(m =>
                    {
                        var parameters = m.GetCustomAttributes<PiralParameterAttribute>(true).ToArray();

                        if (parameters.Length == 0)
                        {
                            return new PropertyDesc[]
                            {
                                new PropertyDesc
                                {
                                    Property = m,
                                    OriginalName = m.Name,
                                    ParentPath = new string[0],
                                }
                            };
                        }

                        return parameters.Select(p =>
                        {
                            var segments = p.JsParameterName.Split(".");

                            return new PropertyDesc
                            {
                                Property = m,
                                OriginalName = segments.Last(),
                                ParentPath = segments.Take(segments.Length - 1).ToArray(),
                            };
                        }).ToArray();
                    })
                    .ToArray();

                allowedArgs.Add(type, allowed);
            }

            return allowed.ToDictionary(m => m.Property.Name, m => m.Property.WithValue(args.GetValue(m)));
        }

        private static JsonElement GetValue(this IDictionary<string, JsonElement> obj, PropertyDesc property)
        {
            if (property.ParentPath.Length > 0)
            {
                if (!obj.TryGetValue(property.ParentPath[0], out var parent))
                {
                    return JsonNull;
                }

                for (var i = 1; i < property.ParentPath.Length; i++)
                {
                    if (!parent.TryGetProperty(property.ParentPath[1], out parent))
                    {
                        return JsonNull;
                    }
                }

                if (parent.TryGetProperty(property.OriginalName, out var result))
                {
                    return result;
                }
            }
            else if (obj.TryGetValue(property.OriginalName, out var result))
            {
                return result;
            }

            return JsonNull;
        }

        private static object WithValue(this PropertyInfo property, JsonElement value)
        {
            var propType = property.PropertyType;

            if (value.ValueKind == JsonValueKind.Null)
            {
                return propType.GetDefaultValue();
            }
            else if (typeof(bool) == propType)
            {
                return value.GetBoolean();
            }
            else if (typeof(int) == propType)
            {
                return value.GetInt32();
            }
            else if (typeof(uint) == propType)
            {
                return value.GetUInt32();
            }
            else if (typeof(short) == propType)
            {
                return value.GetInt16();
            }
            else if (typeof(ushort) == propType)
            {
                return value.GetUInt16();
            }
            else if (typeof(long) == propType)
            {
                return value.GetInt64();
            }
            else if (typeof(ulong) == propType)
            {
                return value.GetUInt64();
            }
            else if (typeof(string) == propType)
            {
                return value.GetString();
            }
            else if (typeof(byte) == propType)
            {
                return value.GetByte();
            }
            else if (typeof(sbyte) == propType)
            {
                return value.GetSByte();
            }
            else if (typeof(DateTime) == propType)
            {
                return value.GetDateTime();
            }
            else if (typeof(DateTimeOffset) == propType)
            {
                return value.GetDateTimeOffset();
            }
            else if (typeof(double) == propType)
            {
                return value.GetDouble();
            }
            else if (typeof(float) == propType)
            {
                return value.GetSingle();
            }
            else if (typeof(Guid) == propType)
            {
                return value.GetGuid();
            }
            else if (typeof(decimal) == propType)
            {
                return value.GetDecimal();
            }
            else if (value.GetType() == propType)
            {
                return value;
            }
            else
            {
                return value.ToObject(propType);
            }
        }

        private static object ToObject(this JsonElement element, Type type)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();

            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                element.WriteTo(writer);
            }

            return JsonSerializer.Deserialize(bufferWriter.WrittenSpan, type);
        }

        private static object GetDefaultValue(this Type t)
        {
            if (t.IsValueType)
            {
                return Activator.CreateInstance(t);
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<Type> GetTypesWithAttributes(this Assembly assembly,
            IReadOnlyCollection<Type> attributeTypes)
        {
            return assembly?.GetTypes().Where(m => m.HasAnyAttribute(attributeTypes)) ?? Enumerable.Empty<Type>();
        }

        public static bool HasAnyAttribute(this Type member, IEnumerable<Type> attributeTypes)
        {
            return attributeTypes.Any(attributeType => Attribute.IsDefined(member, attributeType));
        }
    }
}

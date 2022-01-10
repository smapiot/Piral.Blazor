using Microsoft.AspNetCore.Components;
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
        private static readonly IDictionary<Type, string[]> allowedArgs = new Dictionary<Type, string[]>();

        private class Match
        {
            public IDictionary<string, object> @params { get; set; }
        }

        public static IDictionary<string, object> AdjustArguments(this Type type, IDictionary<string, JsonElement> args)
        {
            if (!allowedArgs.TryGetValue(type, out var allowed))
            {
                allowed = type.GetProperties()
                    .Where(m => m.GetCustomAttributes(typeof(ParameterAttribute), true).Any())
                    .Select(m => m.Name)
                    .ToArray();

                allowedArgs.Add(type, allowed);
            }

            var allArgs = args.Select(m => m).ToList();

            if (args.TryGetValue("match", out var match) && match.TryGetProperty("params", out var routeParams))
            {
                allArgs.AddRange(routeParams.EnumerateObject().Select(m => new KeyValuePair<string, JsonElement>(m.Name, m.Value)));
            }

            var adjustedArgs = allArgs
                .Where(m => allowed.Contains(m.Key))
                .ToDictionary(m => m.Key, m => type.NormalizeValue(m.Key, m.Value));

            return adjustedArgs;
        }

        public static object NormalizeValue(this Type type, string key, JsonElement value)
        {
            var property = type.GetProperty(key);
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

        public static T ToObject<T>(this JsonElement element)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();

            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                element.WriteTo(writer);
            }

            return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan);
        }

        public static object ToObject(this JsonElement element, Type type)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();

            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                element.WriteTo(writer);
            }

            return JsonSerializer.Deserialize(bufferWriter.WrittenSpan, type);
        }

        public static object GetDefaultValue(this Type t)
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

        public static bool IsNullOrEmpty<TKey, TValue>(this IDictionary<TKey, TValue> collection)
        {
            return (collection == null || collection.Count < 1);
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

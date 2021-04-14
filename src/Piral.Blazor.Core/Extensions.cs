using Microsoft.AspNetCore.Components;
using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Piral.Blazor.Core
{
    static class Extensions
    {
        private static readonly IDictionary<Type, string[]> allowedArgs = new Dictionary<Type, string[]>();
        
        private static ILogger _logger;

        public static void Configure(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(Extensions));
        }

        private class Match
        {
            public IDictionary<string, object> @params { get; set; }
        }
        
        public static IDictionary<string, object> AdjustArguments(this Type type, IDictionary<string, object> args)
        {
            if (!allowedArgs.TryGetValue(type, out var allowed))
            {
                allowed = type.GetProperties()
                    .Where(m => m.GetCustomAttributes(typeof(ParameterAttribute), true).Any())
                    .Select(m => m.Name)
                    .ToArray();

                allowedArgs.Add(type, allowed);
            }

            var allArgs = new List<IDictionary<string, object>> {args};
            try
            {
                var routeParams = JsonSerializer.Deserialize<Match>(JsonSerializer.Serialize(args["match"]))?.@params;
                if(!routeParams.IsNullOrEmpty()) allArgs.Add(routeParams);
            }
            catch(KeyNotFoundException) { }

            var adjustedArgs = allArgs
                .SelectMany(dict => dict)
                .Where(m => allowed.Contains(m.Key))
                .ToDictionary(m => m.Key, m => type.NormalizeValue(m.Key, m.Value));
            
            return adjustedArgs;
        }
        
        public static object NormalizeValue(this Type type, string key, object value)
        {
            var property = type.GetProperty(key);
            var propType = property.PropertyType;
            
            if (value == null) return propType.GetDefaultValue();
            if (value.GetType() == propType) return value;
            if (!(value is JsonElement e)) return value;
            
            if (propType == typeof(int))
            {
                if (e.ValueKind == JsonValueKind.Number) return e.GetInt32();
                if (e.ValueKind == JsonValueKind.String) return int.Parse(e.GetString());
            }
            else if (propType == typeof(double))
            {
                if (e.ValueKind == JsonValueKind.Number) return e.GetDouble();
                if (e.ValueKind == JsonValueKind.String) return double.Parse(e.GetString());
            }
            else if (propType == typeof(string))
            {
                return e.GetString();
            }
            else if (propType == typeof(bool))
            {
                if (e.ValueKind == JsonValueKind.True || e.ValueKind == JsonValueKind.False) return e.GetBoolean();
                if (e.ValueKind == JsonValueKind.String) return bool.Parse(e.GetString());
            }
            else if (propType == typeof(Guid))
            {
                return e.GetGuid();
            }
            else if (propType == typeof(DateTime))
            {
                return e.GetDateTime();
            }

            return value;
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

        public static bool HasExplicitFlag(this IDictionary<string, object> args, bool expectedValue, string option)
        {
            if (args.IsNullOrEmpty()) return false;
            try
            {
                return args.TryGetValue(option, out var value) && ((JsonElement) value).GetBoolean() == expectedValue;
            }
            catch
            {
                _logger.LogWarning(
                    $"The '{option}' option is not set correctly. It should be either 'true' or 'false'.");
                return false;
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

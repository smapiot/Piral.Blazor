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

        private static readonly IReadOnlyCollection<Type> AttributeTypes = new List<Type>
        {
            typeof(ExposePiletAttribute),
            typeof(RouteAttribute)
        };

        private static ILogger _logger;

        public static void Configure(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(Extensions));
        }

        public static void RegisterAll(this ComponentActivationService activationService, Assembly assembly,
            IServiceProvider container, IDictionary<string, object> args = default)
        {
            var attributeTypes = FilterAttributeTypes(args);
            var componentTypes = assembly.GetTypesWithAttributes(attributeTypes);

            foreach (var componentType in componentTypes)
            {
                var attributeValues = componentType.GetAllAttributeValues(attributeTypes);
                if (attributeValues is null) continue;

                foreach (string componentName in attributeValues)
                {
                    activationService?.Register(componentName, componentType, container);
                    _logger.LogInformation($"registered {componentName}");
                }
            }
        }

        public static void UnregisterAll(this ComponentActivationService activationService, Assembly assembly)
        {
            var componentTypes = assembly.GetTypesWithAttributes(AttributeTypes);

            foreach (var componentType in componentTypes)
            {
                var attributeValues = componentType.GetAllAttributeValues(AttributeTypes);
                if (attributeValues is null) continue;

                foreach (string attributeValue in attributeValues)
                {
                    activationService?.Unregister(attributeValue);
                }
            }
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

            return args
                .Where(m => allowed.Contains(m.Key))
                .ToDictionary(m => m.Key, m => type.NormalizeValue(m.Key, m.Value));
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

        private static IReadOnlyCollection<Type> FilterAttributeTypes(IDictionary<string, object> args)
        {
            var types = AttributeTypes;

            types = FilterPages(args, types);
            // potentially more filters here

            return types;
        }

        private static IReadOnlyCollection<Type> FilterPages(IDictionary<string, object> args, IReadOnlyCollection<Type> types)
        {
            if (args.HasExplicitFlag(true, "includePages"))
                return types; //don't filter anything out

            _logger.LogInformation(
                "Pages decorated with the @page directive are not included in the Blazor references. If this is unintended, reference the docs."
            );

            return types.Where(at => at != typeof(RouteAttribute)).ToList().AsReadOnly(); // filter out the pages
        }


        private static bool HasExplicitFlag(this IDictionary<string, object> args, bool expectedValue, string option)
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

        private static bool IsNullOrEmpty<TKey, TValue>(this IDictionary<TKey, TValue> collection)
        {
            return (collection == null || collection.Count < 1);
        }

        private static IEnumerable<Type> GetTypesWithAttributes(this Assembly assembly,
            IReadOnlyCollection<Type> attributeTypes)
        {
            return assembly?.GetTypes().Where(m => m.HasAnyAttribute(attributeTypes)) ?? Enumerable.Empty<Type>();
        }

        private static bool HasAnyAttribute(this Type member, IEnumerable<Type> attributeTypes)
        {
            return attributeTypes.Any(attributeType => Attribute.IsDefined(member, attributeType));
        }

        private static IEnumerable<string> GetAllAttributeValues(this Type member, IEnumerable<Type> attributeTypes)
        {
            return attributeTypes.Select(member.GetAttributeValue).Where(val => val != null);
        }

        private static string GetAttributeValue(this Type member, Type attributeType)
        {
            string value = attributeType.Name switch
            {
                nameof(RouteAttribute) => member.GetCustomAttribute<RouteAttribute>(false)?.Template,
                nameof(ExposePiletAttribute) => member.GetCustomAttribute<ExposePiletAttribute>(false)?.Name,
                _ => null
            };

            return value is null ? null : SanitizeAttributeValue(value);
        }

        /// <summary>
        /// Sanitizing a Blazor attribute value. Any leading slashes are removed and
        /// everything that is not alphanumeric, an underscore or a dash gets replaced with an underscore.
        /// </summary>
        private static string SanitizeAttributeValue(string value)
        {
            string val = value.StartsWith("/") ? value.Substring(1) : value;
            return Regex.Replace(val, @"[^\w\-]", "_");
        }
    }
}

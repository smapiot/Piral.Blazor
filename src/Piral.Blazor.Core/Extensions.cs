using Microsoft.AspNetCore.Components;
using Piral.Blazor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Piral.Blazor.Core
{
    static class Extensions
    {
        private static readonly IDictionary<Type, string[]> allowedArgs = new Dictionary<Type, string[]>();
        private static readonly IEnumerable<Type> AttributeTypes =  new List<Type> { typeof(ExposePiletAttribute), typeof(RouteAttribute) };

        public static void RegisterAll(this ComponentActivationService activationService, Assembly assembly,
            IServiceProvider container, IDictionary<string, object> args = default)
        {
            var attributeTypes = AttributeTypes;

            if (args is null || !args.TryGetValue("includePages", out var includePages) || includePages is null)// || !(bool)includePages) --> TODO cast fails
            {
                attributeTypes = AttributeTypes.Where(at => at != typeof(RouteAttribute));
            }

            var componentTypes = assembly.GetTypesWithAttributes(AttributeTypes);

            foreach (var componentType in componentTypes)
            {
                var attributeValues = componentType.GetAllAttributeValues(attributeTypes);
                if(attributeValues is null) continue;
                
                foreach (string componentName in attributeValues)
                {
                    activationService?.Register(componentName, componentType, container);
                }
            }
        }
        
        public static void UnregisterAll(this ComponentActivationService activationService, Assembly assembly) 
        {
            var componentTypes = assembly.GetTypesWithAttributes(AttributeTypes);

            foreach (var componentType in componentTypes)
            {
                var attributeValues = componentType.GetAllAttributeValues(AttributeTypes);
                if(attributeValues is null) continue;
                
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

            if (value == null)
            {
                return propType.GetDefaultValue();
            }

            if (value.GetType() != propType)
            {
                if (value is JsonElement e)
                {
                    if (propType == typeof(int))
                    {
                        return e.GetInt32();
                    }
                    else if (propType == typeof(double))
                    {
                        return e.GetDouble();
                    }
                    else if (propType == typeof(string))
                    {
                        return e.GetString();
                    }
                    else if (propType == typeof(bool))
                    {
                        return e.GetBoolean();
                    }
                    else if (propType == typeof(Guid))
                    {
                        return e.GetGuid();
                    }
                    else if (propType == typeof(DateTime))
                    {
                        return e.GetDateTime();
                    }
                }
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
        
        private static IEnumerable<Type> GetTypesWithAttributes(this Assembly assembly, IEnumerable<Type> attributeTypes)
        {
            return assembly?.GetTypes().Where(m => m.HasAnyAttribute(attributeTypes)) ?? Enumerable.Empty<Type>();
        }

        private static bool HasAnyAttribute(this Type member, IEnumerable<Type> attributeTypes)
        {
            return attributeTypes.Any(attributeType =>  Attribute.IsDefined(member, attributeType));
        }
        
        private static IEnumerable<string> GetAllAttributeValues(this Type member, IEnumerable<Type> attributeTypes)
        {
            return attributeTypes.Select(member.GetAttributeValue).Where(val=> val != null);
        }
        
        private static string GetAttributeValue(this Type member, Type attributeType)
        {
            return attributeType.Name switch
            {
                nameof(RouteAttribute) => member.GetCustomAttribute<RouteAttribute>(false)?.Template.Replace("/", ""), //TODO consistent url manipulation
                nameof(ExposePiletAttribute) => member.GetCustomAttribute<ExposePiletAttribute>(false)?.Name,
                _ => null
            };
        }
    }
}

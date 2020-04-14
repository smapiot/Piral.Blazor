using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Piral.Blazor.Core
{
    static class Extensions
    {
        private static readonly IDictionary<Type, string[]> allowedArgs = new Dictionary<Type, string[]>();

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
    }
}

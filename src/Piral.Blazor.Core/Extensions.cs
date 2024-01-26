using Microsoft.AspNetCore.Components;
using Piral.Blazor.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Piral.Blazor.Core;

static class Extensions
{
    private static readonly IDictionary<Type, PropertyDesc[]> allowedArgs = new Dictionary<Type, PropertyDesc[]>();
    private static readonly JsonElement JsonNull = JsonDocument.Parse("null").RootElement;

    abstract class PropertyDesc
    {
        public PropertyInfo Property;
        public string OriginalName;
        public string Name => Property.Name;
        public abstract object GetValue(RenderArgs args);
    }

    sealed class RenderArgs
    {
        public RenderArgs(NavigationManager navigationManager, IDictionary<string, JsonElement> providedArgs)
        {
            Navigation = navigationManager;
            Arguments = providedArgs;
        }

        public NavigationManager Navigation { get; }

        public IDictionary<string, JsonElement> Arguments { get; }
    }

    sealed class QueryPropertyDesc : PropertyDesc
    {
        public string QueryName;

        public override object GetValue(RenderArgs args)
        {
            var navManager = args.Navigation;
            var queryMap = HttpUtility.ParseQueryString(new Uri(navManager.Uri).Query);

            if (queryMap.Count > 0)
            {
                var value = queryMap[QueryName];

                if (value is not null)
                {
                    return Property.WithValue(value);
                }
            }

            return null;
        }
    }

    sealed class SimplePropertyDesc : PropertyDesc
    {
        public string[] ParentPath;

        public override object GetValue(RenderArgs args)
        {
            var value = GetValueRef(args.Arguments);
            return Property.WithValue(value);
        }        

        private JsonElement GetValueRef(IDictionary<string, JsonElement> args)
        {
            if (ParentPath.Length > 0)
            {
                if (!args.TryGetValue(ParentPath[0], out var parent))
                {
                    return JsonNull;
                }

                for (var i = 1; i < ParentPath.Length; i++)
                {
                    if (!parent.TryGetProperty(ParentPath[1], out parent))
                    {
                        return JsonNull;
                    }
                }

                if (parent.TryGetProperty(OriginalName, out var result))
                {
                    return result;
                }
            }
            else if (args.TryGetValue(OriginalName, out var item))
            {
                return item;
            }
            else if (OriginalName == "ChildContent" && args.TryGetValue("children", out var children))
            {
                return children;
            }

            return JsonNull;
        }
    }

    private class Match
    {
        public IDictionary<string, object> @params { get; set; }
    }
    
    public static void Forget(this Task task)
    {
        task.ConfigureAwait(false);
    }

    public static IDictionary<string, object> AdjustArguments(this Type type, NavigationManager navigationManager, IDictionary<string, JsonElement> providedArgs)
    {
        if (!allowedArgs.TryGetValue(type, out var allowed))
        {
            allowed = type.GetProperties()
                .Where(m => m.GetCustomAttributes<ParameterAttribute>(true).Any())
                .SelectMany(m =>
                {
                    var parameters = m.GetCustomAttributes<PiralParameterAttribute>(true).ToArray();

                    if (parameters.Length > 0)
                    {
                        return parameters.Select(p =>
                        {
                            var segments = p.JsParameterName.Split(".");

                            return new SimplePropertyDesc
                            {
                                Property = m,
                                OriginalName = segments.Last(),
                                ParentPath = segments.Take(segments.Length - 1).ToArray(),
                            };
                        }).ToArray();
                    }
                    
                    var queryParameters = m.GetCustomAttributes<PiralQueryParameterAttribute>(true).ToArray();

                    if (queryParameters.Length > 0)
                    {
                        return queryParameters.Select(p =>
                        {
                            return new QueryPropertyDesc
                            {
                                Property = m,
                                OriginalName = m.Name,
                                QueryName = p.QueryParameterName,
                            };
                        }).ToArray();
                    }
                    
                    return new PropertyDesc[]
                    {
                        new SimplePropertyDesc
                        {
                            Property = m,
                            OriginalName = m.Name,
                            ParentPath = Array.Empty<string>(),
                        }
                    };
                })
                .ToArray();

            allowedArgs.Add(type, allowed);
        }

        var args = new RenderArgs(navigationManager, providedArgs);
        return allowed.ToDictionary(m => m.Name, m => m.GetValue(args));
    }

    private static object WithValue(this PropertyInfo property, string value)
    {
        var typeCode = Type.GetTypeCode(property.PropertyType);

        switch (typeCode)
        {
            case TypeCode.Empty:
            case TypeCode.DBNull:
                return null;
            case TypeCode.Object:
                return value;
            case TypeCode.Boolean:
                return Convert.ToBoolean(value);
            case TypeCode.Char:
                return Convert.ToChar(value);
            case TypeCode.SByte:
                return Convert.ToSByte(value);
            case TypeCode.Byte:
                return Convert.ToByte(value);
            case TypeCode.Int16:
                return Convert.ToInt16(value);
            case TypeCode.UInt16:
                return Convert.ToUInt16(value);
            case TypeCode.Int32:
                return Convert.ToInt32(value);
            case TypeCode.UInt32:
                return Convert.ToUInt32(value);
            case TypeCode.Int64:
                return Convert.ToInt64(value);
            case TypeCode.UInt64:
                return Convert.ToUInt64(value);
            case TypeCode.Single:
                return Convert.ToSingle(value);
            case TypeCode.Double:
                return Convert.ToDouble(value);
            case TypeCode.Decimal:
                return Convert.ToDecimal(value);
            case TypeCode.DateTime:
                return Convert.ToDateTime(value);
            case TypeCode.String:
                return Convert.ToString(value);
            default:
                throw new NotSupportedException($"{property.PropertyType.FullName} is not supported! Only system built-in types are supported. Look at enumeration System.TypeCode for detail.");
        }
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
        else if (typeof(RenderFragment) == propType)
        {
            var cid = Guid.NewGuid().ToString();

            RenderFragment result = builder =>
            {
                builder.OpenElement(1, "piral-content");
                builder.AddAttribute(2, "cid", cid);
                builder.AddElementReferenceCapture(3, _ => {
                    JSBridge.RenderContent(cid, value);
                });
                builder.CloseElement();
            };

            return result;
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

        return JsonSerializer.Deserialize(bufferWriter.WrittenSpan, type, new JsonSerializerOptions
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
        });
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

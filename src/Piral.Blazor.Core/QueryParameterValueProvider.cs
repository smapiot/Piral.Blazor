using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Piral.Blazor.Utils;

namespace Piral.Blazor.Core;

public class QueryParameterValueProvider : IQueryParameterValueProvider
{
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<QueryParameterValueProvider> _logger;

    public QueryParameterValueProvider(NavigationManager navigationManager, ILogger<QueryParameterValueProvider> logger)
    {
        _navigationManager = navigationManager;
        _logger = logger;
    }

    public void ProvideQueryParameterValues(Type componentType, object instance)
    {
        try
        {
            ProvideQueryParameterValuesInternal(componentType, instance);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Query parameter values could not be provided!");
        }
    }

    private void ProvideQueryParameterValuesInternal(Type componentType, object instance)
    {
        if (IsPage(componentType) == false) return;

        var queryDictionary = System.Web.HttpUtility.ParseQueryString(new Uri(_navigationManager.Uri).Query);
        if (queryDictionary.Count == 0) return;
        
        var propertyQueryPropertyKeyCollection = GetQueryParameterProperties(componentType);

        foreach (var propertyQueryParameterKey in propertyQueryPropertyKeyCollection)
        {
            ProcessProperty(propertyQueryParameterKey, queryDictionary, instance);
        }
    }

    private bool IsPage(Type componentType)
    {
        var routeAttributes = componentType.GetCustomAttributes(typeof(RouteAttribute), false);
        return routeAttributes.Length != 0;
    }
    
    private IEnumerable<(PropertyInfo Property, string Key)> GetQueryParameterProperties(Type componentType)
    {
        var pageProperties = componentType.GetProperties();
        var queryParameterProperties = pageProperties.Where(
            p => p.GetCustomAttributes<PiralQueryParameterAttribute>(false).Any());
        var propertyQueryPropertyKeyCollection = queryParameterProperties.Select(p =>
        {
            var attribute = p.GetCustomAttribute<PiralQueryParameterAttribute>() ??
                            throw new InvalidOperationException(
                                $"Property has no {nameof(PiralQueryParameterAttribute)}!");
            return string.IsNullOrWhiteSpace(attribute.Name)
                ? (Property: p, QueryParameterKey: p.Name)
                : (Property: p, QueryParameterKey: attribute.Name);
        });
        return propertyQueryPropertyKeyCollection;
    }

    private void ProcessProperty((PropertyInfo Property, string Key) propertyQueryParameterKey,
        NameValueCollection queryDictionary, object instance)
    {
        try
        {
            var queryParameterValue = queryDictionary[propertyQueryParameterKey.Key];
            if (queryParameterValue is null) return;
            SetQueryParameterValueToProperty(propertyQueryParameterKey.Property, queryParameterValue, instance);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, $"Property '{propertyQueryParameterKey.Property.Name}' processing failed!");
        }
        
    }

    private void SetQueryParameterValueToProperty(PropertyInfo property, string value, object instance)
    {
        var typeCode = Type.GetTypeCode(property.PropertyType);
        switch (typeCode)
        {
            case TypeCode.Empty:
            case TypeCode.DBNull:
                break;
            case TypeCode.Object:
                property.SetValue(instance, value);
                break;
            case TypeCode.Boolean:
                property.SetValue(instance, Convert.ToBoolean(value));
                break;
            case TypeCode.Char:
                property.SetValue(instance, Convert.ToChar(value));
                break;
            case TypeCode.SByte:
                property.SetValue(instance, Convert.ToSByte(value));
                break;
            case TypeCode.Byte:
                property.SetValue(instance, Convert.ToByte(value));
                break;
            case TypeCode.Int16:
                property.SetValue(instance, Convert.ToInt16(value));
                break;
            case TypeCode.UInt16:
                property.SetValue(instance, Convert.ToUInt16(value));
                break;
            case TypeCode.Int32:
                property.SetValue(instance, Convert.ToInt32(value));
                break;
            case TypeCode.UInt32:
                property.SetValue(instance, Convert.ToUInt32(value));
                break;
            case TypeCode.Int64:
                property.SetValue(instance, Convert.ToInt64(value));
                break;
            case TypeCode.UInt64:
                property.SetValue(instance, Convert.ToUInt64(value));
                break;
            case TypeCode.Single:
                property.SetValue(instance, Convert.ToSingle(value));
                break;
            case TypeCode.Double:
                property.SetValue(instance, Convert.ToDouble(value));
                break;
            case TypeCode.Decimal:
                property.SetValue(instance, Convert.ToDecimal(value));
                break;
            case TypeCode.DateTime:
                property.SetValue(instance, Convert.ToDateTime(value));
                break;
            case TypeCode.String:
                property.SetValue(instance, Convert.ToString(value));
                break;
            default:
                throw new NotSupportedException($"{property.PropertyType.FullName} is not supported! Only system built-in types are supported. Look at enumeration System.TypeCode for detail.");
        }
    }
}

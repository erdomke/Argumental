using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Argumental
{
  internal static class Reflection
  {
    public static IEnumerable<Type> ParentsAndSelf(this Type type)
    {
      var curr = type;
      while (curr != null && curr != typeof(object))
      {
        yield return curr;
        curr = curr.BaseType;
      }
    }

    public static bool IsConvertibleFromString(this Type type)
    {
      // The configuration binder has hard-coded support for base64.
      if (type == typeof(object)
        || type == typeof(string)
        || type == typeof(byte[]))
        return true;

      if (type.IsGenericType
        && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        return IsConvertibleFromString(Nullable.GetUnderlyingType(type));

      var converter = TypeDescriptor.GetConverter(type);
      return converter.CanConvertFrom(typeof(string));
    }

    public static bool TryGetListType(Type type, out Type elementType)
    {
      if (type == typeof(string))
      {
        elementType = default;
        return false;
      }
      else if (type.IsArray)
      {
        elementType = type.GetElementType();
        return true;
      }
      else
      {
        elementType = default;
        var interfaces = (IEnumerable<Type>)type.GetInterfaces();
        if (type.IsInterface)
          interfaces = new[] { type }.Concat(interfaces); 
        foreach (var iface in interfaces)
        {
          if (iface.IsGenericType)
          {
            var defn = iface.GetGenericTypeDefinition();
            if (defn == typeof(IDictionary<,>)
              || defn == typeof(IReadOnlyDictionary<,>))
            {
              return false;
            }
            else if (defn == typeof(IEnumerable<>))
            {
              elementType = type.GetGenericArguments()[0];
            }
          }
        }
        return elementType != default;
      }
    }

    public static bool TryGetDictionaryType(Type type, out Type keyType, out Type valueType)
    {
      var interfaces = type.GetInterfaces();
      foreach (var iface in interfaces)
      {
        if (iface.IsGenericType)
        {
          var defn = iface.GetGenericTypeDefinition();
          if (defn == typeof(IDictionary<,>)
            || defn == typeof(IReadOnlyDictionary<,>))
          {
            var args = iface.GetGenericArguments();
            keyType = args[0];
            valueType = args[1];
            return true;
          }
        }
      }
      keyType = default;
      valueType = default;
      return false;
    }

    public static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
    {
      var baseType = type;
      while (baseType != typeof(object))
      {
        var properties = baseType.GetProperties(BindingFlags.Public
          | BindingFlags.NonPublic
          | BindingFlags.Instance
          | BindingFlags.Static
          | BindingFlags.DeclaredOnly
        );

        foreach (var property in properties)
        {
          // if the property is virtual, only add the base-most definition so
          // overridden properties aren't duplicated in the list.
          var setMethod = property.GetSetMethod(true);

          if (setMethod is null
            || !setMethod.IsVirtual
            || setMethod == setMethod.GetBaseDefinition())
          {
            yield return property;
          }
        }

        baseType = baseType.BaseType;
      }
    }
  }
}

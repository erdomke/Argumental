using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;

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

    public static bool TryGetDataType(Type type, out IDataType dataType)
    {
      return TryGetDataType(type, null, out dataType);
    }

    private static bool TryGetDataType(Type type, Type nullableType, out IDataType dataType)
    {
      dataType = null;
      if (type == typeof(object))
        dataType = new AnyType();
      else if (type == typeof(bool))
        dataType = new BooleanType(nullableType ?? type);
      else if (type == typeof(string) || type.IsEnum)
        dataType = new StringType(nullableType ?? type);
      else if (type == typeof(byte)
        || type == typeof(sbyte)
        || type == typeof(short)
        || type == typeof(ushort)
        || type == typeof(int)
        || type == typeof(uint)
        || type == typeof(long)
        || type == typeof(ulong)
        || type == typeof(BigInteger)
        || type == typeof(float)
        || type == typeof(double)
        || type == typeof(decimal))
        dataType = new NumberType(nullableType ?? type);
      else if (type.IsArray && TryGetDataType(type.GetElementType(), null, out IDataType arrayElement))
      {
        if (type.GetArrayRank() == 1)
          dataType = new ArrayType(type, arrayElement);
      }
      else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        return TryGetDataType(Nullable.GetUnderlyingType(type), type, out dataType);
      else
      {
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
              var args = iface.GetGenericArguments();
              if (!TryGetDataType(args[0], null, out IDataType keyType)
                || !TryGetDataType(args[1], null, out IDataType valueType))
                return false;
              dataType = new DictionaryType(type, keyType, valueType);
              return true;
            }
            else if (defn == typeof(IEnumerable<>))
            {
              if (!TryGetDataType(type.GetGenericArguments()[0], null, out IDataType enumElement))
                return false;
              dataType = new ArrayType(type, enumElement);
              return true;
            }
          }
        }

        var converter = TypeDescriptor.GetConverter(type);
        if (converter.CanConvertFrom(typeof(string)))
          dataType = new StringType(nullableType ?? type);
      }
      return dataType != null;
    }
  }
}

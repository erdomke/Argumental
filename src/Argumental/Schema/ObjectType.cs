using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Argumental
{
  internal class ObjectType : IDataType
  {
    public Type Type { get; }

    public bool IsConvertibleFromString => false;

    public ObjectType(Type type)
    {
      Type = type;
    }

    public bool TryGetExample(IProperty property, out object example)
    {
      throw new NotImplementedException();
    }

    public IEnumerable<PropertyInfo> GetAllProperties()
    {
      var baseType = Type;
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

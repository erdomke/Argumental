using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Argumental
{
  internal class ObjectType : IDataType, ISchemaProvider
  {
    private IEnumerable<IProperty> _properties;

    public Type Type { get; }

    public bool IsConvertibleFromString => false;

    public IEnumerable<IProperty> Properties
    {
      get
      {
        if (_properties == null)
          _properties = GetAllProperties()
            .Select(p => new Property(Array.Empty<IConfigSection>(), p))
            .ToList();
        return _properties;
      }
    }

    public ObjectType(Type type)
    {
      Type = type;
    }

    public bool TryGetExample(IProperty property, out object example)
    {
      throw new NotImplementedException();
    }

    private IEnumerable<PropertyInfo> GetAllProperties()
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

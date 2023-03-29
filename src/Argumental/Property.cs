using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Argumental
{
  internal class Property : IProperty
  {
    private List<Attribute> _attributes = new List<Attribute>();

    public ConfigPath Name { get; }

    public IDataType Type { get; }

    public IEnumerable<Attribute> Attributes => _attributes;

    public Property(ConfigPath path, IDataType type)
    {
      Name = path;
      Type = type;
    }

    public Property(ConfigPath newName, IProperty clone)
    {
      _attributes.AddRange(clone.Attributes);
      Name = newName;
      Type = clone.Type;
    }

    public Property(IEnumerable<IConfigSection> parents, PropertyInfo property)
    {
      _attributes = property.GetCustomAttributes().ToList();
      if (!Reflection.TryGetDataType(property.PropertyType, out var dataType))
        dataType = new ObjectType(property.PropertyType);

      Name = new ConfigPath(parents)
      {
        new ConfigSection(property.Name)
      };
      Type = dataType;
    }

    public void AddAttribute(Attribute attribute)
    {
      _attributes.Add(attribute);
    }

    public static IEnumerable<IProperty> Flatten(IEnumerable<IProperty> properties, SerializationInfo info, bool allowSimpleLists)
    {
      var propList = new List<IProperty>();
      FlattenList(Array.Empty<IConfigSection>()
        , properties.Where(p => info.Use(p) < PropertyUse.Hidden)
        , allowSimpleLists
        , propList);
      return propList
        .OrderBy(p => {
          var use = info.Use(p);
          return use == PropertyUse.Required ? -1 : (int)use;
        })
        .ThenBy(p => info.Order(p))
        .ThenBy(p => info.Name(p));
    }

    internal static void FlattenList(IEnumerable<IConfigSection> path
      , IEnumerable<IProperty> properties
      , bool allowSimpleLists
      , List<IProperty> result)
    {
      foreach (var property in properties)
      {
        if (property.Type.IsConvertibleFromString
          || (property.Type is ArrayType simpleList
            && simpleList.ValueType.IsConvertibleFromString
            && allowSimpleLists))
        {
          result.Add(path.Any()
            ? new Property(new ConfigPath(path.Concat(property.Name)), property)
            : property);
        }
        else if (property.Type is ObjectType objectType)
        {
          FlattenList(new ConfigPath(path.Concat(property.Name)), objectType.Properties, allowSimpleLists, result);
        }
        else
        {
          var valueType = property.Type;
          var newPath = new ConfigPath(path.Concat(property.Name));
          var count = 0;
          while (true)
          {
            count++;
            if (valueType is ArrayType arrayType)
            {
              newPath.Add(new AnyInteger());
              valueType = arrayType.ValueType;
            }
            else if (valueType is DictionaryType dictionaryType)
            {
              newPath.Add(dictionaryType.KeyType is NumberType numberType && numberType.IsInteger
                ? (IConfigSection)new AnyInteger()
                : new AnyString());
              valueType = dictionaryType.ValueType;
            }
            else
            {
              count--;
              break;
            }
          }

          if (count == 0)
            throw new InvalidOperationException("Unsupported data type");

          var newProp = new Property(newPath, valueType);
          foreach (var attr in property.Attributes)
            newProp.AddAttribute(attr);
          result.Add(newProp);
        }
      }
    }
  }
}

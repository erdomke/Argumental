using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Argumental
{
  internal class Property : IProperty
  {
    public ConfigPath Name { get; }

    public bool IsPositional => false;

    public IDataType Type { get; }

    public IEnumerable<Attribute> Attributes { get; }

    public bool Hidden { get; set; }

    public bool MaskValue { get; set; }

    public object DefaultValue { get; set; }

    public PropertyUse Use { get; set; }

    public int Order { get; set; }
    
    public Property(ConfigPath path, IDataType type)
    {
      Name = path;
      Type = type;
      Attributes = Array.Empty<Attribute>();
    }

    public Property(ConfigPath newName, IProperty clone)
    {
      Attributes = clone.Attributes;
      DefaultValue = clone.DefaultValue;
      MaskValue = clone.MaskValue;
      Name = newName;
      Order = clone.Order;
      Type = clone.Type;
      Use = clone.Use;
    }

    public Property(IEnumerable<IConfigSection> parents, PropertyInfo property)
    {
      var attributes = property.GetCustomAttributes().ToList();
      var configKey = attributes.OfType<ConfigurationKeyNameAttribute>().FirstOrDefault();
      var displayAttr = attributes.OfType<DisplayAttribute>().FirstOrDefault();
      var descripAttr = attributes.OfType<DescriptionAttribute>().FirstOrDefault();
      var password = attributes.OfType<PasswordPropertyTextAttribute>().FirstOrDefault();
      var dataFormat = attributes.OfType<DataTypeAttribute>().FirstOrDefault();
      var defaultValue = attributes.OfType<DefaultValueAttribute>().FirstOrDefault();

      if (!Reflection.TryGetDataType(property.PropertyType, out var dataType))
        dataType = new ObjectType(property.PropertyType);

      if (attributes.Any(a => a is RequiredAttribute
        || a.GetType().FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute"
        || a.GetType().FullName == "System.Text.Json.Serialization.JsonRequiredAttribute"))
        Use = PropertyUse.Required;
      else if (attributes.OfType<ObsoleteAttribute>().Any(a => a.IsError))
        Use = PropertyUse.Prohibited;
      else if (attributes.OfType<ObsoleteAttribute>().Any())
        Use = PropertyUse.Obsolete;
      else if (attributes.OfType<BrowsableAttribute>().Any(a => !a.Browsable)
        || attributes.OfType<EditorBrowsableAttribute>().Any(a => a.State == EditorBrowsableState.Never)
        || attributes.Any(a => a is JsonIgnoreAttribute || a is XmlIgnoreAttribute))
        Use = PropertyUse.Hidden;
      else
        Use = PropertyUse.Optional;

      if (displayAttr != null && displayAttr.GetOrder().HasValue)
        Order = displayAttr.GetOrder().Value;
      else
        Order = attributes.OfType<JsonPropertyOrderAttribute>()
          .FirstOrDefault()?.Order ?? 0;

      Name = new ConfigPath(parents)
      {
        new ConfigSection(configKey?.Name ?? property.Name)
        {
          Description = displayAttr?.Description ?? descripAttr?.Description
        }
      };
      DefaultValue = defaultValue?.Value;
      MaskValue = property.PropertyType == typeof(SecureString)
        || password?.Password == true
        || dataFormat?.DataType == DataType.Password
        || dataFormat?.DataType == DataType.CreditCard;
      Type = dataType;
      Attributes = attributes;
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

          result.Add(new Property(newPath, valueType)
          {
            Use = property.Use,
            Order = property.Order,
            MaskValue = property.MaskValue
          }) ;
        }
      }
    }

    internal static IEnumerable<IProperty> DefaultSort(IEnumerable<IProperty> properties)
    {
      return properties //.OrderBy(p => string.Join(".", p.Name.Take(p.Name.Count - 1)), StringComparer.OrdinalIgnoreCase)
        .OrderBy(p => p.Use == PropertyUse.Required ? -1 : (int)p.Use)
        .ThenBy(p => p.Order)
        .ThenBy(p => p.Name.Last().ToString(), StringComparer.OrdinalIgnoreCase);
    }
  }
}

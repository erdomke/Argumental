using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Security;

namespace Argumental
{
  internal class Property : IProperty
  {
    public ConfigPath Name { get; }

    public bool IsPositional => false;

    public IDataType Type { get; }

    public IEnumerable<ValidationAttribute> Validations { get; }

    public bool Hidden { get; set; }

    public bool MaskValue { get; set; }

    public object DefaultValue { get; set; }

    public Property(ConfigPath path, IDataType type)
    {
      Name = path;
      Type = type;
      Validations = Array.Empty<ValidationAttribute>();
    }

    public Property(ConfigPath newName, IProperty clone)
    {
      DefaultValue = clone.DefaultValue;
      Hidden = clone.Hidden;
      MaskValue = clone.MaskValue;
      Name = newName;
      Type = clone.Type;
      Validations = clone.Validations;
    }

    public Property(IEnumerable<IConfigSection> parents, PropertyInfo property)
    {
      var configKey = property.GetCustomAttribute<ConfigurationKeyNameAttribute>();
      var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
      var descripAttr = property.GetCustomAttribute<DescriptionAttribute>();
      var browsable = property.GetCustomAttribute<BrowsableAttribute>();
      var editorBrowsable = property.GetCustomAttribute<EditorBrowsableAttribute>();
      var password = property.GetCustomAttribute<PasswordPropertyTextAttribute>();
      var dataFormat = property.GetCustomAttribute<DataTypeAttribute>();
      var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();

      if (!Reflection.TryGetDataType(property.PropertyType, out var dataType))
        dataType = new ObjectType(property.PropertyType);

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
      Hidden = browsable?.Browsable == false
        || editorBrowsable?.State == EditorBrowsableState.Never;
      Type = dataType;
      Validations = property.GetCustomAttributes().OfType<ValidationAttribute>().ToList();
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
            Hidden = property.Hidden,
            MaskValue = property.MaskValue
          });
        }
      }
    }
  }
}

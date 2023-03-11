using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;

namespace Argumental
{
  public class OptionSet<T> : IOptionProvider<T>
  {
    private ConfigPath _parents;

    public IEnumerable<IProperty> Properties
    {
      get
      {
        var result = new List<IProperty>();
        BuildPropertyList(new Property()
        {
          Path = _parents,
          Type = typeof(T)
        }, result);
        return result;
      }
    }

    public OptionSet(params ConfigSection[] parents)
    {
      _parents = new ConfigPath(parents);
    }

    private void BuildPropertyList(IProperty property, List<IProperty> properties)
    {
      if (Reflection.IsConvertibleFromString(property.Type))
      {
        properties.Add(property);
      }
      else if (Reflection.TryGetListType(property.Type, out var elementType))
      {
        properties.Add(property);
        BuildPropertyList(new Property()
        {
          Path = new ConfigPath(property.Path)
          {
            new AnyListIndex()
          },
          Type = elementType
        }, properties);
      }
      else if (Reflection.TryGetDictionaryType(property.Type, out var _, out elementType))
      {
        properties.Add(property);
        BuildPropertyList(new Property()
        {
          Path = new ConfigPath(property.Path)
          {
            new AnyDictKey()
          },
          Type = elementType
        }, properties);
      }
      else
      {
        foreach (var prop in typeof(T).GetAllProperties())
        {
          BuildPropertyList(GetProperty(property.Path, prop), properties);
        }
      }
    }

    public IProperty GetProperty(IEnumerable<IConfigSection> parents, PropertyInfo property)
    {
      var configKey = property.GetCustomAttribute<ConfigurationKeyNameAttribute>();
      var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
      var descripAttr = property.GetCustomAttribute<DescriptionAttribute>();
      var browsable = property.GetCustomAttribute<BrowsableAttribute>();
      var editorBrowsable = property.GetCustomAttribute<EditorBrowsableAttribute>();
      var password = property.GetCustomAttribute<PasswordPropertyTextAttribute>();
      var dataFormat = property.GetCustomAttribute<DataTypeAttribute>();
      var defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();

      return new Property
      {
        Path = new ConfigPath(parents)
        {
          new ConfigSection(configKey?.Name ?? property.Name)
          {
            Description = displayAttr?.Description ?? descripAttr?.Description
          }
        },
        DefaultValue = defaultValue?.Value,
        MaskValue = property.PropertyType == typeof(SecureString)
          || password?.Password == true
          || dataFormat?.DataType == DataType.Password,
        Hidden = browsable?.Browsable == false
          || editorBrowsable?.State == EditorBrowsableState.Never,
        Type = property.PropertyType,
        Validations = property.GetCustomAttributes().OfType<ValidationAttribute>().ToList()
      };
    }

    public bool TryGet(IConfiguration configuration, List<ValidationResult> validationResults, out T value)
    {
      var config = configuration;
      if (_parents?.Count > 0)
        config = config.GetSection(_parents.ToString());
      value = config.Get<T>();
      if (value == null)
        value = Activator.CreateInstance<T>();
      return Validator.TryValidateObject(value, new ValidationContext(value), validationResults, true);
    }

    bool IOptionProvider.TryGet(IConfiguration configuration, List<ValidationResult> validationResults, out object value)
    {
      var result = TryGet(configuration, validationResults, out var typed);
      value = typed;
      return result;
    }

    private class Property : IProperty
    {
      public ConfigPath Path { get; set; }

      public bool IsPositional => false;

      public Type Type { get; set; }

      public IEnumerable<ValidationAttribute> Validations { get; set; } = Array.Empty<ValidationAttribute>();

      public bool Hidden { get; set;}

      public bool MaskValue { get; set; }

      public object DefaultValue { get; set; }
    }
  }
}

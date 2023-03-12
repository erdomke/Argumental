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
  public class Option<T> : IOptionProvider<T>, IProperty
  {
    private bool _isPositional;
    private IDataType _type;
    private List<ValidationAttribute> _validators = new List<ValidationAttribute>();

    public T DefaultValue { get; set; }

    public ConfigPath Path { get; }

    public bool Hidden { get; set; }

    public bool MaskValue { get; set; }

    public IList<ValidationAttribute> Validations => _validators;

    public IEnumerable<IProperty> Properties
    {
      get
      {
        var result = new List<IProperty>();
        BuildPropertyList(this, result);
        return result;
      }
    }

    object IProperty.DefaultValue => DefaultValue;

    bool IProperty.IsPositional => _isPositional;

    IDataType IProperty.Type => _type;

    IEnumerable<ValidationAttribute> IProperty.Validations => _validators;

    public Option(string name, string description = null, bool isPositional = false)
      : this(new ConfigSection(name, description))
    {
      _isPositional = isPositional;
    }

    public Option(params ConfigSection[] parents)
    {
      Path = new ConfigPath(parents);
      if (Reflection.TryGetDataType(typeof(T), out var dataType))
        _type = dataType;
      else
        _type = new ObjectType(typeof(T));
    }

    private void BuildPropertyList(IProperty property, List<IProperty> properties)
    {
      if (property.Type.IsConvertibleFromString)
      {
        properties.Add(property);
      }
      else if (property.Type is ArrayType arrayType)
      {
        if (arrayType.ValueType.IsConvertibleFromString)
          properties.Add(property);
        BuildPropertyList(new Property()
        {
          Path = new ConfigPath(property.Path)
          {
            new AnyListIndex()
          },
          Type = arrayType.ValueType
        }, properties);
      }
      else if (property.Type is DictionaryType dictionaryType)
      {
        properties.Add(property);
        BuildPropertyList(new Property()
        {
          Path = new ConfigPath(property.Path)
          {
            new AnyDictKey()
          },
          Type = dictionaryType.ValueType
        }, properties);
      }
      else if (property.Type is ObjectType objectType)
      {
        foreach (var prop in objectType.GetAllProperties())
        {
          BuildPropertyList(GetProperty(property.Path, prop), properties);
        }
      }
      else
      {
        throw new InvalidOperationException($"Invalid type {property.Type.GetType().Name}");
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

      if (!Reflection.TryGetDataType(property.PropertyType, out var dataType))
        dataType = new ObjectType(property.PropertyType);

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
          || dataFormat?.DataType == DataType.Password
          || dataFormat?.DataType == DataType.CreditCard,
        Hidden = browsable?.Browsable == false
          || editorBrowsable?.State == EditorBrowsableState.Never,
        Type = dataType,
        Validations = property.GetCustomAttributes().OfType<ValidationAttribute>().ToList()
      };
    }

    public bool TryGet(IConfiguration configuration, List<ValidationResult> validationResults, out T value)
    {
      if (_type.IsConvertibleFromString)
      {
        value = configuration.GetValue(Path.ToString(), DefaultValue);
        return Validator.TryValidateValue(value, new ValidationContext(this), validationResults, Validations);
      }
      else
      {
        var config = configuration;
        if (Path?.Count > 0)
          config = config.GetSection(Path.ToString());
        value = config.Get<T>();
        if (value == null)
          value = Activator.CreateInstance<T>();
        return Validator.TryValidateObject(value, new ValidationContext(value), validationResults, true);
      }
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

      public IDataType Type { get; set; }

      public IEnumerable<ValidationAttribute> Validations { get; set; } = Array.Empty<ValidationAttribute>();

      public bool Hidden { get; set;}

      public bool MaskValue { get; set; }

      public object DefaultValue { get; set; }
    }

    private class ObjectType : IDataType
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
}

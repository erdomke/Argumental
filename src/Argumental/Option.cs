using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Argumental
{
  public class Option<T> : IOptionProvider<T>, IProperty
  {
    private bool _isPositional;
    private IDataType _type;
    private List<ValidationAttribute> _validators = new List<ValidationAttribute>();

    public T DefaultValue { get; set; }

    public ConfigPath Name { get; }

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
      Name = new ConfigPath(parents);
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
        BuildPropertyList(new Property(
          new ConfigPath(property.Name)
          {
            new AnyListIndex()
          }
          , arrayType.ValueType
        ), properties);
      }
      else if (property.Type is DictionaryType dictionaryType)
      {
        properties.Add(property);
        BuildPropertyList(new Property(
          new ConfigPath(property.Name) 
          { 
            new AnyDictKey() 
          }
        , dictionaryType.ValueType
        ), properties);
      }
      else if (property.Type is ObjectType objectType)
      {
        foreach (var prop in objectType.GetAllProperties())
        {
          BuildPropertyList(new Property(property.Name, prop), properties);
        }
      }
      else
      {
        throw new InvalidOperationException($"Invalid type {property.Type.GetType().Name}");
      }
    }

    public bool TryGet(IConfiguration configuration, List<ValidationResult> validationResults, out T value)
    {
      if (_type.IsConvertibleFromString)
      {
        try
        {
          value = configuration.GetValue(Name.ToString(), DefaultValue);
          return Validator.TryValidateValue(value, new ValidationContext(this)
          {
            MemberName = Name.ToString(),
          }, validationResults, Validations);
        }
        catch (InvalidOperationException ex)
        {
          if (validationResults != null)
          {
            var messages = new List<string>();
            var curr = (Exception)ex;
            while (curr != null)
            {
              messages.Add(curr.Message);
              curr = curr.InnerException;
            }
            validationResults.Add(new ValidationResult(string.Join(" ", messages), new[] { Name.ToString() }));
          }
          value = default;
          return false;
        }
      }
      else
      {
        var config = configuration;
        if (Name?.Count > 0)
          config = config.GetSection(Name.ToString());
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
  }
}

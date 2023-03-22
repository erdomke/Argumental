using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

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
        if (!Name.Any() && _type is ObjectType objectType)
          return objectType.Properties;
        else
          return new[] { this };
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
        throw new NotSupportedException("Use an OptionGroup for complex options");
    }

    public T Get(InvocationContext context)
    {
      var validationResults = new List<ValidationResult>();
      if (_type.IsConvertibleFromString)
      {
        try
        {
          
          var value = context.Configuration.GetValue(Name.ToString(), DefaultValue);
          if (!Validator.TryValidateValue(value, new ValidationContext(this)
          {
            MemberName = Name.ToString(),
          }, validationResults, Validations))
            context.AddErrors(validationResults);
          return value;
        }
        catch (InvalidOperationException ex)
        {
          context.AddError(ex);
          return default;
        }
      }
      else
      {
        var config = context.Configuration;
        if (Name?.Count > 0)
          config = config.GetSection(Name.ToString());
        var value = config.Get<T>();
        if (value == null)
          value = Activator.CreateInstance<T>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), validationResults, true))
          context.AddErrors(validationResults);
        return value;
      }
    }

    object IOptionProvider.Get(InvocationContext context)
    {
      return Get(context);
    }
  }
}

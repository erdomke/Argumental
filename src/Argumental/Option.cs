using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Argumental
{
  public class Option<T> : IOptionProvider<T>, IProperty
  {
    private List<ValidationAttribute> _validators = new List<ValidationAttribute>();
    private Func<string, T> _converter;

    public T DefaultValue { get; set; }

    public IEnumerable<IProperty> Properties => new[] { this };

    public ConfigPath Path { get; }

    public bool IsPositional { get; }

    public IDataType Type { get; }

    public IEnumerable<ValidationAttribute> Validations => _validators;

    public bool Hidden { get; set; }

    public bool MaskValue { get; set; }

    object IProperty.DefaultValue => DefaultValue;

    public Option(string name = "", string description = null, bool isPositional = false)
    {
      Path = new ConfigSection(name, description);
      IsPositional = isPositional;

      if (Reflection.TryGetDataType(typeof(T), out var dataType)
        && dataType.IsConvertibleFromString)
      {
        Type = dataType;
      }
      else
      {
        var stringConstructor = typeof(T).GetConstructors().FirstOrDefault(c =>
        {
          var args = c.GetParameters();
          return args.Length == 1 && args[0].ParameterType == typeof(string);
        });
        if (stringConstructor == null)
          throw new InvalidOperationException($"Cannot have an option of type {typeof(T).FullName}");
        Type = new StringType(typeof(T));
        _converter = s => (T)stringConstructor.Invoke(new object[] { s });
      }
    }

    public bool TryGet(IConfiguration configuration, List<ValidationResult> validationResults, out T value)
    {
      if (_converter == null)
      {
        value = configuration.GetValue(Path.ToString(), DefaultValue);
      }
      else
      {
        var str = configuration.GetSection(Path.ToString()).Value;
        if (str == null)
          value = DefaultValue;
        else
          value = _converter(str);
      }

      return Validator.TryValidateValue(value, new ValidationContext(this)
      {
        MemberName = Path.Last().ToString()
      }, validationResults, Validations);
    }

    bool IOptionProvider.TryGet(IConfiguration configuration, List<ValidationResult> validationResults, out object value)
    {
      var result = TryGet(configuration, validationResults, out var typed);
      value = typed;
      return result;
    }
  }
}

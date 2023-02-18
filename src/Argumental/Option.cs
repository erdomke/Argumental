using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Argumental
{
  internal class Option<T> : IOptionProvider<T>, IProperty
  {
    private List<ValidationAttribute> _validators = new List<ValidationAttribute>();
    private Func<string, T> _converter;

    public T DefaultValue { get; set; }

    public IEnumerable<IProperty> Properties => new[] { this };

    public ConfigPath Path { get; }

    public bool IsPositional { get; }

    public Type Type => typeof(T);

    public IEnumerable<ValidationAttribute> Validations => _validators;

    public bool Hidden { get; set; }

    public bool MaskValue { get; set; }

    object IProperty.DefaultValue => DefaultValue;

    public Option(string name = "", string description = null, bool isPositional = false)
    {
      Path = new ConfigSection(name, description);
      IsPositional = isPositional;

      var type = typeof(T);
      if (!Reflection.IsConvertibleFromString(type))
      {
        var stringConstructor = type.GetConstructors().FirstOrDefault(c =>
        {
          var args = c.GetParameters();
          return args.Length == 1 && args[0].ParameterType == typeof(string);
        });
        if (stringConstructor == null)
          throw new InvalidOperationException($"Cannot have an option of type {type.FullName}");
        _converter = s => (T)stringConstructor.Invoke(new object[] { s });
      }
    }

    public T Get(IConfiguration configuration)
    {
      if (_converter == null)
        return configuration.GetValue<T>(Path.ToString(), DefaultValue);

      var value = configuration.GetSection(Path.ToString()).Value;
      if (value == null)
        return DefaultValue;
      else
        return _converter(value);
    }

    object IOptionProvider.Get(IConfiguration configuration)
    {
      return Get(configuration);
    }
  }
}

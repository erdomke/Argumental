using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Argumental
{
  public class Option<T> : IOptionProvider<T>, IProperty
  {
    private bool _isPositional;
    private IDataType _type;
    private List<Attribute> _attributes = new List<Attribute>();

    public ConfigPath Name { get; }
    public IList<Attribute> Attributes => _attributes;

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

    IDataType IProperty.Type => _type;

    IEnumerable<Attribute> IProperty.Attributes => _attributes;

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

    public Option<T> SetDefaultValue(T defaultValue)
    {
      _attributes.RemoveWhere(a => a is DefaultValueAttribute);
      if (defaultValue != null)
        _attributes.Add(new DefaultValueAttribute(typeof(T), defaultValue.ToString()));
      return this;
    }

    public Option<T> SetPositional(bool positional)
    {
      var isPositional = _attributes.OfType<PositionalAttribute>().Any();
      if (isPositional && !positional)
        _attributes.RemoveWhere(a => a is PositionalAttribute);
      else if (!isPositional && positional)
        _attributes.Add(new PositionalAttribute());
      return this;
    }

    public T Get(InvocationContext context)
    {
      var validationResults = new List<ValidationResult>();
      if (_type.IsConvertibleFromString)
      {
        try
        {
          var defaultValue = Attributes.OfType<DefaultValueAttribute>().FirstOrDefault()?.Value
            ?? default(T);
          var value = context.Configuration.GetValue(Name.ToString(), (T)defaultValue);
          if (!Validator.TryValidateValue(value, new ValidationContext(this)
          {
            MemberName = Name.ToString(),
          }, validationResults, Attributes.OfType<ValidationAttribute>()))
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

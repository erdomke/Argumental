using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Argumental
{
  public class OptionGroup<T> : IOptionProvider<T>, IOptionGroup, IValidateOptions<T> where T : class
  {
    private IDataType _type;

    public ConfigPath Name { get; }

    public IEnumerable<IProperty> Properties
    {
      get
      {
        var result = new List<IProperty>();
        Property.BuildPropertyList(new Property(Name, _type), result);
        return result;
      }
    }

    public OptionGroup(params ConfigSection[] parents)
    {
      Name = new ConfigPath(parents);
      _type = new ObjectType(typeof(T));
    }

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
      var config = configuration;
      if (Name?.Count > 0)
        config = config.GetSection(Name.ToString());
      services.Configure<T>(config);
      services.TryAddSingleton<IValidateOptions<T>>(this);
    }

    ValidateOptionsResult IValidateOptions<T>.Validate(string name, T options)
    {
      var validationResults = new List<ValidationResult>();
      if (Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true))
        return ValidateOptionsResult.Success;
      return ValidateOptionsResult.Fail(validationResults.Select(r => r.ErrorMessage));
    }

    public T Get(InvocationContext context)
    {
      var config = context.Configuration;
      if (Name?.Count > 0)
        config = config.GetSection(Name.ToString());
      var value = config.Get<T>();
      if (value == null)
        value = Activator.CreateInstance<T>();
      var validation = ((IValidateOptions<T>)this).Validate(string.Empty, value);
      if (validation.Failed)
        context.AddErrors(validation.Failures);
      return value;
    }

    object IOptionProvider.Get(InvocationContext context)
    {
      return Get(context);
    }
  }
}

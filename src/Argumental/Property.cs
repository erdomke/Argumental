﻿using Microsoft.Extensions.Configuration;
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
  }
}

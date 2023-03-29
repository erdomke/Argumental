using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Xml.Linq;

namespace Argumental
{
  public abstract class SerializationInfo
  {
    public virtual IEnumerable<ValidationAttribute> AdditionalValidations(IProperty property)
    {
      return Array.Empty<ValidationAttribute>();
    }

    public virtual object DefaultValue(IProperty property)
    {
      return property.Attributes.OfType<DefaultValueAttribute>().FirstOrDefault()?.Value;
    }

    public virtual string Description(IProperty property)
    {
      var displayAttr = property.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
      var descripAttr = property.Attributes.OfType<DescriptionAttribute>().FirstOrDefault();
      return displayAttr?.GetDescription() 
        ?? descripAttr?.Description
        ?? property.Name.OfType<ConfigSection>().LastOrDefault()?.Description
        ?? property.Type.Name?.Description;
    }

    public virtual bool MaskValue(IProperty property)
    {
      var attributes = property.Attributes.Concat(AdditionalValidations(property)).ToList();
      var dataFormat = attributes.OfType<DataTypeAttribute>().FirstOrDefault();
      return attributes.OfType<MaskValueAttribute>().Any()
        || property.Type.Type == typeof(SecureString)
        || attributes.OfType<PasswordPropertyTextAttribute>().FirstOrDefault()?.Password == true
        || dataFormat?.DataType == DataType.Password
        || dataFormat?.DataType == DataType.CreditCard;
    }

    public virtual double? MultipleOf(IProperty property)
    {
      return null;
    }

    public virtual string Name(IProperty property)
    {
      return string.Join(":", ConfigurationName(property));
    }

    public virtual int Order(IProperty property)
    {
      return property.Attributes.OfType<DisplayAttribute>().FirstOrDefault()?.GetOrder() ?? 0;
    }

    public virtual string Prompt(IProperty property)
    {
      var display = property.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
      return display?.GetPrompt() ?? display?.GetName() ?? property.Name.OfType<ConfigSection>().LastOrDefault()?.Name;
    }

    public string RegularExpression(IProperty property)
    {
      if (property.Type is StringType)
        return property.Attributes
          .Concat(AdditionalValidations(property))
          .OfType<RegularExpressionAttribute>()
          .FirstOrDefault()?.Pattern;
      return null;
    }

    public DataType StringFormat(StringType stringType)
    {
      if (stringType.Type == typeof(DateTime)
        || stringType.Type == typeof(DateTimeOffset))
        return DataType.DateTime;
      else if (stringType.Type.FullName == "System.DateOnly")
        return DataType.Date;
      else if (stringType.Type.FullName == "System.TimeOnly")
        return DataType.Time;
      else if (stringType.Type == typeof(TimeSpan))
        return DataType.Duration;
      else if (stringType.Type == typeof(Uri))
        return DataType.Url;
      return DataType.Text;
    }

    public DataType StringFormat(IProperty property)
    {
      var dataTypeAttr = property.Attributes
        .Concat(AdditionalValidations(property))
        .OfType<DataTypeAttribute>()
        .FirstOrDefault();
      if (dataTypeAttr != null)
        return dataTypeAttr.DataType;
      else if (property.Attributes.OfType<PasswordPropertyTextAttribute>().FirstOrDefault()?.Password == true)
        return DataType.Password;
      else if (property.Type is StringType stringType)
        return StringFormat(stringType);
      return DataType.Custom;
    }

    public bool TryGetEnumeration(IProperty property, out bool allowMultiple, out IEnumerable<IEnumerationValue> values)
    {
      if (property.Type.Type.IsEnum)
      {
        allowMultiple = property.Type.Type.GetCustomAttribute<FlagsAttribute>() != null;
        values = property.Type.Type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
          .Select(f => new EnumerationValue(f))
          .ToList();
        return true;
      }
      else
      {
        allowMultiple = false;
        values = null;
        return false;
      }
    }

    public object Example(IProperty property)
    {
      var defaultValue = DefaultValue(property);
      if (defaultValue != null && !(defaultValue is string str && str == ""))
        return defaultValue;

      if (property.Type is StringType stringType)
      {
        var format = StringFormat(property);
        switch (format)
        {
          case DataType.DateTime:
            return "2012-03-14T13:30:55";
          case DataType.Date:
            return "2012-03-14";
          case DataType.Time:
            return "13:30:55";
          case DataType.Duration:
            return "14.13:30:55.600";
          case DataType.Url:
            return "https://www.example.com";
          case DataType.PhoneNumber:
            return "+15555555555";
          case DataType.EmailAddress:
            return property.Name.OfType<ConfigSection>().Last().Name + "@example.com";
          case DataType.CreditCard:
            return "5555555555555555";
          default:
            if (TryGetEnumeration(property, out var allowMultiple, out var values))
            {
              if (allowMultiple)
                return string.Join(", ", values.Take(3).Select(e => e.Name));
              else
                return values.First()?.Name;
            }

            var fileExtensions = property.Attributes.OfType<FileExtensionsAttribute>().FirstOrDefault();
            if (fileExtensions != null)
              return property.Name.OfType<ConfigSection>().Last().Name + "." + fileExtensions.Extensions.Split(',')[0].TrimStart('.');

            return property.Name.OfType<ConfigSection>().Last().Name;
        }
      }
      else if (property.Type is NumberType numberType)
      {
        if (!TryGetNumberRange(property, out var minimumObj, out var minimumExclusive, out var maximumObj, out var maximumExclusive))
        {
          minimumObj = null;
          maximumObj = null;
        }

        var minimum = Convert.ToDouble(minimumObj ?? (numberType.IsSigned ? double.MinValue : 0.0));
        var maximum = Convert.ToDouble(maximumObj ?? double.MaxValue);

        if (minimum > double.MinValue && maximum < double.MaxValue)
        {
          return Convert.ChangeType((maximum + minimum) / 2, numberType.Type);
        }
        else if (maximum < double.MaxValue)
        {
          if (maximum > 0)
            return Convert.ChangeType(maximum / 2, numberType.Type);
          else
            return Convert.ChangeType(maximum - 3.14, numberType.Type);
        }
        else if (minimum > double.MinValue)
        {
          return Convert.ChangeType(minimum + 3.14, numberType.Type);
        }
        else
        {
          return Convert.ChangeType(3.14, numberType.Type);
        }
      }
      else if (property.Type is BooleanType)
      {
        return true;
      }
      else
      {
        return null;
      }
    }

    public bool TryGetNumberRange(IProperty property, out object minimum, out bool minimumExclusive, out object maximum, out bool maximumExclusive)
    {
      minimum = null;
      minimumExclusive = false;
      maximum = null;
      maximumExclusive = false;
      if (property.Type is NumberType)
      {
        var range = property.Attributes
          .Concat(AdditionalValidations(property))
          .OfType<RangeAttribute>()
          .FirstOrDefault();
        minimum = range?.Minimum;
        maximum = range?.Maximum;
      }
      return minimum != null || maximum != null;
    }

    public bool TryGetStringLength(IProperty property, out int? minLength, out int? maxLength)
    {
      minLength = null;
      maxLength = null;
      if (property.Type is StringType)
      {
        var attributes = property.Attributes.Concat(AdditionalValidations(property)).ToList();
        var stringLength = attributes.OfType<StringLengthAttribute>().FirstOrDefault();
        if (stringLength != null)
        {
          if (stringLength.MinimumLength > 0)
            minLength = stringLength.MinimumLength;
          if (stringLength.MaximumLength < int.MaxValue)
            maxLength = stringLength.MaximumLength;
        }
        else
        {
          var minLengthAttr = attributes.OfType<MinLengthAttribute>().FirstOrDefault();
          if (minLengthAttr?.Length > 0)
            minLength = minLengthAttr.Length;
          var maxLengthAttr = attributes.OfType<MaxLengthAttribute>().FirstOrDefault();
          if (maxLengthAttr?.Length >= 0 && maxLengthAttr?.Length < int.MaxValue)
            maxLength = maxLengthAttr.Length;
        }
      }
      return minLength.HasValue || maxLength.HasValue;
    }

    public bool TryGetListLength(IProperty property, out int? minLength, out int? maxLength)
    {
      minLength = null;
      maxLength = null;
      if (property.Type is ArrayType)
      {
        var attributes = property.Attributes.Concat(AdditionalValidations(property)).ToList();
        var minLengthAttr = attributes.OfType<MinLengthAttribute>().FirstOrDefault();
        if (minLengthAttr?.Length > 0)
          minLength = minLengthAttr.Length;
        var maxLengthAttr = attributes.OfType<MaxLengthAttribute>().FirstOrDefault();
        if (maxLengthAttr?.Length >= 0 && maxLengthAttr?.Length < int.MaxValue)
          maxLength = maxLengthAttr.Length;
      }
      return minLength.HasValue || maxLength.HasValue;
    }

    public PropertyUse Use(IProperty property)
    {
      return Use(property, out var _);
    }

    public PropertyUse Use(IProperty property, out string message)
    {
      message = null;
      var attributes = property.Attributes.Concat(AdditionalValidations(property)).ToList();
      if (attributes.Any(a => a is RequiredAttribute
        || a.GetType().FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute"))
        return PropertyUse.Required;

      var obsolete = attributes.OfType<ObsoleteAttribute>().FirstOrDefault();
      if (obsolete != null)
      {
        message = obsolete.Message;
        return obsolete.IsError ? PropertyUse.Prohibited : PropertyUse.Obsolete;
      }
      
      if (attributes.OfType<BrowsableAttribute>().Any(a => !a.Browsable)
        || attributes.OfType<EditorBrowsableAttribute>().Any(a => a.State == EditorBrowsableState.Never))
        return PropertyUse.Hidden;
      else
        return PropertyUse.Optional;
    }

    public IEnumerable<IConfigSection> ConfigurationName(IProperty property)
    {
      var configKey = property.Attributes
        .OfType<ConfigurationKeyNameAttribute>()
        .FirstOrDefault();
      var lastNameIndex = -1;
      if (configKey != null)
      {
        for (var i = property.Name.Count - 1; i >= 0; i--)
        {
          if (property.Name[i] is ConfigSection)
          {
            lastNameIndex = i;
            break;
          }
        }
      }
      return property.Name
        .Select((n, i) => i == lastNameIndex ? new ConfigSection(configKey.Name) : n);
    }

    public class EnumerationValue : IEnumerationValue
    {
      public bool Hidden { get; }
      public string Name { get; }
      public object Value { get; }
      public string Description { get; }

      public EnumerationValue(FieldInfo field)
      {
        var displayAttr = field.GetCustomAttribute<DisplayAttribute>();
        var descripAttr = field.GetCustomAttribute<DescriptionAttribute>();
        var browsable = field.GetCustomAttribute<BrowsableAttribute>();
        var editorBrowsable = field.GetCustomAttribute<EditorBrowsableAttribute>();
        Hidden = browsable?.Browsable == false
          || editorBrowsable?.State == EditorBrowsableState.Never;
        Name = field.Name;
        Value = field.GetRawConstantValue();
        Description = displayAttr?.GetDescription() ?? descripAttr?.Description;
      }
    }
  }
}

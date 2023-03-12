using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Argumental
{
  public class StringType : IDataType
  {
    public bool IsConvertibleFromString => true;
    public IEnumerable<EnumerationValue> Enumeration { get; }
    public Type Type { get; }

    public StringType(Type type = null)
    {
      Type = type ?? typeof(string);
      if (type.IsEnum)
      {
        Enumeration = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
          .Select(f => new EnumerationValue(f))
          .ToList();
      }
      else
      {
        Enumeration = Enumerable.Empty<EnumerationValue>();
      }
    }

    public bool TryGetExample(IProperty property, out object example)
    {
      example = null;
      if (property.DefaultValue != null)
        example = property.DefaultValue.ToString();
      else if (Type == typeof(DateTime)
        || Type == typeof(DateTimeOffset)
        || property.Validations.OfType<DataTypeAttribute>().Any(d => d.DataType == DataType.DateTime))
        example = "2012-03-14T13:30:55";
      else if (Type.FullName == "System.DateOnly"
        || property.Validations.OfType<DataTypeAttribute>().Any(d => d.DataType == DataType.Date))
        example = "2012-03-14";
      else if (Type.FullName == "System.TimeOnly"
        || property.Validations.OfType<DataTypeAttribute>().Any(d => d.DataType == DataType.Time))
        example = "13:30:55";
      else if (Type == typeof(TimeSpan)
        || property.Validations.OfType<DataTypeAttribute>().Any(d => d.DataType == DataType.Duration))
        example = "14.13:30:55.600";
      else if (Enumeration.Any())
      {
        var result = string.Join("|", Enumeration.Where(e => !e.Hidden).Take(6).Select(e => e.Name));
        if (Enumeration.Where(e => !e.Hidden).Skip(6).Any())
          result += "|...";
        example = result;
      }
      else if (Type == typeof(Uri) 
        || property.Validations.OfType<DataTypeAttribute>().Any(d => d.DataType == DataType.Url))
        example = "https://www.example.com";
      else if (property.Validations.OfType<DataTypeAttribute>().Any(d => d.DataType == DataType.PhoneNumber))
        example = "+15555555555";
      else if (property.Validations.OfType<DataTypeAttribute>().Any(d => d.DataType == DataType.EmailAddress))
        example = property.Path.OfType<ConfigSection>().Last().Name + "@example.com";
      else if (property.Validations.OfType<DataTypeAttribute>().Any(d => d.DataType == DataType.CreditCard))
        example = "5555555555555555";
      else
      {
        var fileExtensions = property.Validations.OfType<FileExtensionsAttribute>().FirstOrDefault();
        if (fileExtensions != null)
        {
          example = property.Path.OfType<ConfigSection>().Last().Name + "." + fileExtensions.Extensions.Split(',')[0].TrimStart('.');
        }
        else
        {
          example = property.Path.OfType<ConfigSection>().Last().Name;
          if (!Validator.TryValidateValue(example, new ValidationContext(this), null, property.Validations))
            return false;
        }
      }

      return example != null;
    }
  }
}

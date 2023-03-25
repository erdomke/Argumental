using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security;
using System.Xml.Linq;

namespace Argumental
{
  public abstract class SerializationInfo
  {
    public abstract IEnumerable<XElement> DocbookNames(IProperty property);

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

    public virtual IEnumerable<IProperty> Flatten(IEnumerable<IProperty> properties)
    {
      var propList = new List<IProperty>();
      FlattenList(Array.Empty<IConfigSection>()
        , properties.Where(p => Use(p) < PropertyUse.Hidden)
        , false
        , propList);
      return propList
        .OrderBy(p => {
          var use = Use(p);
          return use == PropertyUse.Required ? -1 : (int)use;
        })
        .ThenBy(p => Order(p))
        .ThenBy(p => Name(p));
    }

    protected void FlattenList(IEnumerable<IConfigSection> path
      , IEnumerable<IProperty> properties
      , bool allowSimpleLists
      , List<IProperty> result)
    {
      foreach (var property in properties)
      {
        if (property.Type.IsConvertibleFromString
          || (property.Type is ArrayType simpleList
            && simpleList.ValueType.IsConvertibleFromString
            && allowSimpleLists))
        {
          result.Add(path.Any()
            ? new Property(new ConfigPath(path.Concat(property.Name)), property)
            : property);
        }
        else if (property.Type is ObjectType objectType)
        {
          FlattenList(new ConfigPath(path.Concat(property.Name)), objectType.Properties, allowSimpleLists, result);
        }
        else
        {
          var valueType = property.Type;
          var newPath = new ConfigPath(path.Concat(property.Name));
          var count = 0;
          while (true)
          {
            count++;
            if (valueType is ArrayType arrayType)
            {
              newPath.Add(new AnyInteger());
              valueType = arrayType.ValueType;
            }
            else if (valueType is DictionaryType dictionaryType)
            {
              newPath.Add(dictionaryType.KeyType is NumberType numberType && numberType.IsInteger
                ? (IConfigSection)new AnyInteger()
                : new AnyString());
              valueType = dictionaryType.ValueType;
            }
            else
            {
              count--;
              break;
            }
          }

          if (count == 0)
            throw new InvalidOperationException("Unsupported data type");

          var newProp = new Property(newPath, valueType);
          foreach (var attr in property.Attributes)
            newProp.AddAttribute(attr);
          result.Add(newProp);
        }
      }
    }


    public virtual bool MaskValue(IProperty property)
    {
      var dataFormat = property.Attributes.OfType<DataTypeAttribute>().FirstOrDefault();
      return property.Attributes.OfType<MaskValueAttribute>().Any()
        || property.Type.Type == typeof(SecureString)
        || property.Attributes.OfType<PasswordPropertyTextAttribute>().FirstOrDefault()?.Password == true
        || dataFormat?.DataType == DataType.Password
        || dataFormat?.DataType == DataType.CreditCard;
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

    public virtual string RegularExpression(IProperty property)
    {
      if (property.Type is StringType)
        return property.Attributes.OfType<RegularExpressionAttribute>().FirstOrDefault()?.Pattern;
      return null;
    }

    public virtual bool TryGetNumberRange(IProperty property, out object minimum, out bool minimumExclusive, out object maximum, out bool maximumExclusive)
    {
      minimum = null;
      minimumExclusive = false;
      maximum = null;
      maximumExclusive = false;
      if (property.Type is NumberType)
      {
        var range = property.Attributes.OfType<RangeAttribute>().FirstOrDefault();
        minimum = range?.Minimum;
        maximum = range?.Maximum;
      }
      return minimum != null || maximum != null;
    }

    public virtual bool TryGetStringLength(IProperty property, out int? minLength, out int? maxLength)
    {
      minLength = null;
      maxLength = null;
      if (property.Type is StringType)
      {
        var stringLength = property.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
        if (stringLength != null)
        {
          if (stringLength.MinimumLength > 0)
            minLength = stringLength.MinimumLength;
          if (stringLength.MaximumLength < int.MaxValue)
            maxLength = stringLength.MaximumLength;
        }
        else
        {
          var minLengthAttr = property.Attributes.OfType<MinLengthAttribute>().FirstOrDefault();
          if (minLengthAttr?.Length > 0)
            minLength = minLengthAttr.Length;
          var maxLengthAttr = property.Attributes.OfType<MaxLengthAttribute>().FirstOrDefault();
          if (maxLengthAttr?.Length >= 0 && maxLengthAttr?.Length < int.MaxValue)
            maxLength = maxLengthAttr.Length;
        }
      }
      return minLength.HasValue || maxLength.HasValue;
    }

    public virtual bool TryGetListLength(IProperty property, out int? minLength, out int? maxLength)
    {
      minLength = null;
      maxLength = null;
      if (property.Type is ArrayType)
      {
        var minLengthAttr = property.Attributes.OfType<MinLengthAttribute>().FirstOrDefault();
        if (minLengthAttr?.Length > 0)
          minLength = minLengthAttr.Length;
        var maxLengthAttr = property.Attributes.OfType<MaxLengthAttribute>().FirstOrDefault();
        if (maxLengthAttr?.Length >= 0 && maxLengthAttr?.Length < int.MaxValue)
          maxLength = maxLengthAttr.Length;
      }
      return minLength.HasValue || maxLength.HasValue;
    }

    public virtual PropertyUse Use(IProperty property)
    {
      if (property.Attributes.Any(a => a is RequiredAttribute
        || a.GetType().FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute"))
        return PropertyUse.Required;
      else if (property.Attributes.OfType<ObsoleteAttribute>().Any(a => a.IsError))
        return PropertyUse.Prohibited;
      else if (property.Attributes.OfType<ObsoleteAttribute>().Any())
        return PropertyUse.Obsolete;
      else if (property.Attributes.OfType<BrowsableAttribute>().Any(a => !a.Browsable)
        || property.Attributes.OfType<EditorBrowsableAttribute>().Any(a => a.State == EditorBrowsableState.Never))
        return PropertyUse.Hidden;
      else
        return PropertyUse.Optional;
    }

    protected IEnumerable<IConfigSection> ConfigurationName(IProperty property)
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
  }
}

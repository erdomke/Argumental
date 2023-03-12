using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;

namespace Argumental
{
  public class NumberType : IDataType
  {
    public bool IsConvertibleFromString => true;
    public bool IsInteger { get; }
    public bool IsSigned { get; }
    public Type Type { get; }


    public NumberType(Type type)
    {
      Type = type;
      IsInteger = type == typeof(byte)
        || type == typeof(sbyte)
        || type == typeof(short)
        || type == typeof(ushort)
        || type == typeof(int)
        || type == typeof(uint)
        || type == typeof(long)
        || type == typeof(ulong)
        || type == typeof(BigInteger);
      IsSigned = !(type == typeof(byte)
        || type == typeof(ushort)
        || type == typeof(uint)
        || type == typeof(ulong));
    }

    public bool TryGetExample(IProperty property, out object example)
    {
      if (property.DefaultValue != null)
        example = property.DefaultValue;
      else
      {
        var range = property.Validations.OfType<RangeAttribute>().FirstOrDefault();
        var minimum = range?.Minimum == null
          ? (IsSigned ? double.MinValue : 0.0)
          : Convert.ToDouble(range.Minimum);
        var maximum = range?.Maximum == null
          ? double.MaxValue
          : Convert.ToDouble(range.Maximum);

        if (minimum > double.MinValue && maximum < double.MaxValue)
        {
          example = Convert.ChangeType((maximum + minimum) / 2, Type);
        }
        else if (maximum < double.MaxValue)
        {
          if (maximum > 0)
            example = Convert.ChangeType(maximum / 2, Type);
          else
            example = Convert.ChangeType(maximum - 3.14, Type);
        }
        else if (minimum > double.MinValue)
        {
          example = Convert.ChangeType(minimum + 3.14, Type);
        }
        else
        {
          example = Convert.ChangeType(3.14, Type);
        }

        if (!Validator.TryValidateValue(example, new ValidationContext(this), null, property.Validations))
          return false;
      }
      return true;
    }
  }
}

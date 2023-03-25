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
    public ConfigSection Name => null;
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

    public bool TryGetExample(out object example)
    {
      var minimum = IsSigned ? double.MinValue : 0.0;
      var maximum = double.MaxValue;

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
      return true;
    }
  }
}

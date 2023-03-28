using System;
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
  }
}

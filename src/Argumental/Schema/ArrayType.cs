using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  public class ArrayType : IDataType
  {
    public ConfigSection Name { get; }
    public bool IsConvertibleFromString => Type == typeof(byte[]);
    public Type Type { get; }
    public IDataType ValueType { get; }

    public ArrayType(Type type, IDataType valueType)
    {
      Type = type;
      if (!type.FullName.StartsWith("System.Collections") && !type.IsArray)
        Name = Reflection.GetName(type);
      ValueType = valueType;
    }

    public bool TryGetExample(IProperty property, out object example)
    {
      throw new NotImplementedException();
    }
  }
}

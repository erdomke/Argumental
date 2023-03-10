using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  public class DictionaryType : IDataType
  {
    public bool IsConvertibleFromString => false;
    public IDataType KeyType { get; }
    public Type Type { get; }
    public IDataType ValueType { get; }

    public DictionaryType(Type type, IDataType keyType, IDataType elementType)
    {
      Type = type;
      KeyType = keyType;
      ValueType = elementType;
    }

    public bool TryGetExample(IProperty property, out object example)
    {
      throw new NotImplementedException();
    }
  }
}

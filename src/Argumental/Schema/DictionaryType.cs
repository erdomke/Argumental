﻿using System;

namespace Argumental
{
  public class DictionaryType : IDataType
  {
    public bool IsConvertibleFromString => false;
    public IDataType KeyType { get; }
    public ConfigSection Name { get; }
    public Type Type { get; }
    public IDataType ValueType { get; }


    public DictionaryType(Type type, IDataType keyType, IDataType elementType)
    {
      Type = type;
      if (!type.FullName.StartsWith("System.Collections"))
        Name = Reflection.GetName(type);
      KeyType = keyType;
      ValueType = elementType;
    }
  }
}

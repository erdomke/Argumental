using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  partial class BooleanType : IDataType
  {
    public bool IsConvertibleFromString => true;
    public Type Type { get; }


    public BooleanType(Type type)
    {
      Type = type;
    }

    public bool TryGetExample(IProperty property, out object example)
    {
      if (property.DefaultValue is bool boolean)
        example = boolean;
      else
        example = true;
      
      return true;
    }
  }
}

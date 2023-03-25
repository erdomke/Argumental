using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  partial class BooleanType : IDataType
  {
    public bool IsConvertibleFromString => true;
    public ConfigSection Name => null;
    public Type Type { get; }


    public BooleanType(Type type = null)
    {
      Type = type ?? typeof(bool);
    }

    public bool TryGetExample(out object example)
    {
      example = true;
      return true;
    }
  }
}

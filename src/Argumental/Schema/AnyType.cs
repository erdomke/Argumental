using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  public class AnyType : IDataType
  {
    public bool IsConvertibleFromString => true;

    public ConfigSection Name => null;

    public Type Type => typeof(object);

    public bool TryGetExample(IProperty property, out object example)
    {
      throw new NotImplementedException();
    }
  }
}

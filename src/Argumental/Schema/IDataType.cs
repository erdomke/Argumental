using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  public interface IDataType
  {
    ConfigSection Name { get; }
    Type Type { get; }
    bool IsConvertibleFromString { get; }
    bool TryGetExample(IProperty property, out object example);
  }
}

using System;

namespace Argumental
{
  public interface IDataType
  {
    ConfigSection Name { get; }
    Type Type { get; }
    bool IsConvertibleFromString { get; }
  }
}

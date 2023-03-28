using System;

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
  }
}

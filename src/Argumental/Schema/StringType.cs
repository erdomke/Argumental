using System;

namespace Argumental
{
  public class StringType : IDataType
  {
    public bool IsConvertibleFromString => true;
    public ConfigSection Name { get; }
    public Type Type { get; }


    public StringType(Type type = null)
    {
      Type = type ?? typeof(string);
      if (type.IsEnum)
        Name = Reflection.GetName(Type);
    }
  }
}

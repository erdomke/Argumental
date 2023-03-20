using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Argumental
{
  public interface IProperty
  {
    object DefaultValue { get; }
    bool Hidden { get; }
    bool IsPositional { get; }
    bool MaskValue { get; }
    ConfigPath Name { get; }
    IDataType Type { get; }
    IEnumerable<ValidationAttribute> Validations { get; }
  }
}

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Argumental
{
  public interface IOptionProvider : ISchemaProvider
  {
    bool TryGet(IConfiguration configuration, List<ValidationResult> validationResults, out object value);
  }

  public interface IOptionProvider<TOption> : IOptionProvider
  {
    bool TryGet(IConfiguration configuration, List<ValidationResult> validationResults, out TOption value);
  }
}

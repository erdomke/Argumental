using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Argumental
{
  public interface IOptionProvider : ISchemaProvider
  {
    object Get(InvocationContext context);
  }

  public interface IOptionProvider<TOption> : IOptionProvider
  {
    new TOption Get(InvocationContext context);
  }
}

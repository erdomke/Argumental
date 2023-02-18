using Microsoft.Extensions.Configuration;

namespace Argumental
{
  public interface IOptionProvider : ISchemaProvider
  {
    object Get(IConfiguration configuration);
  }

  public interface IOptionProvider<TOption> : IOptionProvider
  {
    TOption Get(IConfiguration configuration);
  }
}

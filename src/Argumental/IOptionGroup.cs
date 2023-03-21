using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Argumental
{
  public interface IOptionGroup : IOptionProvider
  {
    void Register(IServiceCollection services, IConfiguration configuration);
  }
}

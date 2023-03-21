using Microsoft.Extensions.DependencyInjection;

namespace Argumental
{
  public interface IServiceRegistrar
  {
    IServiceCollection Register(IServiceCollection services);
  }
}

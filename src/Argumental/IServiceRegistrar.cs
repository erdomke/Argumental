using Microsoft.Extensions.DependencyInjection;

namespace Argumental
{
  public interface IServiceRegistrar
  {
    IServiceCollection AddServices(IServiceCollection services);
  }
}

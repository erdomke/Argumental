using Microsoft.Extensions.Configuration;

namespace Argumental
{
  public static class Extensions
  {
    public static IConfigurationBuilder AddCommandPipeline(this IConfigurationBuilder configurationBuilder, ICommandPipeline commandPipeline)
    {
      configurationBuilder.Add(commandPipeline);
      return configurationBuilder;
    }
  }
}

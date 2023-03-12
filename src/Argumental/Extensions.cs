using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Argumental
{
  public static class Extensions
  {
    public static IConfigurationBuilder AddCommandPipeline(this IConfigurationBuilder configurationBuilder, ICommandPipeline commandPipeline)
    {
      configurationBuilder.Add(commandPipeline);
      return configurationBuilder;
    }

    public static bool IsRequired(this IProperty property)
    {
      return property.Validations.OfType<RequiredAttribute>().Any();
    }
  }
}

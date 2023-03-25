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
      return property.Attributes.OfType<RequiredAttribute>().Any();
    }

    public static bool TryFindIndex(this string value, string substring, out int index)
    {
      index = value.IndexOf(substring);
      return index >= 0;
    }
  }
}

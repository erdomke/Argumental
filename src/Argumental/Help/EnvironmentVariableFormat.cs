using System.Collections.Generic;
using System.Linq;

namespace Argumental.Help
{
  internal class EnvironmentVariableFormat : IConfigFormat
  {
    public string Prefix { get; }
    
    public EnvironmentVariableFormat(string prefix = null)
    {
      Prefix = prefix;
    }

    public IEnumerable<ConfigProperty> GetProperties(IEnumerable<IProperty> properties)
    {
      foreach (var property in properties)
      {
        var fullName = string.Join("__", property.Name.Select(p => p.ToString())).ToUpperInvariant();
        if (!string.IsNullOrEmpty(Prefix))
          fullName = Prefix + fullName;
        yield return new ConfigProperty(new[] { fullName }, property);
      }
    }
  }
}

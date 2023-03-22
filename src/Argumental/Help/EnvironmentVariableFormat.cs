using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
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
      var propList = new List<IProperty>();
      Property.FlattenList(Array.Empty<IConfigSection>(), properties, false, propList);
      foreach (var property in propList)
      {
        var fullName = string.Join("__", property.Name.Select(p => p.ToString())).ToUpperInvariant();
        if (!string.IsNullOrEmpty(Prefix))
          fullName = Prefix + fullName;
        yield return new ConfigProperty(new[] { fullName }, property);
      }
    }
  }
}

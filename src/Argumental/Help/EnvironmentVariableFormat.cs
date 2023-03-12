using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Argumental.Help
{
  internal class EnvironmentVariableFormat : IConfigFormat
  {
    public string Prefix { get; }
    public bool UseWindowsFormat { get; }
    public Func<IProperty, bool> Filter => throw new NotImplementedException();

    public EnvironmentVariableFormat(string prefix = null, bool? useWindowsFormat = null)
    {
      Prefix = prefix;
      if (useWindowsFormat.HasValue)
        UseWindowsFormat = useWindowsFormat.Value;
      else
        UseWindowsFormat = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public IEnumerable<ConfigAlias> GetAliases(IProperty property)
    {
      var fullName = string.Join("__", property.Path.Select(p => p.ToString()));
      if (!string.IsNullOrEmpty(Prefix))
        fullName = Prefix + fullName;
      if (UseWindowsFormat)
        fullName = "%" + fullName + "%";
      else
        fullName = "$" + fullName;
      return new[] { new ConfigAlias(ConfigAliasType.EnvironmentVariable, fullName) };
    }
  }
}

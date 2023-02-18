using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class ConfigPath : IReadOnlyList<IConfigSection>
  {
    private List<IConfigSection> _names = new List<IConfigSection>();

    public int Count => _names.Count;

    public IConfigSection this[int index] => _names[index];

    public ConfigPath(ConfigSection name)
    {
      _names.Add(name);
    }

    public ConfigPath(params IConfigSection[] names)
    {
      _names.AddRange(names);
    }

    public ConfigPath(IEnumerable<IConfigSection> names)
    {
      _names.AddRange(names);
    }

    public void Add(IConfigSection name)
    {
      _names.Add(name);
    }

    public IEnumerator<IConfigSection> GetEnumerator()
    {
      return _names.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public static implicit operator ConfigPath(ConfigSection name)
    {
      return new ConfigPath(name);
    }

    public override string ToString()
    {
      return string.Join(":", _names.Select(n => n.ToString()));
    }
  }
}

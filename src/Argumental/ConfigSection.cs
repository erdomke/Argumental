using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argumental
{
  public class ConfigSection : IConfigSection
  {
    public string Name { get; }
    public string Description { get; set; }

    public ConfigSection(string name)
    {
      Name = name;
    }

    public ConfigSection(string name, string description)
    {
      Name = name;
      Description = description;
    }

    public override string ToString()
    {
      return Name;
    }

    public bool Matches(string segment)
    {
      return segment == Name;
    }
  }
}

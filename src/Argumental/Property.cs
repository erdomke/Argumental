using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Argumental
{
  internal class Property : IProperty
  {
    private List<Attribute> _attributes = new List<Attribute>();

    public ConfigPath Name { get; }

    public IDataType Type { get; }

    public IEnumerable<Attribute> Attributes => _attributes;

    public Property(ConfigPath path, IDataType type)
    {
      Name = path;
      Type = type;
    }

    public Property(ConfigPath newName, IProperty clone)
    {
      _attributes.AddRange(clone.Attributes);
      Name = newName;
      Type = clone.Type;
    }

    public Property(IEnumerable<IConfigSection> parents, PropertyInfo property)
    {
      _attributes = property.GetCustomAttributes().ToList();
      if (!Reflection.TryGetDataType(property.PropertyType, out var dataType))
        dataType = new ObjectType(property.PropertyType);

      Name = new ConfigPath(parents)
      {
        new ConfigSection(property.Name)
      };
      Type = dataType;
    }

    public void AddAttribute(Attribute attribute)
    {
      _attributes.Add(attribute);
    }
  }
}

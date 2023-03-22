using System.Collections.Generic;

namespace Argumental
{
  public interface ISchemaProvider
  {
    IEnumerable<IProperty> Properties { get; }
  }
}

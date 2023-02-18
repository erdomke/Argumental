using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argumental
{
  public interface ISchemaProvider
  {
    IEnumerable<IProperty> Properties { get; }
  }
}

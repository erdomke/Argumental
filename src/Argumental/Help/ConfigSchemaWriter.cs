using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Argumental
{
  public abstract class ConfigSchemaWriter
  {
    protected TextWriter _writer;
    protected ConfigFormatRepository _repository;

    public virtual void Configure(TextWriter writer, ConfigFormatRepository repository)
    {
      _writer = writer;
      _repository = repository;
    }

    public abstract void WriteHelp(AssemblyMetadata metadata, IEnumerable<ISchemaProvider> schemas, IEnumerable<string> errors);
  }
}

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

    public abstract void Write(AssemblyMetadata metadata, IEnumerable<ISchemaProvider> schemas, IEnumerable<string> errors);

    public virtual void Write(AssemblyMetadata metadata, ISchemaProvider schema, IEnumerable<string> errors)
    {
      Write(metadata, new[] {schema }, errors);
    }
  }
}

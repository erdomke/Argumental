using System.Collections.Generic;

namespace Argumental
{
  public class HelpContext
  {
    public CommandApp App { get; set; }
    public List<string> Errors { get; } = new List<string>();
    public DocumentationBuilder ConfigFormats { get; set; }
    public int? MaxLineWidth { get; }
    public AssemblyMetadata Metadata { get; set; }
    public List<ISchemaProvider> Schemas { get; } = new List<ISchemaProvider>();
    public DocumentationScope Section { get; set; }
  }
}

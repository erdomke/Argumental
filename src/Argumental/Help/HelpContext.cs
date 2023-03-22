﻿using System.Collections.Generic;

namespace Argumental
{
  public class HelpContext
  {
    public List<string> Errors { get; } = new List<string>();
    public ConfigFormatRepository Formats { get; set; }
    public int? MaxLineWidth { get; }
    public AssemblyMetadata Metadata { get; set; }
    public List<ISchemaProvider> Schemas { get; } = new List<ISchemaProvider>();
    public HelpSection Section { get; set; }
  }
}

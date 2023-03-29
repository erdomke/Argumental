using Argumental.Help;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace Argumental
{
  public interface IDocbookSectionWriter
  {
    int Order { get; }

    IEnumerable<XElement> Write(DocumentationContext context);
  }
}

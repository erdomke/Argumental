using System.Xml.Linq;

namespace Argumental
{
  internal interface IDocbookSectionWriter
  {
    XElement Write(HelpContext context);
  }
}

using System.IO;

namespace Argumental
{
  public interface IHelpWriter
  {
    void Write(HelpContext context, TextWriter writer);
  }
}

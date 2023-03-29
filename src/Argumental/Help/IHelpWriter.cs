using System.IO;

namespace Argumental
{
  /// <summary>
  /// Writes help in a particular format
  /// </summary>
  public interface IHelpWriter
  {
    /// <summary>
    /// The name of the format
    /// </summary>
    string Format { get; }
    /// <summary>
    /// Write help documentation to the specified <see cref="TextWriter"/>
    /// </summary>
    /// <param name="context">Context describing the help to write</param>
    /// <param name="writer">The output to write to</param>
    void Write(DocumentationContext context, TextWriter writer);
  }
}

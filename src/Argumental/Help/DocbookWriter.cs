using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Argumental
{
  /// <summary>
  /// Writes documentation in the <see href="https://tdg.docbook.org/tdg/5.2/"/>docbook</see> format.
  /// </summary>
  /// <remarks>
  /// The documentation is structured to follow the order and contents of a 
  /// <see href="https://man7.org/linux/man-pages/man7/man-pages.7.html"/>standard manpage</see>
  /// </remarks>
  public class DocbookWriter : IHelpWriter
  {
    private readonly Func<IEnumerable<IDocbookSectionWriter>> _sectionFactory;

    /// <inheritdoc />
    public string Format => "docbook";

    public DocbookWriter(Func<IEnumerable<IDocbookSectionWriter>> sectionFactory)
    {
      _sectionFactory = sectionFactory;
    }
    
    public XElement Write(DocumentationContext context)
    {
      var root = new XElement(DocbookSchema.article);
      foreach (var section in _sectionFactory().OrderBy(s => s.Order))
      {
        foreach (var element in section.Write(context))
          root.Add(element);
      }
      return root;
    }

    public void Write(DocumentationContext context, TextWriter writer)
    {
      using (var xml = XmlWriter.Create(writer, new XmlWriterSettings()
      {
        OmitXmlDeclaration = false,
        Indent = true,
        IndentChars = "  "
      }))
        Write(context).WriteTo(xml);
    }
  }
}

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Argumental.Help
{
  public class FrontMatterSection : IDocbookSectionWriter
  {
    private readonly AssemblyMetadata _metadata;

    public int Order => -100;

    public FrontMatterSection(AssemblyMetadata metadata)
    {
      _metadata = metadata;
    }

    public IEnumerable<XElement> Write(DocumentationContext context)
    {
      if (_metadata == null)
      {
        yield return new XElement(DocbookSchema.title, "");
      }
      else
      {
        var info = new XElement(DocbookSchema.info
          , new XElement(DocbookSchema.title, _metadata?.Name));

        if (_metadata?.BuildDate != null)
          info.Add(new XElement(DocbookSchema.date, _metadata.BuildDate));
        if (!string.IsNullOrEmpty(_metadata?.Version))
          info.Add(new XElement(DocbookSchema.releaseinfo, _metadata.Version));

        if (!string.IsNullOrEmpty(_metadata?.Copyright))
        {
          var copyrightText = Regex.Replace(_metadata.Copyright, @"^(Copyright\s+)?(\(c\)|©)?\s*", "", RegexOptions.IgnoreCase);
          var years = new List<string>();
          var holder = new XElement(DocbookSchema.holder, Regex.Replace(copyrightText, @"\b\d{4}\b", m =>
          {
            years.Add(m.Value);
            return "";
          }).Trim());
          if (years.Count > 0)
          {
            var copyright = new XElement(DocbookSchema.copyright);
            foreach (var year in years)
              copyright.Add(new XElement(DocbookSchema.year, year));
            copyright.Add(holder);
            info.Add(copyright);
          }
        }

        yield return info;

        foreach (var error in context.Errors)
          yield return new XElement(DocbookSchema.important, new XElement(DocbookSchema.para, error));

        if (!string.IsNullOrEmpty(_metadata.Description))
        {
          yield return new XElement(DocbookSchema.para, _metadata.Description);
        }
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Argumental
{
  public class DocumentationContext
  {
    public List<string> Errors { get; } = new List<string>();
    public int? MaxLineWidth { get; set; }
    public List<ISchemaProvider> Schemas { get; } = new List<ISchemaProvider>();
    public DocumentationScope Scope { get; set; }
    
    public IEnumerable<XElement> DescribeProperty(IProperty property, SerializationInfo info)
    {
      var para = new XElement(DocbookSchema.para);
      var description = info.Description(property);
      if (!string.IsNullOrEmpty(description))
        para.Add(description);
      var defaultValue = info.DefaultValue(property);
      if (defaultValue != null)
      {
        para.Add(" [default: ");
        para.Add(new XElement(DocbookSchema.literal, defaultValue.ToString()));
        para.Add("]");
      }
      yield return para;
    }
  }
}

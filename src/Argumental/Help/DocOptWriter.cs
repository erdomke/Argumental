using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Argumental
{
  /// <summary>
  /// Writes documentation in the <see href="http://docopt.org/"/>docopt</see> format.
  /// </summary>
  public class DocOptWriter : IHelpWriter
  {
    private readonly DocbookWriter _docbook;

    /// <inheritdoc />
    public string Format => "docopt";

    public DocOptWriter(DocbookWriter docbook)
    {
      _docbook = docbook;
    }

    /// <inheritdoc />
    public void Write(DocumentationContext context, TextWriter writer)
    {
      var xml = _docbook.Write(context);
      VisitElement(xml, new TextWrapper(writer)
      {
        MaxWidth = context.MaxLineWidth ?? 80
      });
    }

    private void VisitElement(XElement element, TextWrapper writer)
    {
      if (element.Name == DocbookSchema.important)
      {
        writer.WriteLine((string)element.Element(DocbookSchema.para));
        writer.WriteLine();
      }
      else if (element.Name == DocbookSchema.para)
        writer.WriteLine((string)element);
      else if (element.Name == DocbookSchema.cmdsynopsis)
        WriteSynopsis(element, writer);
      else if (element.Name == DocbookSchema.section)
      {
        if (element.Elements()
          .Where(e => e.Name != DocbookSchema.title)
          .All(e => e.Name == DocbookSchema.cmdsynopsis))
        {
          WriteSynopsis(element.Element(DocbookSchema.cmdsynopsis), writer);
        }
        else
        {
          writer.WriteLine();
          writer.WriteLine((string)element.Element(DocbookSchema.title) + ":");
          writer.IncreaseIndent("  ");
          if (element.Elements().Any(e => e.Name == DocbookSchema.section))
          {
            foreach (var child in element.Elements().Skip(1))
              VisitElement(child, writer);
          }
          else
          {
            foreach (var child in element.Elements().Skip(1))
              WriteText(child, writer);
          }
          writer.DecreaseIndent();
        }
      }
      else
      {
        foreach (var child in element.Elements())
          VisitElement(child, writer);
      }
    }

    private void WriteText(XNode node, TextWrapper writer)
    {
      if (node is XText text)
      {
        writer.Write(text.Value);
      }
      else if (node is XElement element)
      {
        if (element.Name == DocbookSchema.replaceable)
        {
          if (Parents(element).Any(e => e.Name == DocbookSchema.cmdsynopsis))
          {
            writer.WriteWord("<" + ((string)element).Replace(' ', '-') + ">");
          }
          else
          {
            writer.StartWord();
            writer.Write("<");
            foreach (var child in element.Nodes())
              WriteText(child, writer);
            writer.Write(">");
            writer.EndWord();
          }
        }
        else if (element.Name == DocbookSchema.arg)
        {
          var prefix = default(string);
          var suffix = default(string);
          var choice = (string)element.Attribute("choice") ?? "opt";
          //if (choice == "req")
          //{
          //  prefix = "(";
          //  suffix = ")";
          //}
          if (choice == "opt")
          {
            prefix = "[";
            suffix = "]";
          }

          if ((string)element.Attribute("rep") == "repeat")
            suffix = "..." + (suffix ?? "");

          writer.StartWord();
          if (!string.IsNullOrEmpty(prefix))
            writer.Write(prefix);
          foreach (var child in element.Nodes())
            WriteText(child, writer);
          if (!string.IsNullOrEmpty(suffix))
            writer.Write(suffix);
          writer.EndWord();
        }
        else if (element.Name == DocbookSchema.varlistentry)
        {
          var first = true;
          foreach (var term in element.Elements(DocbookSchema.term))
          {
            if (first)
              first = false;
            else
              writer.Write(", ");
            WriteText(term, writer);
          }
          writer.IncreaseIndent(new string(' ', 18));
          writer.Write("  ");
          WriteText(element.Element(DocbookSchema.listitem), writer);
          writer.DecreaseIndent();
          writer.WriteLine();
        }
        else if (element.Name == DocbookSchema.para
          && !(element == element.Parent.FirstNode))
        {
          foreach (var child in element.Nodes())
            WriteText(child, writer);
          writer.WriteLine();
          writer.WriteLine();
        }
        else
        {
          foreach (var child in element.Nodes())
            WriteText(child, writer);
        }
      }
    }

    private void WriteSynopsis(XElement element, TextWrapper writer)
    {
      writer.WriteLine();
      writer.WriteLine("Usage:");
      writer.IncreaseIndent("  ");
      WriteText(element.Nodes().First(), writer);
      writer.IncreaseIndent("  ");
      foreach (var node in element.Nodes().Skip(1))
        WriteText(node, writer);
      writer.DecreaseIndent();
      writer.DecreaseIndent();
      writer.WriteLine();
    }

    private IEnumerable<XElement> Parents(XElement element)
    {
      var curr = element.Parent;
      while (curr != null)
      {
        yield return curr;
        curr = curr.Parent;
      }
    }
  }
}

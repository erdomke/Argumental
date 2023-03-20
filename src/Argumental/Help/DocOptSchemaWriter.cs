using Argumental.Help;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class DocOptSchemaWriter : ManPageWriter
  {
    protected override void WriteErrors(IEnumerable<string> errors)
    {
      foreach (var error in errors)
        _writer.WriteLine(error);
      _writer.WriteLine();
    }

    public override void WriteApplication(string name)
    {
      _writer.Write(name);
      if (_writer is TextWrapper wrapper)
        wrapper.IncreaseIndent("  ");
    }

    public override void WriteArgument(string optionName, string prompt, bool? required = null, bool repeat = false)
    {
      _writer.Write(' ');
      var arg = "";
      if (required == false)
        arg += "[";
      if (!string.IsNullOrEmpty(optionName))
        arg += optionName;
      if (!string.IsNullOrEmpty(prompt))
      {
        if (!string.IsNullOrEmpty(optionName))
          arg += " ";
        arg += "<" + prompt.Replace(' ', '-') + ">";
      }
      if (repeat)
        arg += "...";
      if (required == false)
        arg += "]";

      if (_writer is TextWrapper wrapper)
        wrapper.WriteWord(arg);
      else
        _writer.Write(arg);
    }

    public override void WriteDescription(string description)
    {
      _writer.WriteLine(description);
    }

    public override void WriteEndSection(SchemaSection section)
    {
      if (section != SchemaSection.Usage)
        _writer.WriteLine();
      if (_writer is TextWrapper wrapper
        && (section == SchemaSection.Usage
          || section == SchemaSection.Options
          || section == SchemaSection.Commands
          || section == SchemaSection.Synopsis))
      {
        wrapper.DecreaseIndent();
      }
    }

    public override void WriteStartSection(SchemaSection section)
    {
      _writer.WriteLine();
      if (section == SchemaSection.Usage
        || section == SchemaSection.Options
        || section == SchemaSection.Commands)
      {
        _writer.Write(section.ToString() + ":");
        if (_writer is TextWrapper wrapper)
          wrapper.IncreaseIndent("  ");
      }
    }

    public override void WriteOption(IEnumerable<string> aliases, string description, object defaultValue)
    {
      _writer.WriteLine();
      var wrapper = _writer as TextWrapper;
      _writer.Write(string.Join(", ", aliases));
      wrapper?.IncreaseIndent(new string(' ', 18));
      _writer.Write("  ");
      _writer.Write(description);
      if (defaultValue != null)
      {
        _writer.Write(" [default: ");
        _writer.Write(defaultValue.ToString());
        _writer.Write("]");
      }
      wrapper?.DecreaseIndent();
    }
  }
}

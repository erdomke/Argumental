using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Argumental
{
  internal class ExitCodeSection : IDocbookSectionWriter
  {
    private readonly CommandApp _app;
    private readonly IReadOnlyDictionary<int, string> _messages;

    //public CommandApp App { get; set; }

    //public Dictionary<int, string> ExitCodeMessages { get; } = typeof(ExitCode)
    //  .GetFields(BindingFlags.Public | BindingFlags.Static)
    //  .ToDictionary(f => (int)f.GetRawConstantValue()
    //  , f => f.GetCustomAttribute<DescriptionAttribute>().Description);

    public int Order => (int)DocbookSection.ExitStatus;

    public ExitCodeSection(CommandApp app, IReadOnlyDictionary<int, string> messages)
    {
      _app = app;
      _messages = messages;
    }

    public IEnumerable<XElement> Write(DocumentationContext context)
    {
      if (!(_app?.ExitCodes.Any() ?? false))
        return Enumerable.Empty<XElement>();

      var variableList = new XElement(DocbookSchema.variablelist
        , new XElement(DocbookSchema.varlistentry
          , new XElement(DocbookSchema.term
            , new XElement(DocbookSchema.returnvalue, "0")
          ),
          new XElement(DocbookSchema.listitem,
            new XElement(DocbookSchema.para, _messages[0]))
        )
      );
      foreach (var exitCode in _app?.ExitCodes)
      {
        var para = new XElement(DocbookSchema.para);
        if (_messages.TryGetValue(exitCode.Key, out var message))
        {
          para.Add(message);
        }
        else
        {
          var first = true;
          foreach (var type in exitCode)
          {
            if (first)
              first = false;
            else
              para.Add(", ");
            para.Add(new XElement(DocbookSchema.errorname, type.Name));
          }
        }
        variableList.Add(new XElement(DocbookSchema.varlistentry
          , new XElement(DocbookSchema.term
            , new XElement(DocbookSchema.returnvalue, exitCode.Key.ToString())
          ),
          new XElement(DocbookSchema.listitem, para)
        ));
      }
      return new[] { new XElement(DocbookSchema.section
        , new XElement(DocbookSchema.title, "Exit Status")
        , variableList
      ) };
    }
  }
}

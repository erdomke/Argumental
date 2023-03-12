using System;

namespace Argumental
{
  public interface ICommand : ISchemaProvider
  {
    ConfigPath Name { get; }
    bool Hidden { get; }
    Action<ParseContext> Matcher { get; }
  }
}

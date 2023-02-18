using System;

namespace Argumental
{
  public interface ICommand : ISchemaProvider
  {
    ConfigPath Name { get; }
    Action<ParseContext> Matcher { get; }
  }
}

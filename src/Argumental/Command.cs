using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class Command<TResult> : IConfigHandler<TResult>, ICommand
  {
    public IList<IOptionProvider> Providers { get; } = new List<IOptionProvider>();
    public ConfigPath Name { get; }
    public IEnumerable<IProperty> Properties => Providers.SelectMany(p => p.Properties);
    public Action<ParseContext> Matcher { get; set; }
    public Func<IConfigHandler<TResult>, IConfigurationRoot, TResult> Handler { get; set; }

    public Command(string name = "", string description = null)
    {
      Name = new ConfigSection(name, description);
      Matcher = MatchByName;
    }

    private void MatchByName(ParseContext context)
    {
      if (Name.Count == 0
        || (Name.Count == 1 && string.IsNullOrEmpty(Name[0].ToString())))
      {
        context.Success = true;
      }
      else if (context.Tokens.Count < Name.Count)
      {
        context.Success = false;
      }
      else
      {
        context.Success = true;
        for (var i = 0; i < Name.Count; i++)
        {
          if (context.Tokens[0].Type == TokenType.Value
            && Name[i] is ConfigSection name
            && string.Equals(context.Tokens[0].Value, name.Name, StringComparison.OrdinalIgnoreCase))
            context.Tokens.RemoveAt(0);
          else
            context.Success = false;
        }
      }
    }

    public IReadOnlyList<object> GetOptions(IConfigurationRoot configuration)
    {
      return Providers.Select(p => p.Get(configuration)).ToList();
    }

    private void MatchAll(ParseContext context)
    {
      context.Success = true;
    }
  }
}

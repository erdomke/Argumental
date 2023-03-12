using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  /// <summary>
  /// A command that operates on the configuration and produces a result.
  /// </summary>
  /// <typeparam name="TResult">The type of result produced by invoking the command.</typeparam>
  public class Command<TResult> : IConfigHandler<TResult>, ICommand
  {
    public IList<IOptionProvider> Providers { get; } = new List<IOptionProvider>();
    public ConfigPath Name { get; }
    public string Description
    {
      get { return Name.OfType<ConfigSection>().Last().Description; }
      set { Name.OfType<ConfigSection>().Last().Description = value; }
    }
    public IEnumerable<IProperty> Properties => Providers.SelectMany(p => p.Properties);
    public Action<ParseContext> Matcher { get; set; }
    public Func<IConfigHandler<TResult>, IConfigurationRoot, TResult> Handler { get; set; }
    public bool AllowUnrecognizedTokens { get; set; }
    public bool Hidden { get; set; }

    public Command(string name = "", string description = null)
    {
      Name = new ConfigSection(name, description);
      Matcher = MatchByName;
    }

    public TResult Invoke(IConfigurationRoot configuration)
    {
      if (Handler == null)
        throw new InvalidOperationException($"Command '{Name}' does not have a handler.");
      return Handler.Invoke(this, configuration);
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

    private void MatchAll(ParseContext context)
    {
      context.Success = true;
    }
  }
}

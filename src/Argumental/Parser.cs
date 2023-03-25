using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class Parser : ConfigurationProvider
  {
    public ICommand Command { get; private set; }

    public ICommandPipeline Pipeline { get; }

    public List<string> UnrecognizedTokens { get; } = new List<string>();

    internal Parser(ICommandPipeline pipeline)
    {
      Pipeline = pipeline;
    }

    public override void Load()
    {
      Data = Parse();
    }

    public Dictionary<string, string> Parse()
    {
      var data = new Dictionary<string, string>(Pipeline.SerializationInfo.OptionComparer);
      var switchMappings = Pipeline.Commands
        .SelectMany(c => c.Properties)
        .Distinct()
        .Select(p => Pipeline.SerializationInfo.Aliases(p))
        .Where(a => a.Skip(1).Any())
        .SelectMany(a =>
        {
          var name = a.First();
          return a.Skip(1).Select(n => new KeyValuePair<string, string>((n.Length == 1 ? "-" : "--") + n, "--" + name));
        })
        .GroupBy(k => k.Key, Pipeline.SerializationInfo.OptionComparer)
        .ToDictionary(g => g.Key, g => g.First().Value, Pipeline.SerializationInfo.OptionComparer);
      var tokens = Tokenize(switchMappings);
      var allCommands = new List<ICommand>();
      if (Pipeline.HelpCommand != null)
        allCommands.Add(Pipeline.HelpCommand);
      if (Pipeline.VersionCommand != null)
        allCommands.Add(Pipeline.VersionCommand);
      allCommands.AddRange(Pipeline.Commands);
      foreach (var command in allCommands)
      {
        var context = new ParseContext(tokens);
        command.Matcher?.Invoke(context);
        if (context.Success)
        {
          foreach (var kvp in context.Data)
            data[kvp.Key] = kvp.Value;
          tokens = context.Tokens.ToList();
          Command = command;
          break;
        }
      }

      if (Command == null)
      {
        throw new ConfigurationException(null, new[] { "Required command was not provided." });
      }

      var properties = Command.Properties.ToList();
      var position = 0;
      for (var i = 0; i < tokens.Count; i++)
      {
        if (tokens[i].Type == TokenType.Unknown)
        {
          UnrecognizedTokens.Add(tokens[i].Value);
        }
        else if (tokens[i].Type == TokenType.Value)
        {
          var prop = properties
            .Where(p => Pipeline.SerializationInfo.IsPositional(p))
            .Skip(position)
            .FirstOrDefault();
          if (prop == null)
          {
            UnrecognizedTokens.Add("[Position " + position + "]");
          }
          else if (prop.Type is ArrayType)
          {
            var listIdx = i;
            var propKeyPrefix = prop.Name.ToString() + ":";
            while (listIdx < tokens.Count && tokens[listIdx].Type == TokenType.Value)
            {
              data[propKeyPrefix + (listIdx - i)] = tokens[listIdx].Value;
              listIdx++;
            }
            i = listIdx - 1;
          }
          else
          {
            data[prop.Name.ToString()] = tokens[i].Value;
          }
          position++;
        }
        else // Key
        {
          var propName = tokens[i].Value;
          var prop = properties.FirstOrDefault(p => Pipeline.SerializationInfo.OptionComparer.Equals(Pipeline.SerializationInfo.Name(p), propName));
          if (i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.Value)
          {
            if (prop != null && prop.Type is ArrayType)
            {
              var listIdx = i + 1;
              var propKeyPrefix = propName + ":";
              while (listIdx < tokens.Count && tokens[listIdx].Type == TokenType.Value)
              {
                data[propKeyPrefix + (listIdx - i - 1)] = tokens[listIdx].Value;
                listIdx++;
              }
              i = listIdx - 1;
            }
            else
            {
              i++;
              data[propName] = tokens[i].Value;
            }
          }
          else
          {
            if (prop == null || prop.Type is BooleanType)
              data[propName] = "true";
            else
              UnrecognizedTokens.Add(tokens[i].Value);
          }
        }
      }
      return data;
    }

    public IReadOnlyList<Token> Tokenize(IDictionary<string, string> switchMappings)
    {
      var result = new List<Token>();
      for (var i = 0; i < Pipeline.Args.Count; i++)
      {
        var currentArg = Pipeline.Args[i];
        int keyStartIndex = 0;

        if (currentArg == "--" && i < Pipeline.Args.Count - 1)
        {
          i++;
          result.Add(new Token(TokenType.Value, Pipeline.Args[i]));
        }
        else if (currentArg.StartsWith("--"))
        {
          keyStartIndex = 2;
        }
        else if (currentArg.StartsWith("-"))
        {
          keyStartIndex = 1;
        }
        else if (currentArg.StartsWith("/"))
        {
          // "/SomeSwitch" is equivalent to "--SomeSwitch" when interpreting switch mappings
          // So we do a conversion to simplify later processing
          currentArg = "--" + currentArg.Substring(1);
          keyStartIndex = 2;
        }
        else
        {
          result.Add(new Token(TokenType.Value, currentArg));
          continue;
        }

        int separator = currentArg.IndexOf('=');
        var keySegment = currentArg;
        if (separator > 0)
          keySegment = currentArg.Substring(0, separator);

        // If the switch is a key in given switch mappings, interpret it
        if (switchMappings.TryGetValue(keySegment, out string mappedKeySegment))
        {
          result.Add(new Token(TokenType.Key, mappedKeySegment.TrimStart('-', '/')));
        }
        else if (keyStartIndex == 1 && separator < 0)
        {
          var bundled = keySegment.Skip(1)
            .Select(c => switchMappings.TryGetValue("-" + c, out var mapped) ? mapped : null)
            .ToList();
          // Option bundling
          // git clean -f - d - x
          // git clean -fdx
          if (bundled.All(k => k != null))
          {
            result.AddRange(bundled.Select(k => new Token(TokenType.Key, k)));
          }
          // Option-argument delimiters
          // myapp -vquiet
          // myapp - v quiet
          else if (bundled.Count > 1 && bundled[0] != null)
          {
            result.Add(new Token(TokenType.Key, bundled[0]));
            result.Add(new Token(TokenType.Value, keySegment.Substring(2)));
          }
          else
          {
            result.Add(new Token(TokenType.Unknown, keySegment));
          }
        }
        else
        {
          result.Add(new Token(TokenType.Key, keySegment.Substring(keyStartIndex)));
        }

        if (separator > 0)
          result.Add(new Token(TokenType.Value, currentArg.Substring(separator + 1)));
      }

      return result;
    }
  }
}

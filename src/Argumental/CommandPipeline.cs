using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class CommandPipeline<TResult> : ICommandPipeline
  {
    private readonly List<Command<TResult>> _commands = new List<Command<TResult>>();
    private IEqualityComparer<string> _optionComparer = StringComparer.OrdinalIgnoreCase;

    public IEnumerable<ICommand> Commands => _commands;

    /// <summary>
    /// Gets or sets the switch mappings.
    /// </summary>
    public IDictionary<string, string> SwitchMappings { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the command line args.
    /// </summary>
    public IReadOnlyList<string> Args { get; set; } = Array.Empty<string>();

    public string Description { get; set; }

    public Parser GetParser()
    {
      return new Parser(_optionComparer, this);
    }

    public IConfigurationRoot Build()
    {
      return new ConfigurationBuilder().AddCommandPipeline(this).Build();
    }

    public TResult Run()
    {
      return Run(Build());
    }

    public TResult Run(IConfigurationRoot configuration)
    {
      var parser = configuration.Providers.OfType<Parser>().FirstOrDefault();
      if (parser == null || !(parser.Command is Command<TResult> command))
        throw new InvalidOperationException("Command pipeline was not added to the configuration builder.");
      if (command.Handler == null)
        throw new InvalidOperationException("Command does not have a handler.");
      if (parser.UnrecognizedTokens.Count > 0 && !command.AllowUnrecognizedTokens)
        throw new CommandException("Unexpected options were included on the command line: " + string.Join(", ", parser.UnrecognizedTokens)
          , this, command, null);
      return command.Handler.Invoke(command, configuration);
    }
    
    IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder)
    {
      return GetParser();
    }

    public CommandPipeline<TResult> AddAlias(string alias, string full)
    {
      SwitchMappings.Add(alias, full);
      return this;
    }

    public CommandPipeline<TResult> AddArgs(IReadOnlyList<string> args)
    {
      Args = args;
      return this;
    }

    public CommandPipeline<TResult> AddCommand(Command<TResult> command)
    {
      _commands.Add(command);
      return this;
    }

    public CommandPipeline<TResult> AddCommand(string name, Action<Command<TResult>> builder)
    {
      var command = new Command<TResult>(name);
      _commands.Add(command);
      builder(command);
      return this;
    }

    public CommandPipeline<TResult> AddDescription(string description)
    {
      Description = description;
      return this;
    }

    public CommandPipeline<TResult> AddOptionComparer(IEqualityComparer<string> optionComparer)
    {
      _optionComparer = optionComparer;
      return this;
    }

    public static CommandPipeline<TResult> Default()
    {
      var result = new CommandPipeline<TResult>();
      var helpAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "?", "h", "help" };
      var helpCommand = new Command<TResult>("help")
      {
        AllowUnrecognizedTokens = true,
        Matcher = context =>
        {
          if (context.Tokens.Count > 0
            && context.Tokens[0].Type != TokenType.Key
            && string.Equals(context.Tokens[0].Value, "help", StringComparison.OrdinalIgnoreCase))
          {
            context.Success = true;
          }
          else
          {
            var idx = context.Tokens
              .FindIndex(t => t.Type == TokenType.Key && helpAliases.Contains(t.Value));
            if (idx >= 0)
            {
              context.Success = true;
              var command = default(Token);
              if (idx > 0
                && context.Tokens[0].Type != TokenType.Key)
              {
                command = context.Tokens[0];
              }
              context.Tokens.Clear();
              context.Tokens.Add(new Token(TokenType.Value, "help"));
              if (!string.IsNullOrEmpty(command.Value))
                context.Tokens.Add(command);
            }
          }
        }
      };
      var commandNameOpt = new Option<string>(isPositional: true);
      helpCommand.Providers.Add(commandNameOpt);
      helpCommand.Handler = (_, config) =>
      {
        var command = default(ICommand);
        if (commandNameOpt.TryGet(config, null, out var commandName)
          && !string.IsNullOrEmpty(commandName))
        {
          command = result.Commands.FirstOrDefault(c => string.Equals(c.Name.ToString(), commandName, StringComparison.OrdinalIgnoreCase));
        }
        throw new CommandException("Help requested.", result, command, null);
      };
      result.AddCommand(helpCommand);

      var versionCommand = new Command<TResult>("version")
      {
        AllowUnrecognizedTokens = true,
        Matcher = context =>
        {
          if (context.Tokens.Any(t => t.Type == TokenType.Key && string.Equals(t.Value, "version", StringComparison.OrdinalIgnoreCase)))
          {
            context.Success = true;
            context.Tokens.Clear();
          }
        },
        Handler = (_, config) =>
        {
          throw new VersionException("Version requested.");
        }
      };
      return result.AddCommand(versionCommand);
    }
  }
}

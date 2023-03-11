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
      if (parser.UnrecognizedTokens.Count > 0)
        throw new CommandException("Unexpected options were included on the command line: " + string.Join(", ", parser.UnrecognizedTokens)
          , Commands, command, null);
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

    public CommandPipeline<TResult> AddOptionComparer(IEqualityComparer<string> optionComparer)
    {
      _optionComparer = optionComparer;
      return this;
    }

    public static CommandPipeline<TResult> Default()
    {
      var result = new CommandPipeline<TResult>();
      var helpCommand = new Command<TResult>("help")
      {
        Matcher = context =>
        {
          if (context.Tokens.Any(t => t.Type == TokenType.Key && string.Equals(t.Value, "help", StringComparison.OrdinalIgnoreCase)))
          {
            context.Success = true;
            context.Tokens.Clear();
          }
        }
      };
      helpCommand.Handler = (_, config) =>
      {
        throw new InvalidOperationException("Help called");
      };

      var versionCommand = new Command<TResult>("version")
      {
        Matcher = context =>
        {
          if (context.Tokens.Any(t => t.Type == TokenType.Key && string.Equals(t.Value, "version", StringComparison.OrdinalIgnoreCase)))
          {
            context.Success = true;
            context.Tokens.Clear();
          }
        }
      };
      helpCommand.Handler = (_, config) =>
      {
        throw new InvalidOperationException("Help called");
      };
      return result.AddCommand(helpCommand);
    }
  }
}

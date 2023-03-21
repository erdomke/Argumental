using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class CommandPipeline<TResult> : ICommandPipeline
  {
    private readonly List<Command<TResult>> _commands = new List<Command<TResult>>();
    private readonly Parser _parser;

    public IReadOnlyList<ICommand> Commands => _commands;
    public IConfigurationBuilder ConfigurationBuilder { get; private set; }
    public IEqualityComparer<string> OptionComparer { get; private set; } = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Gets or sets the switch mappings.
    /// </summary>
    public IDictionary<string, string> SwitchMappings { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the command line args.
    /// </summary>
    public IReadOnlyList<string> Args { get; set; } = Array.Empty<string>();

    public ICommand HelpCommand { get; private set; }

    public ICommand VersionCommand { get; private set; }

    
    public CommandPipeline()
    {
      _parser = new Parser(this);
    }

    public Parser GetParser()
    {
      return _parser;
    }

    public IConfigurationRoot Build()
    {
      return Build(null);
    }

    public IConfigurationRoot Build(Action<IConfigurationBuilder> callback)
    {
      try
      {
        var builder = new ConfigurationBuilder();
        callback?.Invoke(builder);
        return builder.AddCommandPipeline(this).Build();
      }
      catch (ConfigurationException ex)
      {
        ex.Pipeline = this;
        ex.ConfigurationBuilder = ConfigurationBuilder;
        throw;
      }
    }

    public TResult Invoke()
    {
      return Invoke(Build(), null);
    }

    public TResult Invoke(IConfiguration configuration, IServiceProvider serviceProvider)
    {
      try
      {
        if (!(_parser.Command is Command<TResult> command))
          throw new InvalidOperationException("Command pipeline was not added to the configuration builder.");
        if (_parser.UnrecognizedTokens.Count > 0 && !command.AllowUnrecognizedTokens)
          throw new ConfigurationException(command, null);
        return command.Invoke(configuration, serviceProvider);
      }
      catch (ConfigurationException ex)
      {
        ex.Pipeline = this;
        ex.ConfigurationBuilder = ConfigurationBuilder;
        throw;
      }
    }
    
    IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder)
    {
      ConfigurationBuilder = builder;
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

    public CommandPipeline<TResult> AddHelpCommand(string name = null)
    {
      name = string.IsNullOrEmpty(name) ? "help" : name;
      SwitchMappings.Add("-?", "--" + name);
      SwitchMappings.Add("-" + name.Substring(0, 1), "--" + name);
      var helpCommand = new Command<TResult>(name)
      {
        AllowUnrecognizedTokens = true,
        Matcher = context =>
        {
          var switches = new HashSet<string>(OptionComparer) { name };
          switches.UnionWith(SwitchMappings
            .Where(k => OptionComparer.Equals(k.Value, "--" + name))
            .Select(k => k.Key.TrimStart('-', '/')));
          if (context.Tokens.Count > 0
            && context.Tokens[0].Type != TokenType.Key
            && string.Equals(context.Tokens[0].Value, name, StringComparison.OrdinalIgnoreCase))
          {
            context.Success = true;
            context.Tokens.RemoveAt(0);
          }
          else
          {
            var idx = context.Tokens
              .FindIndex(t => t.Type == TokenType.Key && switches.Contains(t.Value));
            if (idx >= 0)
            {
              context.Success = true;
            }
          }
        }
      };
      var commandNameOpt = new Option<List<string>>("command", isPositional: true);
      helpCommand.Providers.Add(commandNameOpt);
      helpCommand.Handler = (ctx) =>
      {
        var command = default(ICommand);
        var commandNames = commandNameOpt.Get(ctx);
        foreach (var cmd in Commands)
        {
          var context = new ParseContext(commandNames.Select(n => new Token(TokenType.Value, n)));
          cmd.Matcher?.Invoke(context);
          if (context.Success)
          {
            command = cmd;
            break;
          }
        }
        throw new ConfigurationException(command, null);
      }; 
      HelpCommand = helpCommand;
      return this;
    }

    public CommandPipeline<TResult> SetOptionComparer(IEqualityComparer<string> optionComparer)
    {
      OptionComparer = optionComparer;
      return this;
    }

    public CommandPipeline<TResult> AddVersionCommand(string name = null)
    {
      name = name ?? "version";
      var versionCommand = new Command<TResult>(name)
      {
        AllowUnrecognizedTokens = true,
        Matcher = context =>
        {
          if (context.Tokens.Any(t => t.Type == TokenType.Key && OptionComparer.Equals(t.Value, name)))
          {
            context.Success = true;
            context.Tokens.Clear();
          }
        },
        Handler = (_) =>
        {
          throw new VersionException("Version requested.");
        }
      };
      VersionCommand = versionCommand;
      return this;
    }

    public static CommandPipeline<TResult> Default()
    {
      return new CommandPipeline<TResult>()
        .AddHelpCommand()
        .AddVersionCommand();
    }
  }
}

﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class CommandPipeline<TResult> : ICommandPipeline
  {
    private readonly List<Command<TResult>> _commands = new List<Command<TResult>>();

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
      return new Parser(this);
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
        throw new InvalidOperationException("Command pipeline was not added to the configuration builder");
      if (command.Handler == null)
        throw new InvalidOperationException("Command does not have a handler");
      return command.Handler.Invoke(command, configuration);
    }
    
    IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder)
    {
      return GetParser();
    }

    public CommandPipeline<TResult> AddCommand(Command<TResult> command)
    {
      _commands.Add(command);
      return this;
    }

    public CommandPipeline<TResult> AddArgs(IReadOnlyList<string> args)
    {
      Args = args;
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
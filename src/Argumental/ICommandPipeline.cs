using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Argumental
{
  public interface ICommandPipeline : IConfigurationSource
  {
    /// <summary>
    /// Gets or sets the command line args.
    /// </summary>
    IReadOnlyList<string> Args { get; }

    IReadOnlyList<ICommand> Commands { get; }

    ICommand HelpCommand { get; }

    IEqualityComparer<string> OptionComparer { get; }

    /// <summary>
    /// Gets or sets the switch mappings.
    /// </summary>
    IDictionary<string, string> SwitchMappings { get; }

    ICommand VersionCommand { get; }
  }
}

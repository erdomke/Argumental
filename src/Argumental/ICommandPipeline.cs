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

    IEnumerable<ICommand> Commands { get; }

    string Description { get; }

    /// <summary>
    /// Gets or sets the switch mappings.
    /// </summary>
    IDictionary<string, string> SwitchMappings { get; }
  }
}

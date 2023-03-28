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

    IConfigurationBuilder ConfigurationBuilder { get; }

    ICommand HelpCommand { get; }

    CommandLineInfo SerializationInfo { get; }

    ICommand VersionCommand { get; }

    Parser GetParser();
  }
}

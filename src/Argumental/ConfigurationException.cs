using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class ConfigurationException : Exception
  {
    public ICommandPipeline Pipeline { get; internal set; }
    public IConfigurationBuilder ConfigurationBuilder { get; internal set; }
    public ICommand SelectedCommand { get; }
    public IEnumerable<string> Errors { get; }

    internal ConfigurationException(ICommand selectedCommand, IEnumerable<string> errors = null)
      : base(errors?.Any() == true ? string.Join("\r\n", errors) : "Configuration error.")
    {
      SelectedCommand = selectedCommand;
      Errors = errors ?? Enumerable.Empty<string>();
    }

    internal ConfigurationException(ICommandPipeline pipeline, IEnumerable<string> errors = null)
      : base(errors?.Any() == true ? string.Join("\r\n", errors) : "Configuration error.")
    {
      Pipeline = pipeline;
      SelectedCommand = pipeline?.GetParser().Command;
      Errors = errors ?? Enumerable.Empty<string>();
    }
  }
}

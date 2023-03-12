using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Argumental
{
  public class ConfigurationException : Exception
  {
    public ICommandPipeline Pipeline { get; }
    public ICommand SelectedCommand { get; }
    public IEnumerable<string> Errors { get; }

    internal ConfigurationException(ICommandPipeline pipeline, ICommand selectedCommand, IEnumerable<string> errors)
      : base(errors?.Any() == true ? string.Join("\r\n", errors) : "Configuration error.")
    {
      Pipeline = pipeline;
      SelectedCommand = selectedCommand;
      Errors = errors ?? Enumerable.Empty<string>();
    }
  }
}

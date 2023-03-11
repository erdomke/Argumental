using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Argumental
{
  public class CommandException : Exception
  {
    public ICommandPipeline Pipeline { get; }
    public ICommand SelectedCommand { get; }
    public IEnumerable<ValidationResult> ValidationErrors { get; }

    internal CommandException(string message, ICommandPipeline pipeline, ICommand selectedCommand, IEnumerable<ValidationResult> validationErrors) 
      : base(message)
    {
      Pipeline = pipeline;
      SelectedCommand = selectedCommand;
      ValidationErrors = validationErrors ?? Enumerable.Empty<ValidationResult>();
    }
  }
}

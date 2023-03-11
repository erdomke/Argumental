using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Argumental
{
  public class CommandException : Exception
  {
    public IEnumerable<ICommand> AllCommands { get; }
    public ICommand SelectedCommand { get; }
    public IEnumerable<ValidationResult> ValidationErrors { get; }

    internal CommandException(string message, IEnumerable<ICommand> allCommands, ICommand selectedCommand, IEnumerable<ValidationResult> validationErrors) 
      : base(message)
    {
      AllCommands = allCommands ?? Enumerable.Empty<ICommand>();
      SelectedCommand = selectedCommand;
      ValidationErrors = validationErrors ?? Enumerable.Empty<ValidationResult>();
    }
  }
}

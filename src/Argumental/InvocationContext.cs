using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Argumental
{
  public class InvocationContext
  {
    private List<string> _errorMessages = new List<string>();

    public IConfigHandler Handler { get; }
    public IConfiguration Configuration { get; }
    public IServiceProvider ServiceProvider { get; }

    internal InvocationContext(IConfigHandler handler, IConfiguration configuration, IServiceProvider serviceProvider)
    {
      Handler = handler;
      Configuration = configuration;
      ServiceProvider = serviceProvider;
    }

    public void AddError(Exception ex)
    {
      var messages = new List<string>();
      var curr = ex;
      while (curr != null)
      {
        messages.Add(curr.Message);
        curr = curr.InnerException;
      }
      AddError(string.Join(" -> ", messages));
    }

    public void AddError(string message)
    {
      _errorMessages.Add(message);
    }

    public void AddErrors(IEnumerable<string> messages)
    {
      _errorMessages.AddRange(messages);
    }

    public void AddErrors(IEnumerable<ValidationResult> messages)
    {
      _errorMessages.AddRange(messages.Select(m => m.ErrorMessage));
    }

    internal void AssertSuccess()
    {
      if (_errorMessages.Count > 0)
        throw new ConfigurationException(Handler as ICommand, _errorMessages);
    }
  }
}

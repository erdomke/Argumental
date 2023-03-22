using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Argumental
{
  public class ConfigFormatRepository
  {
    private readonly List<IHelpWriter> _writers = new List<IHelpWriter>();

    public List<IConfigFormat> Formats { get; } = new List<IConfigFormat>();

    public ConfigFormatRepository AddFormat(IConfigFormat format)
    {
      Formats.Add(format);
      return this;
    }

    public ConfigFormatRepository AddWriter(IHelpWriter writer)
    {
      _writers.Add(writer);
      return this;
    }

    public IEnumerable<ConfigProperty> GetProperties<T>(IEnumerable<IProperty> schemaProperties) where T : IConfigFormat
    {
      var format = Formats.OfType<T>().FirstOrDefault();
      if (format == null)
        return Enumerable.Empty<ConfigProperty>();

      return format.GetProperties(schemaProperties
        .Where(p => !p.Hidden)
        .OrderBy(p => p.IsRequired() && !p.IsPositional ? 0 : 1)
        .ThenBy(p => p.IsPositional ? "" : p.Name.ToString(), StringComparer.OrdinalIgnoreCase));
    }

    public void WriteError(TextWriter writer, AssemblyMetadata metadata, ConfigurationException exception)
    {
      var context = new HelpContext()
      {
        Formats = this,
        Metadata = metadata,
        Section = exception.SelectedCommand == null ? HelpSection.Root : HelpSection.Command,
      };
      context.Errors.AddRange(exception.Errors);
      if (exception.SelectedCommand != null)
        context.Schemas.Add(exception.SelectedCommand);
      else if (exception.Pipeline?.Commands != null)
        context.Schemas.AddRange(exception.Pipeline.Commands);
      _writers.First().Write(context, writer);
    }

    public static ConfigFormatRepository Default(IConfigurationBuilder builder)
    {
      var result = new ConfigFormatRepository()
        .AddWriter(new DocOptWriter());
      if (builder != null)
      {
        foreach (var source in builder.Sources)
        {
          if (source is ICommandPipeline pipeline)
          {
            var formatsToRemove = result.Formats.OfType<CommandLineFormat>().ToList();
            foreach (var format in formatsToRemove)
              result.Formats.Remove(format);

            result.AddFormat(new CommandLineFormat(pipeline.SwitchMappings, true
              , pipeline.HelpCommand?.Name.First().ToString()
              , pipeline.VersionCommand?.Name.First().ToString()));
          }
          else if (source.GetType().FullName == "Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationSource")
          {
            if (!result.Formats.OfType<CommandLineFormat>().Any())
            {
              var mappings = (IDictionary<string, string>)source.GetType().GetProperty("SwitchMappings").GetValue(source);
              result.AddFormat(new CommandLineFormat(mappings, false));
            }
          }
          else if (source.GetType().FullName == "Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource")
          {
            var prefix = (string)source.GetType().GetProperty("Prefix").GetValue(source);
            result.AddFormat(new EnvironmentVariableFormat(prefix));
          } 
        }
      }
      return result;
    }
  }
}

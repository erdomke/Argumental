using Argumental.Help;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Argumental
{
  public class ConfigFormatRepository
  {
    private readonly List<IHelpWriter> _writers = new List<IHelpWriter>();

    public List<IConfigFormat> Formats { get; } = new List<IConfigFormat>();

    public ConfigFormatRepository AddFormat<T>(Action<T> update) where T : IConfigFormat, new()
    {
      var existing = Formats.OfType<T>().FirstOrDefault();
      if (existing == null)
      {
        existing = new T();
        Formats.Add(existing);
      }
      update(existing);
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

      return format.GetProperties(schemaProperties)
        .Where(p => p.Property.Use < PropertyUse.Hidden);
    }

    public void WriteError(TextWriter writer, CommandApp app, ConfigurationException exception)
    {
      var context = new HelpContext()
      {
        App = app,
        Formats = this,
        Metadata = app.GetService<AssemblyMetadata>(),
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
            result.AddFormat<CommandLineFormat>(f => f.AddConfiguration(pipeline.SwitchMappings, true
              , pipeline.HelpCommand?.Name.First().ToString()
              , pipeline.VersionCommand?.Name.First().ToString()));
          }
          else if (source.GetType().FullName == "Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationSource")
          {
            var mappings = (IDictionary<string, string>)source.GetType().GetProperty("SwitchMappings").GetValue(source);
            result.AddFormat<CommandLineFormat>(f => f.AddConfiguration(mappings, false));
          }
          else if (source.GetType().FullName == "Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource")
          {
            var prefix = (string)source.GetType().GetProperty("Prefix").GetValue(source);
            result.AddFormat<EnvironmentVariableFormat>(f => f.Prefixes.Add(prefix));
          }
          else if (source.GetType().FullName == "Microsoft.Extensions.Configuration.Json.JsonConfigurationSource")
          {
            var path = (string)source.GetType().GetProperty("Path").GetValue(source);
            if (!string.IsNullOrEmpty(path))
              result.AddFormat<JsonFileFormat>(f => f.Paths.Add(path));
          }
        }
      }
      return result;
    }
  }
}

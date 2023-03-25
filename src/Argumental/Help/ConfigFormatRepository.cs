using Argumental.Help;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Argumental
{
  public class ConfigFormatRepository
  {
    private readonly List<IHelpWriter> _writers = new List<IHelpWriter>();

    private List<SerializationInfo> Formats { get; } = new List<SerializationInfo>();

    public ConfigFormatRepository AddFormat(SerializationInfo metadata)
    {
      Formats.Add(metadata);
      return this;
    }

    public ConfigFormatRepository AddWriter(IHelpWriter writer)
    {
      _writers.Add(writer);
      return this;
    }

    public T GetSerializationInfo<T>() where T : SerializationInfo
    {
      return Formats.OfType<T>().LastOrDefault();
    }

    public void WriteError(TextWriter writer, CommandApp app, ConfigurationException exception)
    {
      var context = new HelpContext()
      {
        App = app,
        ConfigFormats = this,
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
        .AddWriter(new DocOptWriter())
        .AddWriter(new DocbookWriter());
      if (builder != null)
      {
        foreach (var source in builder.Sources)
        {
          if (source is ICommandPipeline pipeline)
          {
            result.AddFormat(pipeline.SerializationInfo);
          }
          else if (source.GetType().FullName == "Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationSource")
          {
            var metadata = new CommandLineInfo()
            {
              PosixConventions = false
            };
            var mappings = (IDictionary<string, string>)source.GetType().GetProperty("SwitchMappings").GetValue(source);
            if (mappings != null)
            {
              foreach (var mapping in mappings)
                metadata.AddAlias(mapping.Key, mapping.Value);
            }
            result.AddFormat(metadata);
          }
          else if (source.GetType().FullName == "Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource")
          {
            var prefix = (string)source.GetType().GetProperty("Prefix").GetValue(source);
            var existing = result.Formats.OfType<EnvironmentVariableInfo>().FirstOrDefault();
            if (existing == null)
            {
              existing = new EnvironmentVariableInfo();
              result.AddFormat(existing);
            }
            existing.Prefixes.Add(prefix);
          }
          else if (source.GetType().FullName == "Microsoft.Extensions.Configuration.Json.JsonConfigurationSource")
          {
            var path = (string)source.GetType().GetProperty("Path").GetValue(source);
            if (!string.IsNullOrEmpty(path))
            {
              var existing = result.Formats.OfType<JsonSettingsInfo>().FirstOrDefault();
              if (existing == null)
              {
                existing = new JsonSettingsInfo();
                result.AddFormat(existing);
                result.AddWriter(new JsonSchemaWriter());
              }
              existing.Paths.Add(path);
            }
          }
        }
      }
      return result;
    }
  }
}

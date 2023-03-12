using Argumental.Help;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Argumental
{
  public class ConfigFormatRepository
  {
    private readonly List<ConfigSchemaWriter> _writers = new List<ConfigSchemaWriter>();

    public List<IConfigFormat> Formats { get; } = new List<IConfigFormat>();

    public ConfigFormatRepository AddFormat(IConfigFormat format)
    {
      Formats.Add(format);
      return this;
    }

    public ConfigFormatRepository AddWriter(ConfigSchemaWriter writer)
    {
      _writers.Add(writer);
      return this;
    }

    public IEnumerable<ConfigAlias> GetAliases(IProperty property)
    {
      return Formats
        .Where(f => f.Filter?.Invoke(property) != false)
        .SelectMany(f => f.GetAliases(property))
        .OrderBy(a => a.Type);
    }

    public void WriteError(TextWriter writer, AssemblyMetadata metadata, ConfigurationException exception)
    {
      var schemaWriter = _writers.First();
      schemaWriter.Configure(writer, this);
      schemaWriter.WriteHelp(metadata
        , exception.SelectedCommand == null
          ? exception.Pipeline.Commands
          : new[] { exception.SelectedCommand }
        , exception.Errors);
    }

    public static ConfigFormatRepository Default(IConfigurationBuilder builder)
    {
      var result = new ConfigFormatRepository();
      if (builder != null)
      {
        foreach (var source in builder.Sources)
        {
          if (source is ICommandPipeline pipeline)
          {
            result.AddFormat(new CommandLineFormat(pipeline.SwitchMappings, true
              , pipeline.HelpCommand?.Name.First().ToString()
              , pipeline.VersionCommand?.Name.First().ToString()));
            result.AddWriter(new DocOptSchemaWriter());
          }
          else if (source.GetType().FullName == "Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationSource")
          {
            var mappings = (IDictionary<string, string>)source.GetType().GetProperty("SwitchMappings").GetValue(source);
            result.AddFormat(new CommandLineFormat(mappings, false));
            result.AddWriter(new DocOptSchemaWriter());
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

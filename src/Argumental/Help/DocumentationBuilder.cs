using Argumental.Help;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Argumental
{
  public class DocumentationBuilder // : IConfigurationBuilder
  {
    private readonly List<IHelpWriter> _writers = new List<IHelpWriter>();
    //private IConfigurationBuilder _builder;

    private List<SerializationInfo> Formats { get; } = new List<SerializationInfo>();

    public DocumentationBuilder AddSerialization(SerializationInfo info)
    {
      Formats.Add(info);
      return this;
    }

    public DocumentationBuilder AddOrUpdateSerialization<T>(Action<T> update) where T : SerializationInfo, new()
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

    public DocumentationBuilder AddWriter(IHelpWriter writer)
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
        Section = exception.SelectedCommand == null ? DocumentationScope.Root : DocumentationScope.Command,
      };
      context.Errors.AddRange(exception.Errors);
      if (exception.SelectedCommand != null)
        context.Schemas.Add(exception.SelectedCommand);
      else if (exception.Pipeline?.Commands != null)
        context.Schemas.AddRange(exception.Pipeline.Commands);
      _writers.First().Write(context, writer);
    }

    public static DocumentationBuilder Default(IConfigurationBuilder builder)
    {
      var result = new DocumentationBuilder()
        .AddWriter(new DocOptWriter())
        .AddWriter(new DocbookWriter());
      if (builder != null)
      {
        foreach (var source in builder.Sources)
        {
          if (source is ICommandPipeline pipeline)
          {
            result.AddSerialization(pipeline.SerializationInfo);
          }
          else if (source.GetType().FullName == "Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationSource")
          {
            result.AddOrUpdateSerialization<CommandLineInfo>(c =>
            {
              var mappings = (IDictionary<string, string>)source.GetType().GetProperty("SwitchMappings").GetValue(source);
              if (mappings != null)
              {
                foreach (var mapping in mappings)
                  c.AddAlias(mapping.Key, mapping.Value);
              }
            });
          }
          else if (source.GetType().FullName == "Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource")
          {
            var prefix = (string)source.GetType().GetProperty("Prefix").GetValue(source);
            result.AddOrUpdateSerialization<EnvironmentVariableInfo>(e => e.Prefixes.Add(prefix));
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
                result.AddSerialization(existing);
                result.AddWriter(new JsonSchemaWriter());
              }
              existing.Paths.Add(path);
            }
          }
        }
      }
      return result;
    }

    //#region "IConfigurationBuilder"
    //IDictionary<string, object> IConfigurationBuilder.Properties => _builder.Properties;

    //IList<IConfigurationSource> IConfigurationBuilder.Sources => _builder.Sources;

    //IConfigurationBuilder IConfigurationBuilder.Add(IConfigurationSource source)
    //{
    //  _builder.Add(source);
    //  return this;
    //}

    //IConfigurationRoot IConfigurationBuilder.Build()
    //{
    //  return _builder.Build();
    //}
    //#endregion
  }
}

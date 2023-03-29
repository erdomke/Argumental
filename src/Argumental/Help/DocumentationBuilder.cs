using Argumental.Help;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Argumental
{
  public class DocumentationBuilder
  {
    private bool _initialized;
    private Action<DocumentationBuilder> _initializer = DefaultInitialization;

    public CommandApp App { get; set; }
    public DocbookWriter Docbook { get; }
    public Dictionary<int, string> ExitStatuses { get; } = typeof(ExitCode)
      .GetFields(BindingFlags.Public | BindingFlags.Static)
      .ToDictionary(f => (int)f.GetRawConstantValue()
      , f => f.GetCustomAttribute<DescriptionAttribute>().Description);
    public AssemblyMetadata Metadata { get; set; }
    public SerializationInfo SerializationInfo { get; set; }
    public IList<IConfigurationSource> Sources { get; set; }
    public IList<IDocbookSectionWriter> Sections { get; } = new List<IDocbookSectionWriter>();
    public IList<IHelpWriter> Writers { get; } = new List<IHelpWriter>();

    public DocumentationBuilder()
    {
      Docbook = new DocbookWriter(GetSections);
      Writers.Add(Docbook);
    }

    public DocumentationBuilder SetInitializer(Action<DocumentationBuilder> initializer)
    {
      _initializer = initializer;
      return this;
    }

    private IEnumerable<IDocbookSectionWriter> GetSections()
    {
      Initialize();
      return Sections;
    }

    public static void DefaultInitialization(DocumentationBuilder builder)
    {
      builder.Sections.Add(new FrontMatterSection(builder.Metadata));
      if (builder.App != null)
        builder.Sections.Add(new ExitCodeSection(builder.App, builder.ExitStatuses));

      builder.SerializationInfo = builder.SerializationInfo
        ?? builder.Sources?.OfType<ICommandPipeline>().FirstOrDefault()?.SerializationInfo
        ?? new CommandLineInfo();

      const string cmdConfigSource = "Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationSource";
      foreach (var group in (builder.Sources ?? Enumerable.Empty<IConfigurationSource>())
        .GroupBy(s => s is ICommandPipeline pipeline || s.GetType().FullName == cmdConfigSource
          ? cmdConfigSource
          : s.GetType().FullName))
      {
        if (group.Key == cmdConfigSource
          && builder.SerializationInfo is CommandLineInfo commandLineInfo)
        {
          foreach (var source in group)
          {
            if (source.GetType().FullName == cmdConfigSource)
            {
              var mappings = (IDictionary<string, string>)source.GetType().GetProperty("SwitchMappings").GetValue(source);
              if (mappings != null)
              {
                foreach (var mapping in mappings)
                  commandLineInfo.AddAlias(mapping.Key, mapping.Value);
              }
            }
          }
          builder.Sections.Add(new CommandOptionsSection(commandLineInfo, builder.Metadata));
          builder.Writers.Insert(0, new DocOptWriter(builder.Docbook));
        }
        else if (group.Key == "Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource")
        {
          builder.Sections.Add(new EnvironmentVariableSection(builder.SerializationInfo
            , group.Select(s => (string)s.GetType().GetProperty("Prefix").GetValue(s)).ToList()));
        }
        else if (group.Key == "Microsoft.Extensions.Configuration.Json.JsonConfigurationSource")
        {
          builder.Sections.Add(new JsonSettingsSection(builder.SerializationInfo
            , group.Select(s => (string)s.GetType().GetProperty("Path").GetValue(s)).ToList()));
          builder.Writers.Add(new JsonSchemaWriter(builder.SerializationInfo, builder.Metadata));
        }
      }
    }

    private void Initialize()
    {
      if (!_initialized)
      {
        _initializer(this);
        _initialized = true;
      }
    }

    public void WriteError(TextWriter writer, CommandApp app, ConfigurationException exception)
    {
      App = App ?? app;
      Sources = Sources
        ?? exception.ConfigurationBuilder?.Sources
        ?? App?.GetService<IConfigurationBuilder>()?.Sources;
      SerializationInfo = SerializationInfo ?? exception.Pipeline?.SerializationInfo;

      var context = new DocumentationContext()
      {
        Scope = exception.SelectedCommand == null ? DocumentationScope.Root : DocumentationScope.Command,
      };
      context.Errors.AddRange(exception.Errors);
      if (exception.SelectedCommand != null)
        context.Schemas.Add(exception.SelectedCommand);
      else if (exception.Pipeline?.Commands != null)
        context.Schemas.AddRange(exception.Pipeline.Commands);
      Initialize();
      Writers.First().Write(context, writer);
    }
  }
}

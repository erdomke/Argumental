using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental.Help
{
  public abstract class ManPageWriter : ConfigSchemaWriter
  {
    public override void Write(AssemblyMetadata metadata, IEnumerable<ISchemaProvider> schemas, IEnumerable<string> errors)
    {
      if (errors?.Any() == true)
        WriteErrors(errors);
      var schemaList = schemas.ToList();
      if (schemaList.Count == 1 
        && schemaList[0] is ICommand command
        && string.IsNullOrEmpty(command.Name.ToString()))
      {
        WriteCommand(metadata, command);
      }
      else
      {
        if (!string.IsNullOrEmpty(metadata.Description))
          WriteDescription(metadata.Description);

        WriteStartSection(SchemaSection.Usage);
        WriteStartSection(SchemaSection.Synopsis);
        WriteApplication(metadata.Name);
        WriteArgument("command", null, false);
        WriteArgument("options", null, false);
        WriteEndSection(SchemaSection.Synopsis);
        WriteEndSection(SchemaSection.Usage);

        var options = _repository.GetProperties<CommandLineFormat>(Array.Empty<IProperty>()).ToList();
        if (options.Count > 0)
        {
          WriteStartSection(SchemaSection.Options);
          foreach (var option in options)
            WriteOption(option);
          WriteEndSection(SchemaSection.Options);
        }

        if (schemaList.OfType<ICommand>().Any())
        {
          WriteStartSection(SchemaSection.Commands);
          foreach (var cmd in schemaList.OfType<ICommand>())
          {
            WriteOption(new[] { string.Join(" ", cmd.Name.Select(n => n.ToString())) }
              , (cmd.Name.Last() as ConfigSection)?.Description
              , null);
          }
          WriteEndSection(SchemaSection.Commands);
        }
      }
    }

    public override void Write(AssemblyMetadata metadata, ISchemaProvider schema, IEnumerable<string> errors)
    {
      if (errors?.Any() == true)
        WriteErrors(errors);
      WriteCommand(metadata, schema);
    }

    protected virtual void WriteErrors(IEnumerable<string> errors) { }

    public virtual void WriteCommand(AssemblyMetadata metadata, ISchemaProvider schema)
    {
      var description = ((schema as ICommand)?.Name.Last() as ConfigSection)?.Description
        ?? metadata.Description;
      if (description != null)
        WriteDescription(description);

      WriteStartSection(SchemaSection.Usage);
      WriteCommandSynopsis(metadata.Name, (schema as ICommand)?.Name ?? new ConfigPath(), schema.Properties);
      WriteEndSection(SchemaSection.Usage);

      var options = _repository.GetProperties<CommandLineFormat>(schema.Properties)
        .Where(p => !p.Property.IsPositional)
        .ToList();
      if (options.Count > 0)
      {
        WriteStartSection(SchemaSection.Options);
        foreach (var option in options)
          WriteOption(option);
        WriteEndSection(SchemaSection.Options);
      }
    }

    public virtual void WriteCommandSynopsis(string application, ConfigPath command, IEnumerable<IProperty> properties)
    {
      WriteStartSection(SchemaSection.Synopsis);
      WriteApplication(application);
      foreach (var part in command
        .OfType<ConfigSection>()
        .Where(p => !string.IsNullOrEmpty(p.Name)))
        WriteArgument(part.Name, null);
      foreach (var property in _repository.GetProperties<CommandLineFormat>(properties).Where(p => !p.IsGlobal))
      {
        var name = property.Aliases.First();
        var prompt = default(string);
        if (name.StartsWith("<"))
        {
          prompt = name.TrimStart('<').TrimEnd('>');
          name = null;
        }
        else if (name.TryFindIndex(" <", out var idx))
        {
          prompt = name.Substring(idx + 1).TrimStart('<').TrimEnd('>');
          name = name.Substring(0, idx);
        }
        WriteArgument(name
          , prompt
          , property.Property.IsRequired()
          , property.Property.Type is ArrayType);
      }
      WriteEndSection(SchemaSection.Synopsis);
    }

    public abstract void WriteDescription(string description);

    public abstract void WriteApplication(string name);

    public abstract void WriteArgument(string optionName, string prompt, bool? required = null, bool repeat = false);

    public virtual void WriteOption(ConfigProperty property)
    {
      WriteOption(property.Aliases, (property.Property.Name.Last() as ConfigSection)?.Description, property.Property.DefaultValue);
    }

    public abstract void WriteOption(IEnumerable<string> aliases, string description, object defaultValue);

    public abstract void WriteStartSection(SchemaSection section);

    public abstract void WriteEndSection(SchemaSection section);
  }
}

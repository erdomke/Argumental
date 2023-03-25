using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Argumental
{
  public class JsonSchemaWriter : IHelpWriter
  {
    public string Format => "schema+json";

    public void Write(HelpContext context, Stream stream)
    {
      var type = new ObjectType(context.Schemas.SelectMany(s => s.Properties));
    }

    private void Write(Utf8JsonWriter writer, IDataType type, IProperty property)
    {
      if (type is ObjectType objectType)
      {
        Write(writer, objectType);
      }
      else if (property.Type is ArrayType arrayType)
      {
        writer.WriteString("type", "array");
        writer.WritePropertyName("items");
        writer.WriteStartObject();
        Write(writer, arrayType.ValueType, null);
        writer.WriteEndObject();
      }
      else if (property.Type is DictionaryType dictionaryType)
      {
        writer.WriteString("type", "object");
        writer.WritePropertyName("additionalProperties");
        writer.WriteStartObject();
        Write(writer, dictionaryType.ValueType, null);
        writer.WriteEndObject();
      }
      else if (property.Type is StringType stringType)
      {
        Write(writer, stringType);
        if (property != null)
        {
          if (property.Attributes.OfType<EmailAddressAttribute>().Any())
            writer.WriteString("format", "email");
          var pattern = property.Attributes.OfType<RegularExpressionAttribute>().FirstOrDefault();
          if (!string.IsNullOrEmpty(pattern?.Pattern))
            writer.WriteString("pattern", pattern?.Pattern);
          var length = property.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
          //var minLength = property.Attributes.OfType<MinLengthAttribute>().FirstOrDefault();
          if (length?.MinimumLength > 0)
            writer.WriteNumber("minLength", length.MinimumLength);
          if (length != null
            && length.MaximumLength > 0
            && length.MaximumLength < int.MaxValue)
            writer.WriteNumber("maxLength", length.MaximumLength);
        }
      }
      else if (property.Type is NumberType numberType)
      {
        writer.WriteString("type", numberType.IsInteger ? "integer" : "number");
        var range = property?.Attributes.OfType<RangeAttribute>().FirstOrDefault();
        if (range?.Minimum != null)
        {
          writer.WritePropertyName("minimum");
          JsonSerializer.Serialize(writer, range.Minimum);
          writer.WritePropertyName("maximum");
          JsonSerializer.Serialize(writer, range.Maximum);
        }
      }
      else if (property.Type is BooleanType)
      {
        writer.WriteString("type", "boolean");
      }
    }

    private void Write(Utf8JsonWriter writer, ObjectType objectType)
    {
      writer.WriteString("type", "object");
      writer.WritePropertyName("properties");
      writer.WriteStartObject();
      foreach (var property in objectType.Properties
        .Where(p => p.Use < PropertyUse.Hidden))
      {
        writer.WritePropertyName(property.Name.ToString());
        writer.WriteStartObject();

        Write(writer, property.Type, property);
        
        var description = (property.Name.Last() as ConfigSection)?.Description
          ?? property.Type.Name?.Description;
        if (!string.IsNullOrEmpty(description))
          writer.WriteString("description", description);
        if (property.DefaultValue != null)
        {
          writer.WritePropertyName("default");
          JsonSerializer.Serialize(writer, property.DefaultValue);
        }
        
        writer.WriteEndObject();
      }
      var required = objectType.Properties
        .Where(p => p.Use == PropertyUse.Required)
        .Select(p => p.Name.ToString())
        .ToList();
      if (required.Count > 0)
      {
        writer.WritePropertyName("required");
        writer.WriteStartArray();
        foreach (var property in required)
          writer.WriteStringValue(property);
        writer.WriteEndArray();
      }
      writer.WriteBoolean("additionalProperties", false);
      writer.WriteEndObject();
    }

    private void Write(Utf8JsonWriter writer, StringType stringType)
    {
      writer.WriteString("type", "string");
      if (stringType.Type == typeof(DateTime)
        || stringType.Type == typeof(DateTimeOffset))
        writer.WriteString("format", "date-time");
      else if (stringType.Type.FullName == "System.DateOnly")
        writer.WriteString("format", "date");
      else if (stringType.Type.FullName == "System.TimeOnly")
        writer.WriteString("format", "time");
      else if (stringType.Type == typeof(TimeSpan))
        writer.WriteString("format", "duration");
      else if (stringType.Type == typeof(Uri))
        writer.WriteString("format", "uri");
      if (stringType.Enumeration?.Any() == true)
      {
        writer.WritePropertyName("enum");
        writer.WriteStartArray();
        foreach (var value in stringType.Enumeration.Where(v => !v.Hidden))
          writer.WriteStringValue(value.Value.ToString());
        writer.WriteEndArray();
      }
    }

    public void Write(HelpContext context, TextWriter writer)
    {
      throw new NotImplementedException();
    }
  }
}

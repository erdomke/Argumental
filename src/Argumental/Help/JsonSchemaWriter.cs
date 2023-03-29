using Argumental.Help;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Argumental
{
  public class JsonSchemaWriter : IHelpWriter
  {
    private readonly SerializationInfo _info;
    private readonly AssemblyMetadata _metadata;

    public string Format => "schema+json";

    public JsonSchemaWriter(SerializationInfo info, AssemblyMetadata metadata)
    {
      _info = info;
      _metadata = metadata;
    }

    public void Write(DocumentationContext context, Stream stream)
    {
      var type = new ObjectType(context.Schemas.SelectMany(s => s.Properties));
      using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions()
      {
        Indented = true
      }))
      {
        writer.WriteStartObject();
        writer.WriteString("$schema", "https://json-schema.org/draft/2020-12/schema");
        if (!string.IsNullOrEmpty(_metadata.Name))
          writer.WriteString("title", _metadata.Name);
        if (!string.IsNullOrEmpty(_metadata.Description))
          writer.WriteString("description", _metadata.Description);
        if (!string.IsNullOrEmpty(_metadata.Copyright))
          writer.WriteString("copyright", _metadata.Copyright);
        if (!string.IsNullOrEmpty(_metadata.Version))
          writer.WriteString("version", _metadata.Version);
        Write(writer, _info, type);
        writer.WriteEndObject();
      }
    }

    private void Write(Utf8JsonWriter writer, SerializationInfo info, IDataType type, IProperty property)
    {
      if (type is ObjectType objectType)
      {
        Write(writer, info, objectType);
      }
      else if (property.Type is ArrayType arrayType)
      {
        writer.WriteString("type", "array");
        if (info.TryGetListLength(property, out var minLength, out var maxLength))
        {
          if (minLength.HasValue)
            writer.WriteNumber("minItems", minLength.Value);
          if (maxLength.HasValue)
            writer.WriteNumber("maxItems", maxLength.Value);
        }
        writer.WritePropertyName("items");
        writer.WriteStartObject();
        Write(writer, info, arrayType.ValueType, null);
        writer.WriteEndObject();
      }
      else if (property.Type is DictionaryType dictionaryType)
      {
        writer.WriteString("type", "object");
        writer.WritePropertyName("additionalProperties");
        writer.WriteStartObject();
        Write(writer, info, dictionaryType.ValueType, null);
        writer.WriteEndObject();
      }
      else if (property.Type is StringType stringType)
      {
        writer.WriteString("type", "string");
        var format = DataType.Custom;
        if (property == null)
        {
          format = info.StringFormat(stringType);
        }
        else if (property != null)
        {
          format = info.StringFormat(property);
          var pattern = info.RegularExpression(property);
          if (!string.IsNullOrEmpty(pattern))
            writer.WriteString("pattern", pattern);
          if (info.TryGetStringLength(property, out var minLength, out var maxLength))
          {
            if (minLength.HasValue)
              writer.WriteNumber("minLength", minLength.Value);
            if (maxLength.HasValue)
              writer.WriteNumber("maxLength", maxLength.Value);
          }
        }

        switch (format)
        {
          case DataType.Date:
            writer.WriteString("format", "date");
            break;
          case DataType.DateTime:
            writer.WriteString("format", "date-time");
            break;
          case DataType.Duration:
            writer.WriteString("format", "duration");
            break;
          case DataType.EmailAddress:
            writer.WriteString("format", "email");
            break;
          case DataType.Time:
            writer.WriteString("format", "time");
            break;
          case DataType.Url:
            writer.WriteString("format", "uri");
            break;
          default:
            if (stringType.Type == typeof(Guid))
              writer.WriteString("format", "uuid");
            else if (stringType.Type == typeof(Regex))
              writer.WriteString("format", "regex");
            break;
        }

        if (info.TryGetEnumeration(property, out var allowMultiple, out var values))
        {
          writer.WritePropertyName("enum");
          JsonSerializer.Serialize(values
            .Where(e => !e.Hidden)
            .Select(e => e.Name)
            .ToList());
        }
      }
      else if (property.Type is NumberType numberType)
      {
        writer.WriteString("type", numberType.IsInteger ? "integer" : "number");
        if (info.TryGetNumberRange(property, out var minimum, out var minExclusive, out var maximum, out var maxExclusive))
        {
          if (minimum != null)
          {
            writer.WritePropertyName(minExclusive ? "exclusiveMinimum" : "minimum");
            JsonSerializer.Serialize(writer, minimum);
          }
          if (maximum != null)
          {
            writer.WritePropertyName(maxExclusive ? "exclusiveMaximum" : "maximum");
            JsonSerializer.Serialize(writer, maximum);
          }
        }
      }
      else if (property.Type is BooleanType)
      {
        writer.WriteString("type", "boolean");
      }
    }

    private void Write(Utf8JsonWriter writer, SerializationInfo info, ObjectType objectType)
    {
      writer.WriteString("type", "object");
      writer.WritePropertyName("properties");
      writer.WriteStartObject();
      foreach (var property in objectType.Properties
        .Where(p => info.Use(p) < PropertyUse.Hidden))
      {
        writer.WritePropertyName(property.Name.ToString());
        writer.WriteStartObject();

        Write(writer, info, property.Type, property);
        
        var description = info.Description(property);
        if (!string.IsNullOrEmpty(description))
          writer.WriteString("description", description);
        var defaultValue = info.DefaultValue(property);
        if (defaultValue != null)
        {
          writer.WritePropertyName("default");
          JsonSerializer.Serialize(writer, defaultValue);
        }
        
        writer.WriteEndObject();
      }
      var required = objectType.Properties
        .Where(p => info.Use(p) == PropertyUse.Required)
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

    public void Write(DocumentationContext context, TextWriter writer)
    {
      Write(context, new WriterStream(writer));
    }

    private class WriterStream : Stream
    {
      private TextWriter _writer;

      public override bool CanRead => false;

      public override bool CanSeek => false;

      public override bool CanWrite => true;

      public override long Length => throw new NotSupportedException();

      public override long Position 
      { 
        get => throw new NotSupportedException(); 
        set => throw new NotSupportedException(); 
      }

      public WriterStream(TextWriter writer)
      {
        _writer = writer;
      }

      protected override void Dispose(bool disposing)
      {
        if (disposing)
          _writer.Dispose();
        base.Dispose(disposing);
      }

      public override void Flush()
      {
        _writer.Flush();
      }

      public override int Read(byte[] buffer, int offset, int count)
      {
        throw new NotSupportedException();
      }

      public override long Seek(long offset, SeekOrigin origin)
      {
        throw new NotSupportedException();
      }

      public override void SetLength(long value)
      {
        throw new NotSupportedException();
      }

      public override void Write(byte[] buffer, int offset, int count)
      {
        _writer.Write(Encoding.UTF8.GetChars(buffer, offset, count));
      }
    }
  }
}

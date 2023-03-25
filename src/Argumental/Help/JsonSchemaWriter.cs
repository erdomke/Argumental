using Argumental.Help;
using System;
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
      var info = context.ConfigFormats.GetSerializationInfo<JsonSettingsInfo>();
      using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions()
      {
        Indented = true
      }))
      {
        writer.WriteStartObject();
        writer.WriteString("$schema", "https://json-schema.org/draft/2020-12/schema");
        if (!string.IsNullOrEmpty(context.Metadata.Name))
          writer.WriteString("title", context.Metadata.Name);
        if (!string.IsNullOrEmpty(context.Metadata.Description))
          writer.WriteString("description", context.Metadata.Description);
        if (!string.IsNullOrEmpty(context.Metadata.Copyright))
          writer.WriteString("copyright", context.Metadata.Copyright);
        if (!string.IsNullOrEmpty(context.Metadata.Version))
          writer.WriteString("version", context.Metadata.Version);
        Write(writer, info, type);
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
        Write(writer, stringType);
        if (property != null)
        {
          if (property.Attributes.OfType<EmailAddressAttribute>().Any())
            writer.WriteString("format", "email");
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

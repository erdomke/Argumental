using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Argumental.Test")]
namespace Argumental
{
  public class AssemblyMetadata
  {
    public DateTime? BuildDate { get; private set; }
    public string Copyright { get; private set; }
    public string Description { get; private set; }
    public string Name { get; private set; }
    public string Version { get; private set; }

    public AssemblyMetadata SetBuildDate(DateTime? buildDate)
    {
      this.BuildDate = buildDate;
      return this;
    }

    public AssemblyMetadata SetCopyright(string copyright)
    {
      this.Copyright = copyright;
      return this;
    }

    public AssemblyMetadata SetDescription(string description)
    {
      this.Description = description;
      return this;
    }

    public AssemblyMetadata SetName(string name)
    {
      this.Name = name;
      return this;
    }

    public AssemblyMetadata SetVersion(string version)
    {
      this.Version = version;
      return this;
    }

    internal static Func<AssemblyMetadata> _defaultMetadata = DefaultImplementation;

    private static AssemblyMetadata DefaultImplementation()
    {
      var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
      var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

      var buildDate = default(DateTime?);
      if (!string.IsNullOrEmpty(assembly.Location))
      {
        try
        {
          buildDate = RetrieveLinkerTimestamp(assembly.Location);
        }
        catch (Exception) { }
      }

      return new AssemblyMetadata()
        .SetBuildDate(buildDate)
        .SetCopyright(assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright)
        .SetDescription(assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description
          ?? assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title)
        .SetName(Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]))
        .SetVersion(assemblyVersionAttribute == null
          ? assembly.GetName().Version?.ToString()
          : assemblyVersionAttribute.InformationalVersion);
    }

    public static AssemblyMetadata Default()
    {
      return _defaultMetadata();
    }

    /// <summary>
    /// Retrieves the linker timestamp.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The timestamp</returns>
    /// <remarks>http://www.codinghorror.com/blog/2005/04/determining-build-date-the-hard-way.html</remarks>
    private static DateTime RetrieveLinkerTimestamp(string filePath)
    {
      const int peHeaderOffset = 60;
      const int linkerTimestampOffset = 8;
      var buffer = new byte[2048];
      using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        fileStream.Read(buffer, 0, 2048);
      return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        .AddSeconds(BitConverter.ToInt32(buffer, BitConverter.ToInt32(buffer, peHeaderOffset) + linkerTimestampOffset));
    }
  }
}

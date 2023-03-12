using System;
using System.IO;
using System.Reflection;

namespace Argumental
{
  public class AssemblyMetadata
  {
    public string Copyright { get; private set; }
    public string Description { get; private set; }
    public string Name { get; private set; }
    public string Version { get; private set; }

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

    public static AssemblyMetadata Default()
    {
      var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
      var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
      return new AssemblyMetadata()
        .SetCopyright(assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright)
        .SetDescription(assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description
          ?? assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title)
        .SetName(Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]))
        .SetVersion(assemblyVersionAttribute == null
          ? assembly.GetName().Version?.ToString()
          : assemblyVersionAttribute.InformationalVersion);
    }
  }
}

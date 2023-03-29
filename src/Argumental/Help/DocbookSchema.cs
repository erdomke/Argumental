using System.Xml.Linq;

namespace Argumental
{
  internal static class DocbookSchema
  {
    public static XNamespace docNs { get; } = "http://docbook.org/ns/docbook";

    public static XName arg { get; } = docNs + "arg";
    public static XName article { get; } = docNs + "article";
    public static XName cmdsynopsis { get; } = docNs + "cmdsynopsis";
    public static XName code { get; } = docNs + "code";
    public static XName command { get; } = docNs + "command";
    public static XName copyright { get; } = docNs + "copyright";
    public static XName date { get; } = docNs + "date";
    public static XName envar { get; } = docNs + "envar";
    public static XName errorname { get; } = docNs + "errorname";
    public static XName filename { get; } = docNs + "filename";
    public static XName holder { get; } = docNs + "holder";
    public static XName important { get; } = docNs + "important";
    public static XName info { get; } = docNs + "info";
    public static XName listitem { get; } = docNs + "listitem";
    public static XName literal { get; } = docNs + "literal";
    public static XName para { get; } = docNs + "para";
    public static XName parameter { get; } = docNs + "parameter";
    public static XName property { get; } = docNs + "property";
    public static XName releaseinfo { get; } = docNs + "releaseinfo";
    public static XName replaceable { get; } = docNs + "replaceable";
    public static XName returnvalue { get; } = docNs + "returnvalue";
    public static XName section { get; } = docNs + "section";
    public static XName term { get; } = docNs + "term";
    public static XName title { get; } = docNs + "title";
    public static XName variablelist { get; } = docNs + "variablelist";
    public static XName varlistentry { get; } = docNs + "varlistentry";
    public static XName year { get; } = docNs + "year";
  }
}

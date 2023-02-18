namespace Argumental
{
  internal class AnyListIndex : IConfigSection
  {
    public bool Matches(string segment)
    {
      return long.TryParse(segment, out var _);
    }

    public override string ToString()
    {
      return "#";
    }
  }
}

namespace Argumental
{
  internal class AnyDictKey : IConfigSection
  {
    public bool Matches(string segment)
    {
      return true;
    }

    public override string ToString()
    {
      return "*";
    }
  }
}

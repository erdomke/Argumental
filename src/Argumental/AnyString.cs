namespace Argumental
{
  internal class AnyString : IConfigSection
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

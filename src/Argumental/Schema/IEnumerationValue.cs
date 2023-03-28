namespace Argumental
{
  public interface IEnumerationValue
  {
    string Description { get; }
    bool Hidden { get; }
    string Name { get; }
    object Value { get; }
  }
}
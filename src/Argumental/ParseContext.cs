using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class ParseContext
  {
    public IDictionary<string, string> Data { get; } = new Dictionary<string, string>();
    public IList<Token> Tokens { get; }
    public bool Success { get; set; }

    internal ParseContext(IEnumerable<Token> tokens)
    {
      Tokens = tokens.ToList();
    }
  }
}

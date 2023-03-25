using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  internal static class Utilities
  {
    public static void RemoveWhere<T>(this IList<T> values, Func<T, bool> filter)
    {
      var i = 0;
      while (i < values.Count)
      {
        if (filter(values[i]))
          values.RemoveAt(i);
        else
          i++;
      }
    }
  }
}

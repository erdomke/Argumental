using System;
using System.Collections.Generic;
using System.Linq;

namespace Argumental
{
  public class Binder<TResult> : IConfigHandler<TResult>, IOptionProvider<TResult>
  {
    public IEnumerable<IProperty> Properties => Providers.SelectMany(p => p.Properties);

    public Func<InvocationContext, TResult> Handler { get; set; }

    public IList<IOptionProvider> Providers { get; } = new List<IOptionProvider>();

    public TResult Get(InvocationContext context)
    {
      try
      {
        if (Handler != null)
          return Handler.Invoke(context);
      }
      catch (Exception ex) 
      {
        context.AddError(ex);
      }
      return default;
    }

    object IOptionProvider.Get(InvocationContext context)
    {
      return Get(context);
    }
  }
}

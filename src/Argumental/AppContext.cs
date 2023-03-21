using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  public class AppContext : IServiceProvider
  {
    public AssemblyMetadata Metadata { get; }

    public AppContext(AssemblyMetadata metadata)
    {
      Metadata = metadata;
    }

    public object GetService(Type serviceType)
    {
      throw new NotImplementedException();
    }
  }
}

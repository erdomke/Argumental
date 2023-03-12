using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Argumental
{
  internal interface IConfigurationBuilderSource
  {
    IConfigurationBuilder ConfigurationBuilder { get; set;  }
  }
}

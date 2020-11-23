using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    public interface ITransportConnection : IAsyncDisposable
    {
        Stream Stream { get; }
    }
}

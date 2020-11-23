using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Workstation.ServiceModel.Ua.Channels;

namespace Workstation.ServiceModel.Ua
{
    public interface IEncodingProvider
    {
        IEncoder CreateEncoder(Stream stream, IEncodingContext? context = null, bool keepStreamOpen = false);
        IDecoder CreateDecoder(Stream stream, IEncodingContext? context = null, bool keepStreamOpen = false);
    }
}

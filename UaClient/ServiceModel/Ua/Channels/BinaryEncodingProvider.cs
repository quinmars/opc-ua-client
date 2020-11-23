using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Workstation.ServiceModel.Ua.Channels
{
    public class BinaryEncodingProvider : IEncodingProvider
    {
        public IDecoder CreateDecoder(Stream stream, IEncodingContext? context = null, bool keepStreamOpen = false)
        {
            return new BinaryDecoder(stream, context, keepStreamOpen);
        }

        public IEncoder CreateEncoder(Stream stream, IEncodingContext? context = null, bool keepStreamOpen = false)
        {
            return new BinaryEncoder(stream, context, keepStreamOpen);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Workstation.ServiceModel.Ua.Channels
{
    public static class StackProfiles
    {
        public static StackProfile BinaryUascTcp { get; }
            = new StackProfile(
                new BinaryEncodingProvider(),
                null!,
                new TcpConnectionProvider()
            );
    }
}

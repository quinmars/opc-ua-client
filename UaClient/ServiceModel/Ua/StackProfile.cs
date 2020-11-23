using System;
using System.Collections.Generic;
using System.Text;
using Workstation.ServiceModel.Ua.Channels;

namespace Workstation.ServiceModel.Ua
{
    public class StackProfile
    {
        public IEncodingProvider EncodingProvider { get; }
        public ISecureConversationProvider SecureConversationProvider { get; }
        public ITransportConnectionProvider TransportConnectionProvider { get; }
        
        public StackProfile(IEncodingProvider encodingProvider, ISecureConversationProvider secureConversationProvider, ITransportConnectionProvider transportConnectionProvider)
        {
            EncodingProvider = encodingProvider;
            SecureConversationProvider = secureConversationProvider;
            TransportConnectionProvider = transportConnectionProvider;
        }
    }
}

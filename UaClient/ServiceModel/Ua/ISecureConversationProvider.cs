using System;
using System.Collections.Generic;
using System.Text;

namespace Workstation.ServiceModel.Ua
{
    public interface ISecureConversationProvider
    {
        ISecureConversation Create(MessageSecurityMode mode);
    }
}

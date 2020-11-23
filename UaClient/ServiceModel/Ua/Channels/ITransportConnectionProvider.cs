﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua.Channels
{
    public interface ITransportConnectionProvider
    {
        Task<ITransportConnection> ConnectAsync(Uri endpoint);
    }
}

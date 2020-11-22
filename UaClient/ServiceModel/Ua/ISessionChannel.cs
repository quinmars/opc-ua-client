// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace Workstation.ServiceModel.Ua
{
    public interface ISessionChannel : ICommunicationObject, IRequestChannel, ISourceBlock<PublishResponse>, IObservable<PublishResponse>
    {
        public IReadOnlyList<string> NamespaceUris { get; }
        event EventHandler Closed;

        event EventHandler Closing;

        event EventHandler Faulted;

        event EventHandler Opened;

        event EventHandler Opening;
    }
}
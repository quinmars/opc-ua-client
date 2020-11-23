using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua.Channels
{
    public class TcpConnectionProvider : ITransportConnectionProvider
    {
        private const int ConnectTimeout = 5000;

        public async Task<ITransportConnection> ConnectAsync(Uri uri)
        {
            var client = new TcpClient { NoDelay = true };
            await client.ConnectAsync(uri.Host, uri.Port).TimeoutAfter(ConnectTimeout).ConfigureAwait(false);

            return new TcpConnection(client);
        }

        private class TcpConnection : ITransportConnection
        {
            private readonly TcpClient _client;
            
            public Stream Stream { get; }
            
            public TcpConnection(TcpClient client)
            {
                _client = client;
                Stream = client.GetStream();
            }

            public ValueTask DisposeAsync()
            {
#if NET45
                _client?.Close();
#else
                _client?.Dispose();
#endif
                return default;
            }
        }
    }
}

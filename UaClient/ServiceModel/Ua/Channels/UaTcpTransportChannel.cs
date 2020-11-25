﻿// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// A channel for communicating with OPC UA servers using the UA TCP transport profile.
    /// </summary>
    public class UaTcpTransportChannel : CommunicationObject
    {
        public const uint ProtocolVersion = 0u;
        public const uint DefaultBufferSize = 64 * 1024;
        public const uint DefaultMaxMessageSize = 16 * 1024 * 1024;
        public const uint DefaultMaxChunkCount = 4 * 1024;
        private const int _minBufferSize = 8 * 1024;
        private static readonly Task _completedTask = Task.FromResult(true);

        private readonly ILogger? _logger;
        private byte[]? _sendBuffer;
        private byte[]? _receiveBuffer;
        private readonly ITransportConnectionProvider _connectionProvider;
        private ITransportConnection? _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpTransportChannel"/> class.
        /// </summary>
        /// <param name="remoteEndpoint">The remote endpoint.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The transport channel options.</param>
        public UaTcpTransportChannel(
            EndpointDescription remoteEndpoint,
            ILoggerFactory? loggerFactory = null,
            UaTcpTransportChannelOptions? options = null,
            ITransportConnectionProvider? connectionProvider = null)
            : base(loggerFactory)
        {
            RemoteEndpoint = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));
            _logger = loggerFactory?.CreateLogger<UaTcpTransportChannel>();
            LocalReceiveBufferSize = options?.LocalReceiveBufferSize ?? DefaultBufferSize;
            LocalSendBufferSize = options?.LocalSendBufferSize ?? DefaultBufferSize;
            LocalMaxMessageSize = options?.LocalMaxMessageSize ?? DefaultMaxMessageSize;
            LocalMaxChunkCount = options?.LocalMaxChunkCount ?? DefaultMaxChunkCount;
            _connectionProvider = connectionProvider ?? new TcpConnectionProvider();
        }

        /// <summary>
        /// Gets the remote endpoint.
        /// </summary>
        public EndpointDescription RemoteEndpoint { get; }

        /// <summary>
        /// Gets the size of the receive buffer.
        /// </summary>
        public uint LocalReceiveBufferSize { get; }

        /// <summary>
        /// Gets the size of the send buffer.
        /// </summary>
        public uint LocalSendBufferSize { get; }

        /// <summary>
        /// Gets the maximum total size of a message.
        /// </summary>
        public uint LocalMaxMessageSize { get; }

        /// <summary>
        /// Gets the maximum number of message chunks.
        /// </summary>
        public uint LocalMaxChunkCount { get; }

        /// <summary>
        /// Gets the size of the remote receive buffer.
        /// </summary>
        public uint RemoteReceiveBufferSize { get; private set; }

        /// <summary>
        /// Gets the size of the remote send buffer.
        /// </summary>
        public uint RemoteSendBufferSize { get; private set; }

        /// <summary>
        /// Gets the maximum size of a message that may be sent.
        /// </summary>
        public uint RemoteMaxMessageSize { get; private set; }

        /// <summary>
        /// Gets the maximum number of message chunks that may be sent.
        /// </summary>
        public uint RemoteMaxChunkCount { get; private set; }

        /// <summary>
        /// Gets the inner TCP socket.
        /// </summary>
        //protected virtual Socket? Socket => this.tcpClient?.Client;

        /// <summary>
        /// Asynchronously sends a sequence of bytes to the remote endpoint.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        protected virtual async Task SendAsync(byte[] buffer, int offset, int count, CancellationToken token = default)
        {
            ThrowIfClosedOrNotOpening();
            var stream = _connection?.Stream ?? throw new InvalidOperationException("The connection field is null!");
            await stream.WriteAsync(buffer, offset, count, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously receives a sequence of bytes from the remote endpoint.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        protected virtual async Task<int> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken token = default)
        {
            ThrowIfClosedOrNotOpening();
            var stream = _connection?.Stream ?? throw new InvalidOperationException("The connection field is null!");
            int initialOffset = offset;
            int maxCount = count;
            int num = 0;
            count = 8;
            while (count > 0)
            {
                try
                {
                    num = await stream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    return 0;
                }

                if (num == 0)
                {
                    return 0;
                }

                offset += num;
                count -= num;
            }

            var len = BitConverter.ToUInt32(buffer, 4);
            if (len > maxCount)
            {
                throw new ServiceResultException(StatusCodes.BadResponseTooLarge);
            }

            count = (int)len - 8;
            while (count > 0)
            {
                try
                {
                    num = await stream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    return 0;
                }

                if (num == 0)
                {
                    return 0;
                }

                offset += num;
                count -= num;
            }

            return offset - initialOffset;
        }

        /// <inheritdoc/>
        protected override async Task OnOpenAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _sendBuffer = new byte[_minBufferSize];
            _receiveBuffer = new byte[_minBufferSize];

            var uri = new Uri(this.RemoteEndpoint.EndpointUrl!);
            _connection = await _connectionProvider.ConnectAsync(uri).ConfigureAwait(false);

            // send 'hello'.
            int count;
            var encoder = new BinaryEncoder(new MemoryStream(_sendBuffer, 0, _minBufferSize, true, false));
            try
            {
                encoder.WriteUInt32(null, UaTcpMessageTypes.HELF);
                encoder.WriteUInt32(null, 0u);
                encoder.WriteUInt32(null, ProtocolVersion);
                encoder.WriteUInt32(null, LocalReceiveBufferSize);
                encoder.WriteUInt32(null, LocalSendBufferSize);
                encoder.WriteUInt32(null, LocalMaxMessageSize);
                encoder.WriteUInt32(null, LocalMaxChunkCount);
                encoder.WriteString(null, uri.ToString());
                count = encoder.Position;
                encoder.Position = 4;
                encoder.WriteUInt32(null, (uint)count);
                encoder.Position = count;

                await SendAsync(_sendBuffer, 0, count, token).ConfigureAwait(false);
            }
            finally
            {
                encoder.Dispose();
            }

            // receive response
            count = await ReceiveAsync(_receiveBuffer, 0, _minBufferSize, token).ConfigureAwait(false);
            if (count == 0)
            {
                throw new ObjectDisposedException("socket");
            }

            // decode 'ack' or 'err'.
            var decoder = new BinaryDecoder(new MemoryStream(_receiveBuffer, 0, count, false, false));
            try
            {
                var type = decoder.ReadUInt32(null);
                var len = decoder.ReadUInt32(null);
                if (type == UaTcpMessageTypes.ACKF)
                {
                    var remoteProtocolVersion = decoder.ReadUInt32(null);
                    if (remoteProtocolVersion < ProtocolVersion)
                    {
                        throw new ServiceResultException(StatusCodes.BadProtocolVersionUnsupported);
                    }

                    RemoteSendBufferSize = decoder.ReadUInt32(null);
                    RemoteReceiveBufferSize = decoder.ReadUInt32(null);
                    RemoteMaxMessageSize = decoder.ReadUInt32(null);
                    RemoteMaxChunkCount = decoder.ReadUInt32(null);
                    return;
                }
                else if (type == UaTcpMessageTypes.ERRF)
                {
                    var statusCode = decoder.ReadUInt32(null);
                    var message = decoder.ReadString(null);
                    if (message != null)
                    {
                        throw new ServiceResultException(statusCode, message);
                    }

                    throw new ServiceResultException(statusCode);
                }

                throw new InvalidOperationException("UaTcpTransportChannel.OnOpenAsync received unexpected message type.");
            }
            finally
            {
                decoder.Dispose();
            }
        }

        /// <inheritdoc/>
        protected override async Task OnCloseAsync(CancellationToken token)
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        protected override async Task OnAbortAsync(CancellationToken token)
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
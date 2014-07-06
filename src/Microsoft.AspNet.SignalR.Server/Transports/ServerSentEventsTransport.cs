// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.Framework.Logging;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Transports
{
    public class ServerSentEventsTransport : ForeverTransport
    {
        private static byte[] _keepAlive = Encoding.UTF8.GetBytes("data: {}\n\n");
        private static byte[] _dataInitialized = Encoding.UTF8.GetBytes("data: initialized\n\n");

        public ServerSentEventsTransport(HttpContext context,
                                         JsonSerializer jsonSerializer,
                                         ITransportHeartbeat heartbeat,
                                         IPerformanceCounterManager performanceCounterWriter,
                                         IApplicationLifetime applicationLifetime,
                                         ILoggerFactory loggerFactory,
                                         IMemoryPool pool)
            : base(context, jsonSerializer, heartbeat, performanceCounterWriter, applicationLifetime, loggerFactory, pool)
        {
        }

        public override Task KeepAlive()
        {
            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return EnqueueOperation(state => PerformKeepAlive(state), this);
        }

        public override Task Send(PersistentResponse response)
        {
            OnSendingResponse(response);

            var context = new SendContext(this, response);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return EnqueueOperation(state => PerformSend(state), context);
        }

        protected internal override Task InitializeResponse(ITransportConnection connection)
        {
            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return base.InitializeResponse(connection)
                       .Then(s => WriteInit(s), this);
        }

        private static Task PerformKeepAlive(object state)
        {
            var transport = (ServerSentEventsTransport)state;

            transport.Context.Response.Write(new ArraySegment<byte>(_keepAlive));

            return transport.Context.Response.Flush();
        }

        private static Task PerformSend(object state)
        {
            var context = (SendContext)state;

            using (var writer = new BinaryMemoryPoolTextWriter(context.Transport.Pool))
            {
                writer.Write("data: ");
                context.Transport.JsonSerializer.Serialize(context.State, writer);
                writer.WriteLine();
                writer.WriteLine();
                writer.Flush();

                context.Transport.Context.Response.Write(writer.Buffer);
            }

            return context.Transport.Context.Response.Flush();
        }

        private static Task WriteInit(ServerSentEventsTransport transport)
        {
            transport.Context.Response.ContentType = "text/event-stream";

            // "data: initialized\n\n"
            transport.Context.Response.Write(new ArraySegment<byte>(_dataInitialized));

            return transport.Context.Response.Flush();
        }

        private class SendContext
        {
            public ServerSentEventsTransport Transport;
            public object State;

            public SendContext(ServerSentEventsTransport transport, object state)
            {
                Transport = transport;
                State = state;
            }
        }
    }
}

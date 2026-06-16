using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace CactusOSC
{

    public class OSCReceiveServer
    {
        UdpClient receiveServer;
        private string address;
        private UInt16 port;
        private ConcurrentQueue<OscMessage> messagePool;
        private CancellationTokenSource shutdownTrigger;
        private Task receiveTask;

        public OSCReceiveServer(string address, UInt16 port)
        {
            this.address = address;
            this.port = port;
            this.messagePool = new ConcurrentQueue<OscMessage>();
            this.start();


        }

        public void start()
        {
            this.shutdownTrigger = new CancellationTokenSource();
            if (this.receiveServer != null)
            {
                receiveServer.Close();
            }
            //this.receiveServer = new UdpClient(this.address, this.port);
            
            this.receiveTask = Task.Run(receiveOSC);
        }
        private List<OscMessage> getBundledMessages(object packets)
        {
            Queue<object> bundles = new Queue<object>();
            List<OscMessage> messages = new List<OscMessage>();

        }
        private async Task receiveOSC()
        {
            while (!this.shutdownTrigger.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult message = await receiveServer.ReceiveAsync();
                    object packet = message.Buffer;


                    this.messagePool.Enqueue(message);
                }
                catch (ObjectDisposedException)
                {
                    //socket closed during shutdown
                    break;
                }
                catch (SocketException)
                {
                    //socket closed during shutdown
                    break;
                }
            }

        }
        public void shutdownServer()
        {
            if (receiveServer == null)
            {
                throw new InvalidOperationException("server not started");
            }
            this.shutdownTrigger.Cancel();
            this.receiveServer.Close();
        }

        public List<OscMessage> getMessages()
        {
            OscMessage messageCache;
            List<OscMessage> received = new List<OscMessage>();
            while (messagePool.TryDequeue(out messageCache))
            {
                received.Add(messageCache);
            }

            return received;
        }
    }
}

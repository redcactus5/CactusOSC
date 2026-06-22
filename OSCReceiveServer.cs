using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace CactusOSC
{

    internal class OSCReceiveServer
    {
        UdpClient receiveServer;
        private string address;
        private UInt16 port;
        private ConcurrentQueue<byte[]> messagePool;
        private CancellationTokenSource shutdownTrigger;
        private Task receiveTask;
        private OSCPackageCompiler converter;

        public OSCReceiveServer(OSCPackageCompiler converter, string address, UInt16 port)
        {
            this.address = address;
            this.port = port;
            this.messagePool = new ConcurrentQueue<byte[]>();
            this.converter = converter;
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
        
        private async Task receiveOSC()
        {
            while (!this.shutdownTrigger.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult message = await receiveServer.ReceiveAsync();
                    


                    this.messagePool.Enqueue(message.Buffer);
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

        public List<byte[]> getMessages()
        {
            byte[] messageCache;
            List<byte[]> received = new List<byte[]>();
            while (messagePool.TryDequeue(out messageCache))
            {
                received.Add(messageCache);
            }

            return received;
        }
    }
}

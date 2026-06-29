using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace CactusOSC
{

    internal class OSCReceiveServer
    {
        UdpClient receiveServer;
        private string address;
        private UInt16 port;
        private Channel<byte[]> messagePool;
        private CancellationTokenSource shutdownTrigger;
        private Task receiveTask;
        private channelManager converterBridge;

        public OSCReceiveServer(channelManager converterBridge, string address, UInt16 port)
        {
            this.address = address;
            this.port = port;
            this.converterBridge = converterBridge;
            this.messagePool = converterBridge.getReceivedPackagesChannel();

        }

        public void start()
        {
            this.shutdownTrigger = new CancellationTokenSource();
            if (this.receiveServer != null)
            {
                receiveServer.Close();
            }
            if (this.address == null)
            {
                this.receiveServer = new UdpClient(this.port);
            }
            else
            {
                this.receiveServer = new UdpClient(this.address, this.port);
            }
            
            
            this.receiveTask = Task.Run(receiveOSC);
        }
        
        private async Task receiveOSC()
        {
            ChannelWriter<byte[]> writer =this.messagePool.Writer;
            while (!this.shutdownTrigger.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult message = await receiveServer.ReceiveAsync();



                    writer.WriteAsync(message.Buffer).AsTask().Wait();
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

        
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace CactusOSC
{
    internal class OSCSendServer
    {
        UdpClient sendServer;
        private string address;
        private UInt16 port;
        private Channel<byte[]> messageQueue;
        private CancellationTokenSource shutdownTrigger;
        private Task sendTask;
        ManualResetEventSlim finishedSend;
        private OSCPackageCompiler converter;

        public OSCSendServer(OSCPackageCompiler converter, string address, UInt16 port)
        {

            this.address = address;
            this.port = port;
            this.messageQueue = Channel.CreateUnbounded<byte[]>();
            finishedSend = new ManualResetEventSlim(false);
            this.converter = converter;
        }
        public void start()
        {
            shutdownTrigger = new CancellationTokenSource();
            if (this.sendServer != null)
            {
                sendServer.Close();
            }
            sendServer = new UdpClient(this.address, this.port);

            sendTask = Task.Run(SendOSC);
        }
        public void waitForSendFinish()
        {
            this.finishedSend.Wait();
        }
        private async Task SendOSC()
        {
            byte[] messageCache;

            while (!this.shutdownTrigger.IsCancellationRequested)
            {
                try
                {

                    while (messageQueue.Reader.TryRead(out messageCache))
                    {
                        if (!messageCache.Equals(null))
                        {
                            await sendServer.SendAsync(messageCache);
                        }

                    }
                    this.finishedSend.Set();

                    await messageQueue.Reader.WaitToReadAsync();
                    this.finishedSend.Reset();

                }
                catch (ChannelClosedException)
                {
                    break;
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
            if (sendServer == null)
            {
                throw new InvalidOperationException("Server not started");
            }
            this.shutdownTrigger.Cancel();
            this.sendServer.Close();
            this.messageQueue.Writer.Complete();

        }

        public void sendMessage(byte[] message)
        {
            if (sendServer == null)
            {
                throw new InvalidOperationException("Server not started");
            }
            this.messageQueue.Writer.TryWrite(message);

        }
    }
    
}

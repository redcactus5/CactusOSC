using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace CactusOSC
{
    public class OSCSendServer
    {
        UdpClient sendServer;
        private string address;
        private UInt16 port;
        private Channel<OscMessage> messageQueue;
        private CancellationTokenSource shutdownTrigger;
        private Task sendTask;
        ManualResetEventSlim finishedSend;


        public OSCSendServer(string address, UInt16 port)
        {

            this.address = address;
            this.port = port;
            this.messageQueue = Channel.CreateUnbounded<OscMessage>();
            finishedSend = new ManualResetEventSlim(false);
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
            OscMessage messageCache;

            while (!this.shutdownTrigger.IsCancellationRequested)
            {
                try
                {

                    while (messageQueue.Reader.TryRead(out messageCache))
                    {
                        if (!messageCache.Equals(null))
                        {
                            await sendServer.SendMessageAsync(messageCache);
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

        public void sendMessage(OscMessage message)
        {
            if (sendServer == null)
            {
                throw new InvalidOperationException("Server not started");
            }
            this.messageQueue.Writer.TryWrite(message);

        }
    }
    
}

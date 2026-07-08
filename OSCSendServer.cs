/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/
using System.Net.Sockets;

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
        private channelManager converterBridge;

        public OSCSendServer(channelManager converterBridge, string address, UInt16 port)
        {

            this.address = address;
            this.port = port;
            this.converterBridge = converterBridge;
            finishedSend = new ManualResetEventSlim(false);
            this.messageQueue=converterBridge.getPackagesToSendChannel();
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

                    await messageQueue.Reader.WaitToReadAsync(shutdownTrigger.Token);
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

       
    }
    
}

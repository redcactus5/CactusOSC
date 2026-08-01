/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/
using System.Net.Sockets;

using System.Threading.Channels;

namespace CactusOSC
{
    internal class OSCSendServer:IDisposable
    {
        UdpClient sendServer;
        private string address;
        private ushort port;
        private Channel<byte[]> messageQueue;
        private CancellationTokenSource shutdownTrigger;
        private Task sendTask;
        
        private ChannelManager converterBridge;
        private TaskCompletionSource<bool> sendFinished;
    

        public OSCSendServer(ChannelManager converterBridge, string address, ushort port)
        {

            this.address = address;
            this.port = port;
            this.converterBridge = converterBridge;
            
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
            sendFinished= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            sendTask = Task.Run(SendOSC);
        }
        public void waitForSendFinish()
        {
            
            sendFinished.Task.GetAwaiter().GetResult();
            
        }

        public async Task AsyncWaitForSendFinish()
        {
            await sendFinished.Task.WaitAsync(shutdownTrigger.Token);
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
                        if (messageCache!=null)
                        {
                            await sendServer.SendAsync(messageCache,shutdownTrigger.Token);
                        }

                    }
                    sendFinished.TrySetResult(true);

                    await messageQueue.Reader.WaitToReadAsync(shutdownTrigger.Token);
                    
                    sendFinished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                catch (OperationCanceledException)
                {
                    //user triggered shutdown
                    break;
                }

            }
        }

        public void shutdownServer()
        {
            if (sendServer == null)
            {
                throw new ServerNotStartedException();
            }
            this.shutdownTrigger.Cancel();
            this.sendServer.Close();
            byte[] garbageDisposal;
            while (messageQueue.Reader.TryRead(out garbageDisposal))
            {

            }

            this.sendTask.GetAwaiter().GetResult();
            this.sendTask = null;
            this.Dispose();
        }

        public void Dispose()
        {
            
            if(this.shutdownTrigger != null)
            {
                this.shutdownTrigger.Dispose();
                this.shutdownTrigger = null;
            }
            if(this.sendServer != null)
            {
                this.sendServer.Dispose();
                this.sendServer = null;
            }
            
            
        }
       
    }
    
}

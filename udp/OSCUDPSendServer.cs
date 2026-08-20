/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/
using System.Net.Sockets;

using System.Threading.Channels;
using System.Net;

namespace CactusOSC
{
    internal class OSCUDPSendServer:IDisposable
    {
        UdpClient sendServer;
        private IPAddress address;
        private ushort port;
        private Channel<byte[]> messageQueue;
        private CancellationTokenSource shutdownTrigger;
        private CancellationTokenSource linkedTrigger;
        private Task sendTask;
        
        private ChannelManager converterBridge;
        private TaskCompletionSource<bool> sendFinished;
        private ErrorAndShutdownCarrier Carreir;

        public OSCUDPSendServer(ChannelManager converterBridge, IPAddress address, ushort port)
        {

            this.address = address;
            this.port = port;
            this.converterBridge = converterBridge;
            
            this.messageQueue=converterBridge.getPackagesToSendChannel();
        }
        public void start(ErrorAndShutdownCarrier carreir)
        {
            shutdownTrigger = new CancellationTokenSource();
            linkedTrigger = CancellationTokenSource.CreateLinkedTokenSource(shutdownTrigger.Token, carreir.getTokenSource().Token);
            this.Carreir = carreir;
            if (this.sendServer != null)
            {
                sendServer.Close();
            }
            sendServer = new UdpClient(this.address.ToString(), this.port);
            sendFinished= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            sendTask = SendOSC();
        }
        public void waitForSendFinish()
        {
            
            this.AsyncWaitForSendFinish().GetAwaiter().GetResult();
            
        }

        public async Task AsyncWaitForSendFinish()
        {
            await sendFinished.Task.WaitAsync(linkedTrigger.Token);
        }
        private async Task SendOSC()
        {
            byte[] messageCache;

            while (!this.linkedTrigger.IsCancellationRequested)
            {
                try
                {

                    while (messageQueue.Reader.TryRead(out messageCache))
                    {
                        if (messageCache!=null)
                        {
                            await sendServer.SendAsync(messageCache,linkedTrigger.Token);
                        }

                    }
                    sendFinished.TrySetResult(true);

                    await messageQueue.Reader.WaitToReadAsync(linkedTrigger.Token);
                    
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
                catch (SocketException e)
                {
                    if (shutdownTrigger.IsCancellationRequested)
                    {
                        break;
                    }
                    else
                    {
                        Carreir.setException( e);
                    }
                    //socket closed during shutdown
                    
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
            if (this.shutdownTrigger != null)
            {
                if (!this.shutdownTrigger.IsCancellationRequested)
                {
                    this.shutdownTrigger.Cancel();
                }
                
                
            }
            
            
            byte[] garbageDisposal;
            while (messageQueue.Reader.TryRead(out garbageDisposal))
            {

            }
            if (this.sendServer != null)
            {
                this.sendServer.Close();
                this.sendServer.Dispose();
                this.sendServer = null;
            }
            if (this.sendTask != null)
            {
                this.sendTask.GetAwaiter().GetResult();
            }
            if (this.shutdownTrigger != null)
            {
                this.shutdownTrigger.Dispose();
                this.shutdownTrigger = null;
            }
            this.sendTask = null;
            if (linkedTrigger != null)
            {
                if (!linkedTrigger.IsCancellationRequested)
                {
                    linkedTrigger.Cancel();

                }
                linkedTrigger.Dispose();
                linkedTrigger = null;
            }
        }

        public void Dispose()
        {
            
            this.shutdownServer();
            
            
            
        }
       
    }
    
}

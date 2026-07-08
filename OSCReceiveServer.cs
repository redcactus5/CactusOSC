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

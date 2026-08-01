
/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/
namespace CactusOSC
{
    /// <summary>
    /// a prebuilt, ready made udp server for sending and receiving OSC packages
    /// </summary>
    public sealed class OSCServer : IDisposable
    {
        private ChannelManager channelMan;
        private DecodeEncodeServer decoderEncoder;
        private OSCReceiveServer OSCReceiver;
        private OSCSendServer OSCSender;
        private string sendIP;
        private ushort sendPort;
        private ushort receivePort;
        private string receiveIP;
        private bool started;
        private int channelCapacity;
        private bool boundedMode;
        private CancellationTokenSource shutdownTrigger;

        /// <summary>
        /// create a new osc server instance
        /// </summary>
        public OSCServer()
        {
            started = false;

        }

        
        /// <summary>
        /// receive a decoded osc package if one is avalable
        /// </summary>
        /// <param name="targetPackage"></param>
        /// <returns>bool</returns>
        /// <exception cref="ServerNotStartedException"></exception>
        public bool TryReceiveOSCPackage(out OSCPackage targetPackage)
        {
            
            if (started)
            {
                return this.decoderEncoder.tryGetDecodedPackage(out targetPackage);
            }
            else
            {
                throw new ServerNotStartedException();
            }
            
        }

        /// <summary>
        /// receive either an empty list or a list of decoded packages if packages are avalable to receive
        /// </summary>
        /// <returns>list of OSCPackage</returns>
        /// <exception cref="ServerNotStartedException"></exception>
        public List<OSCPackage> ReceiveOSCPackageList()
        {
            if (started)
            {
                return this.decoderEncoder.getDecodedPackageList();
            }
            else
            {
                throw new ServerNotStartedException();
            }
            
        }
        /// <summary>
        /// send an OSCPackage over udp
        /// </summary>
        /// <param name="packageToSend"></param>
        /// <exception cref="ServerNotStartedException"></exception>
        public void SendOSCPackage(OSCPackage packageToSend)
        {
            if (started)
            {
                this.decoderEncoder.enqueuePackageEncoding(packageToSend).GetAwaiter().GetResult();
            }
            else
            {
                throw new ServerNotStartedException();
            }
            
        }
        /// <summary>
        /// asyncronously send an OSCPackage over udp
        /// </summary>
        /// <param name="packageToSend"></param>
        /// <returns></returns>
        /// <exception cref="ServerNotStartedException"></exception>
        public async Task SendOSCPackageAsync(OSCPackage packageToSend)
        {
            if (started)
            {
                await decoderEncoder.enqueuePackageEncoding(packageToSend);
            }
            else
            {
                throw new ServerNotStartedException();
            }
            
        }

        /// <summary>
        /// send a list of OSC packages over udp, in order
        /// </summary>
        /// <param name="packageListToSend"></param>
        /// <exception cref="ServerNotStartedException"></exception>
        public void SendOSCPackageList(List<OSCPackage> packageListToSend)
        {
            if (started)
            {
                this.decoderEncoder.enqueuePackageListEncoding(packageListToSend).GetAwaiter().GetResult();
            }
            else
            {
                throw new ServerNotStartedException();
            }
            
        }
        /// <summary>
        /// asyncrounously send a list of OSCPackages, in order, over udp
        /// </summary>
        /// <param name="packageListToSend"></param>
        /// <returns></returns>
        /// <exception cref="ServerNotStartedException"></exception>
        public async Task SendOSCPackageListAsync(List<OSCPackage> packageListToSend)
        {
            if (started)
            {
                await this.decoderEncoder.enqueuePackageListEncoding(packageListToSend);
            }
            else
            {
                throw new ServerNotStartedException();
            }

        }

        /// <summary>
        /// wait for the server send queues to empty
        /// </summary>
        /// <exception cref="ServerNotStartedException"></exception>
        public void WaitForSendCompletion()
        {
            if (started)
            {
                this.decoderEncoder.waitForEncodeQueueFinish().GetAwaiter().GetResult();
                this.OSCSender.waitForSendFinish();
            }
            else
            {
                throw new ServerNotStartedException();
            }
        }

        /// <summary>
        /// asyncrounously wait for the server send queues to empty
        /// </summary>
        /// <exception cref="ServerNotStartedException"></exception>
        public async Task WaitForSendCompletionAsync()
        {
            if (started)
            {
                await this.decoderEncoder.waitForEncodeQueueFinish();
                await this.OSCSender.AsyncWaitForSendFinish();
            }
            else
            {
                throw new ServerNotStartedException();
            }
        }

        /// <summary>
        /// asyncrounously wait for an oscPackage to be added to the send list
        /// </summary>
        /// <returns>Task</returns>
        /// <exception cref="ServerNotStartedException"></exception>
        public async Task WaitForOSCPackageReceptionAsync()
        {
            if (started)
            {
                try
                {
                    await channelMan.getDecodedPackagesChannel().Reader.WaitToReadAsync(shutdownTrigger.Token);
                }
                catch (OperationCanceledException)
                {

                }

            }
            else
            {
                throw new ServerNotStartedException();
            }

        }
        /// <summary>
        /// wait for an oscPackage to be added to the send list
        /// </summary>
        /// <exception cref="ServerNotStartedException"></exception>
        public void WaitForOSCPackageReception()
        {
            if (started)
            {
                try 
                {
                    channelMan.getDecodedPackagesChannel().Reader.WaitToReadAsync(shutdownTrigger.Token).AsTask().GetAwaiter().GetResult();
                } 
                catch (OperationCanceledException)
                {

                }
                
            }  
            else
            {
                throw new ServerNotStartedException();
            }
        }

        /// <summary>
        /// check if the send queue has any open space (not recomended for asyncrounous code)
        /// </summary>
        /// <returns>bool</returns>
        /// <exception cref="ServerNotStartedException"></exception>
        public bool IsSendQueueFull()
        {
            if (started)
            {
                if (this.boundedMode)
                {
                    if (channelMan.getPackagesToEncodeChannel().Reader.Count >= this.channelCapacity)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                throw new ServerNotStartedException();
            }
        }
        /// <summary>
        /// dispose of the resources used by the server
        /// </summary>
        public void Dispose()
        {
            
            
            this.ShutDown();
        }
        /// <summary>
        /// wait for there to be space in the send queue
        /// </summary>
        /// <exception cref="ServerNotStartedException"></exception>
        public void WaitForSendQueueSpace()
        {
            if (started)
            {
                if (this.boundedMode)
                {
                    channelMan.getPackagesToSendChannel().Writer.WaitToWriteAsync().AsTask().GetAwaiter().GetResult();
                }
                
            }
            else
            {
                throw new ServerNotStartedException();
            }
            
        }
        /// <summary>
        /// asyncrounously wait for there to be space in the send queue
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ServerNotStartedException"></exception>
        public async Task WaitForSendQueueSpaceAsync()
        {
            if (started)
            {
                if (this.boundedMode)
                {
                    await channelMan.getPackagesToSendChannel().Writer.WaitToWriteAsync();
                }

            }
            else
            {
                throw new ServerNotStartedException();
            }

        }
        /// <summary>
        /// configure and start an osc server instance
        /// </summary>
        /// <param name="receivePort"></param>
        /// <param name="receiveIP"></param>
        /// <param name="sendPort"></param>
        /// <param name="sendIP"></param>
        /// <param name="parallelMode"></param>
        /// <param name="unboundedQueueMode"></param>
        /// <param name="boundedQueueSize"></param>
        /// <param name="ReceiveDropMode"></param>
        /// <exception cref="ServerAlreadyStartedException"></exception>
        public void StartOSCServer(ushort receivePort, string receiveIP, ushort sendPort, string sendIP, bool parallelMode=true, bool unboundedQueueMode=true, int boundedQueueSize=50000,OSCQueueDropMode ReceiveDropMode=OSCQueueDropMode.DropNewest,OSCQueueDropMode SendDropMode=OSCQueueDropMode.Wait)
        {
            if (!this.started)
            {
                this.receivePort = receivePort;
                this.sendPort = sendPort;
                this.sendIP = sendIP;
                this.receiveIP = receiveIP;
                shutdownTrigger = new CancellationTokenSource();
                this.boundedMode = !unboundedQueueMode;
                this.channelCapacity = boundedQueueSize;
                try
                {
                    if (this.channelMan != null)
                    {
                        this.channelMan.shutDown();
                    }

                    this.channelMan = new ChannelManager(unboundedQueueMode, boundedQueueSize, ReceiveDropMode,SendDropMode);

                    if (this.decoderEncoder != null)
                    {
                        this.decoderEncoder.shutdown();
                    }
                    this.decoderEncoder = new DecodeEncodeServer(this.channelMan);
                    if (this.OSCReceiver != null)
                    {
                        this.OSCReceiver.shutdownServer();
                    }
                    this.OSCReceiver = new OSCReceiveServer(this.channelMan, this.receiveIP, this.receivePort);
                    if (this.OSCSender != null)
                    {
                        this.OSCSender.shutdownServer();
                    }
                    this.OSCSender = new OSCSendServer(this.channelMan, this.sendIP, this.sendPort);

                    this.decoderEncoder.start(parallelMode).GetAwaiter().GetResult();
                    this.OSCSender.start();
                    this.OSCReceiver.start();




                    this.started = true;
                }
                catch(Exception error)
                {
                    this.started = true;
                    this.ShutDown();
                }
                
            }
            else
            {
                throw new ServerAlreadyStartedException();
            }
        }
        /// <summary>
        /// shut down a running OSC server instance
        /// </summary>
        /// <exception cref="ServerNotStartedException"></exception>
        public void ShutDown()
        {
            if (this.started)
            {
                shutdownTrigger.Cancel();
                if (shutdownTrigger != null)
                {
                    shutdownTrigger.Dispose();
                    shutdownTrigger = null;
                }

                if (this.decoderEncoder != null)
                {
                    this.decoderEncoder.shutdown();
                }
                this.decoderEncoder = null;
                if (this.OSCReceiver != null)
                {
                    this.OSCReceiver.shutdownServer();
                }
                this.OSCReceiver = null;
                if (this.OSCSender != null)
                {
                    this.OSCSender.shutdownServer();
                }
                this.OSCSender = null;
                if (this.channelMan != null)
                {
                    this.channelMan.shutDown();
                }
                this.channelMan = null;

                if (shutdownTrigger != null)
                {
                    shutdownTrigger.Dispose();
                    shutdownTrigger = null;
                }
                this.started = false;

            }
            else
            {
                throw new ServerNotStartedException();
            }
        }
    }
}

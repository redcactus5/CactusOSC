/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/


using System.Net;
using System.Net.NetworkInformation;


namespace CactusOSC
{
    /// <summary>
    /// a prebuilt, ready made udp server for sending and receiving OSC packages
    /// </summary>
    public sealed class OSCTCPServer : IDisposable
    {
        private ChannelManager channelMan;
        private DecodeEncodeServer decoderEncoder;
        private OSCTCPReceiveServer OSCReceiver;
        private OSCTCPSendServer OSCSender;
        private OSCTCPConnectionManager connectionMan;
        private IPAddress TargetAddress;
        private ushort TargetPort;
        private bool shouldTimeout;
        private uint timeout;
        private bool unboundedMessageSize;
        private uint maxMessageSize;
        
        private bool started;
        private int channelCapacity;
        private bool boundedMode;
        private ushort listenPort;
        private bool specificAddress;
        private bool throwOnOversizedMessage;
        private bool shuttingDown;
        

        private CancellationTokenSource shutdownTrigger;
        private ErrorAndShutdownCarrier carrier;
        /// <summary>
        /// create a new osc server instance
        /// </summary>
        public OSCTCPServer()
        {
            started = false;
            this.shuttingDown = false;
        }

        

        private void detectFatalError()
        {
            if(carrier.getException()!=null)
            {
                
                this.internalShutDown(carrier.getException());
            }
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
                this.detectFatalError();
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
                this.detectFatalError();
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
                this.detectFatalError();
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
                this.detectFatalError();
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
                this.detectFatalError();
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
                this.detectFatalError();
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
                this.detectFatalError();
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
                this.detectFatalError();
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
                this.detectFatalError();
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
                this.detectFatalError();
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
            
            
            this.internalShutDown();
        }
        /// <summary>
        /// wait for there to be space in the send queue
        /// </summary>
        /// <exception cref="ServerNotStartedException"></exception>
        public void WaitForSendQueueSpace()
        {
            if (started)
            {
                detectFatalError();
                if (this.boundedMode)
                {
                    channelMan.getPackagesToSendChannel().Writer.WaitToWriteAsync(this.shutdownTrigger.Token).AsTask().GetAwaiter().GetResult();
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
                detectFatalError();
                if (this.boundedMode)
                {
                    await channelMan.getPackagesToSendChannel().Writer.WaitToWriteAsync(this.shutdownTrigger.Token);
                }

            }
            else
            {
                throw new ServerNotStartedException();
            }

        }
        /// <summary>
        /// start the server and wait for and automatically accept a connection
        /// </summary>
        /// <param name="ListenPort"></param>
        /// <param name="specificAddress"></param>
        /// <param name="address"></param>
        /// <param name="ShouldTimeout"></param>
        /// <param name="Timeout"></param>
        /// <param name="UnboundedMessageSize"></param>
        /// <param name="MaxMessageSize"></param>
        /// <param name="ThrowOnOversizedMessage"></param>
        /// <param name="ParallelMode"></param>
        /// <param name="UnboundedQueueMode"></param>
        /// <param name="BoundedQueueSize"></param>
        /// <param name="ReceiveDropMode"></param>
        /// <param name="SendDropMode"></param>
        /// <exception cref="ServerAlreadyStartedException"></exception>
        public void AcceptConnection(ushort ListenPort, bool specificAddress = false, IPAddress Address = null, bool ShouldTimeout = true, uint Timeout = 30000, bool UnboundedMessageSize = false, uint MaxMessageSize=640000, bool ThrowOnOversizedMessage=true, bool ParallelMode=true, bool UnboundedQueueMode=true, int BoundedQueueSize=50000,OSCQueueDropMode ReceiveDropMode=OSCQueueDropMode.DropNewest,OSCQueueDropMode SendDropMode=OSCQueueDropMode.Wait)
        {
            if (this.shuttingDown)
            {
                throw new ServerShuttingDownException();
            }
            if (!this.started)
            {
                this.listenPort = ListenPort;
                this.specificAddress = specificAddress;
                this.TargetAddress = Address;
                this.shouldTimeout = ShouldTimeout;
                this.timeout = Timeout;
                this.unboundedMessageSize = UnboundedMessageSize;
                this.maxMessageSize = MaxMessageSize;
                this.throwOnOversizedMessage = ThrowOnOversizedMessage;
                
                shutdownTrigger = new CancellationTokenSource();
                this.carrier = new ErrorAndShutdownCarrier(shutdownTrigger);
                this.boundedMode = !UnboundedQueueMode;
                this.channelCapacity = BoundedQueueSize;
                try
                {
                    if (this.channelMan != null)
                    {
                        this.channelMan.shutDown();
                    }

                    this.channelMan = new ChannelManager(UnboundedQueueMode, BoundedQueueSize, ReceiveDropMode,SendDropMode);

                    if (this.decoderEncoder != null)
                    {
                        this.decoderEncoder.shutdown();
                    }
                    this.decoderEncoder = new DecodeEncodeServer(this.channelMan);
                    if (this.OSCSender != null)
                    {
                        this.OSCSender.Shutdown();
                    }
                    if (this.OSCReceiver != null)
                    {
                        this.OSCReceiver.Shutdown();
                    }
                    if (this.connectionMan != null)
                    {
                        this.connectionMan.Shutdown();

                    }
                    this.connectionMan = new OSCTCPConnectionManager();
                    this.connectionMan.AcceptConnection(this.listenPort, this.specificAddress, this.TargetAddress, this.shouldTimeout, this.timeout).GetAwaiter().GetResult();


                    this.OSCReceiver = new OSCTCPReceiveServer(this.connectionMan,this.channelMan,this.unboundedMessageSize,this.maxMessageSize,this.throwOnOversizedMessage);
                    
                    this.OSCSender = new OSCTCPSendServer(this.channelMan,this.connectionMan);

                    this.decoderEncoder.start(ParallelMode,carrier).GetAwaiter().GetResult();
                    this.OSCSender.Start(carrier);
                    this.OSCReceiver.Start(carrier);




                    this.started = true;
                }
                catch(Exception error)
                {

                    this.internalShutDown(new ServerStartupFailedException("server Startup failed!", error));
                }
                
            }
            else
            {
                throw new ServerAlreadyStartedException();
            }
        }
        /// <summary>
        /// start the server and wait for and automatically accept a connection
        /// </summary>
        /// <param name="ListenPort"></param>
        /// <param name="specificAddress"></param>
        /// <param name="address"></param>
        /// <param name="ShouldTimeout"></param>
        /// <param name="Timeout"></param>
        /// <param name="UnboundedMessageSize"></param>
        /// <param name="MaxMessageSize"></param>
        /// <param name="ThrowOnOversizedMessage"></param>
        /// <param name="ParallelMode"></param>
        /// <param name="UnboundedQueueMode"></param>
        /// <param name="BoundedQueueSize"></param>
        /// <param name="ReceiveDropMode"></param>
        /// <param name="SendDropMode"></param>
        /// <exception cref="ServerAlreadyStartedException"></exception>
        public void InitiateConnection(IPAddress Address, ushort Port, bool ShouldTimeout = true, uint Timeout = 30000, bool UnboundedMessageSize = false, uint MaxMessageSize = 640000, bool ThrowOnOversizedMessage = true, bool ParallelMode = true, bool UnboundedQueueMode = true, int BoundedQueueSize = 50000, OSCQueueDropMode ReceiveDropMode = OSCQueueDropMode.DropNewest, OSCQueueDropMode SendDropMode = OSCQueueDropMode.Wait)
        {
            if (this.shuttingDown)
            {
                throw new ServerShuttingDownException();
            }
            if (!this.started)
            {
                
                this.TargetPort = Port;
                this.TargetAddress = Address;
                this.shouldTimeout = ShouldTimeout;
                this.timeout = Timeout;
                this.unboundedMessageSize = UnboundedMessageSize;
                this.maxMessageSize = MaxMessageSize;
                this.throwOnOversizedMessage = ThrowOnOversizedMessage;

                shutdownTrigger = new CancellationTokenSource();
                this.carrier = new ErrorAndShutdownCarrier(shutdownTrigger);
                this.boundedMode = !UnboundedQueueMode;
                this.channelCapacity = BoundedQueueSize;
                try
                {
                    if (this.channelMan != null)
                    {
                        this.channelMan.shutDown();
                    }

                    this.channelMan = new ChannelManager(UnboundedQueueMode, BoundedQueueSize, ReceiveDropMode, SendDropMode);

                    if (this.decoderEncoder != null)
                    {
                        this.decoderEncoder.shutdown();
                    }
                    this.decoderEncoder = new DecodeEncodeServer(this.channelMan);
                    if (this.OSCSender != null)
                    {
                        this.OSCSender.Shutdown();
                    }
                    if (this.OSCReceiver != null)
                    {
                        this.OSCReceiver.Shutdown();
                    }
                    if (this.connectionMan != null)
                    {
                        this.connectionMan.Shutdown();

                    }
                    this.connectionMan = new OSCTCPConnectionManager();
                    this.connectionMan.InitiateConnection(this.TargetAddress,this.TargetPort,this.shouldTimeout,this.timeout).GetAwaiter().GetResult();


                    this.OSCReceiver = new OSCTCPReceiveServer(this.connectionMan, this.channelMan, this.unboundedMessageSize, this.maxMessageSize, this.throwOnOversizedMessage);

                    this.OSCSender = new OSCTCPSendServer(this.channelMan, this.connectionMan);

                    this.decoderEncoder.start(ParallelMode,carrier).GetAwaiter().GetResult();
                    this.OSCSender.Start(carrier);
                    this.OSCReceiver.Start(carrier);




                    this.started = true;
                }
                catch (Exception error)
                {

                    this.internalShutDown(new ServerStartupFailedException("server Startup failed!", error));
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

                this.internalShutDown();
            }
            else
            {
                throw new ServerNotStartedException();
            }
        }
        private void internalShutDown(Exception error=null)
        {
            this.shuttingDown = true;
            this.started = false;
                
            if (shutdownTrigger != null)
            {
                if (!this.shutdownTrigger.IsCancellationRequested)
                {
                    shutdownTrigger.Cancel();
                }
                
            }

            if (this.decoderEncoder != null)
            {
                this.decoderEncoder.shutdown();
            }
            this.decoderEncoder = null;
            if (this.OSCReceiver != null)
            {
                this.OSCReceiver.Shutdown();
            }
            this.OSCReceiver = null;
            if (this.OSCSender != null)
            {
                this.OSCSender.Shutdown();
            }
            this.OSCSender = null;
            if (this.channelMan != null)
            {
                this.channelMan.shutDown();
            }
            this.channelMan = null;
            if (this.connectionMan != null)
            {
                this.connectionMan.Shutdown();
            }
            this.connectionMan = null;
            if(this.shutdownTrigger!=null)
            {
                shutdownTrigger.Dispose();
                shutdownTrigger = null;
            }
            shuttingDown = false; 
            if (error != null)
            {
                throw  error;
            }
            
        }
    }
}


/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/
namespace CactusOSC
{
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

        public OSCServer()
        {
            started = false;

        }

        
        //receive a decoded osc package if one is avalable
        public bool tryReceiveOSCPackage(out OSCPackage targetPackage)
        {
            if (started)
            {
                return this.decoderEncoder.tryGetDecodedPackage(out targetPackage);
            }
            else
            {
                throw new serverNotStartedException();
            }
            
        }

        //receive either an empty list or a list of decoded packages if packages are avalable to receive
        public List<OSCPackage> receiveOSCPackageList()
        {
            if (started)
            {
                return this.decoderEncoder.getDecodedPackageList();
            }
            else
            {
                throw new serverNotStartedException();
            }
            
        }

        public void sendOSCPackage(OSCPackage packageToSend)
        {
            if (started)
            {
                this.decoderEncoder.enqueuePackageEncoding(packageToSend).GetAwaiter().GetResult();
            }
            else
            {
                throw new serverNotStartedException();
            }
            
        }

        public async Task SendOSCPackageAsync(OSCPackage packageToSend)
        {
            if (started)
            {
                await decoderEncoder.enqueuePackageEncoding(packageToSend);
            }
            else
            {
                throw new serverNotStartedException();
            }
            
        }


        public void sendOSCPackageList(List<OSCPackage> packageListToSend)
        {
            if (started)
            {
                this.decoderEncoder.enqueuePackageListEncoding(packageListToSend).GetAwaiter().GetResult();
            }
            else
            {
                throw new serverNotStartedException();
            }
            
        }

        public async Task sendOSCPackageListAsync(List<OSCPackage> packageListToSend)
        {
            if (started)
            {
                await this.decoderEncoder.enqueuePackageListEncoding(packageListToSend);
            }
            else
            {
                throw new serverNotStartedException();
            }

        }


        public void waitForSendCompletion()
        {
            if (started)
            {
                this.decoderEncoder.waitForEncodeQueueFinish().GetAwaiter().GetResult();
                this.OSCSender.waitForSendFinish();
            }
            else
            {
                throw new serverNotStartedException();
            }
        }

        public async Task waitForSendCompletionAsync()
        {
            if (started)
            {
                await this.decoderEncoder.waitForEncodeQueueFinish();
                await this.OSCSender.AsyncWaitForSendFinish();
            }
            else
            {
                throw new serverNotStartedException();
            }
        }

        public async Task waitForOSCPackageReceptionAsync()
        {
            if (started)
            {
                await channelMan.getReceivedPackagesChannel().Reader.WaitToReadAsync(shutdownTrigger.Token);
            }
            else
            {
                throw new serverNotStartedException();
            }

        }

        public void waitForOSCPackageReception()
        {
            if (started)
            {
                channelMan.getReceivedPackagesChannel().Reader.WaitToReadAsync(shutdownTrigger.Token).GetAwaiter().GetResult();
            }  
            else
            {
                throw new serverNotStartedException();
            }
        }

        public bool isSendQueueFull()
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
                throw new serverNotStartedException();
            }
        }

        public void Dispose()
        {
            
            if(shutdownTrigger!= null)
            {
                shutdownTrigger.Dispose();
                shutdownTrigger = null;
            }
            
        }
        public void waitForSendQueueSpace()
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
                throw new serverNotStartedException();
            }
            
        }

        public async Task waitForSendQueueSpaceAsync()
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
                throw new serverNotStartedException();
            }

        }

        public void startOSCServer(ushort receivePort, string receiveIP, ushort sendPort, string sendIP, bool unboundedQueueMode=true, int boundedQueueSize=50000,OSCQueueDropMode dropMode=OSCQueueDropMode.dropNewest)
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
                if (this.channelMan != null)
                {
                    this.channelMan.shutDown();
                }

                this.channelMan = new ChannelManager(unboundedQueueMode,boundedQueueSize,dropMode);

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

                this.decoderEncoder.start().Wait();
                this.OSCSender.start();
                this.OSCReceiver.start();
                



                this.started = true;
            }
            else
            {
                throw new serverAlreadyStartedException();
            }
        }

        public void shutDownOSCServer()
        {
            if (this.started)
            {
                shutdownTrigger.Cancel();

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

                this.Dispose();
                this.started = false;

            }
            else
            {
                throw new serverNotStartedException();
            }
        }
    }
}

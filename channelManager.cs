/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/
using System.Threading.Channels;

namespace CactusOSC
{
    public enum OSCQueueDropMode
    {
        DropOldest,
        DropNewest,
        Wait,
        DropWrite
    }
    internal class ChannelManager
    {
        private Channel<byte[]> PackagesToSend;
        private Channel<byte[]> receivedPackages;
        private Channel<OSCPackage> packagesToEncode;
        private Channel<OSCPackage> decodedPackages;
        
        public ChannelManager(bool UnboundedMode, int BoundedQueueSize,OSCQueueDropMode ReceiveDropMode,OSCQueueDropMode sendDropMode)
        {
            if (UnboundedMode)
            {
                PackagesToSend = Channel.CreateUnbounded<byte[]>();
                receivedPackages = Channel.CreateUnbounded<byte[]>();
                packagesToEncode = Channel.CreateUnbounded<OSCPackage>();
                decodedPackages=Channel.CreateUnbounded<OSCPackage>();
            }
            else
            {
                BoundedChannelOptions ReceiveOptions;
                switch (ReceiveDropMode)
                {
                    case(OSCQueueDropMode.DropOldest):
                        ReceiveOptions= new BoundedChannelOptions(BoundedQueueSize)
                        {
                            FullMode = BoundedChannelFullMode.DropOldest,
                            SingleReader = true,
                            SingleWriter = false
                        };
                        break;
                    case (OSCQueueDropMode.DropNewest):
                        ReceiveOptions = new BoundedChannelOptions(BoundedQueueSize)
                        {
                            FullMode = BoundedChannelFullMode.DropNewest,
                            SingleReader = true,
                            SingleWriter = false
                        };
                        break;
                    case (OSCQueueDropMode.Wait):
                    
                        ReceiveOptions = new BoundedChannelOptions(BoundedQueueSize)
                        {
                            FullMode = BoundedChannelFullMode.Wait,
                            SingleReader = true,
                            SingleWriter = false
                        };
                        break;
                    case (OSCQueueDropMode.DropWrite):

                        ReceiveOptions = new BoundedChannelOptions(BoundedQueueSize)
                        {
                            FullMode = BoundedChannelFullMode.DropWrite,
                            SingleReader = true,
                            SingleWriter = false
                        };
                        break;
                    default:
                        throw new InvalidOSCDropPolicyException();
                }
                BoundedChannelOptions SendOptions;
                switch (sendDropMode)
                {
                    case (OSCQueueDropMode.DropOldest):
                        SendOptions = new BoundedChannelOptions(BoundedQueueSize)
                        {
                            FullMode = BoundedChannelFullMode.DropOldest,
                            SingleReader = false,
                            SingleWriter = false
                        };
                        break;
                    case (OSCQueueDropMode.DropNewest):
                        SendOptions = new BoundedChannelOptions(BoundedQueueSize)
                        {
                            FullMode = BoundedChannelFullMode.DropNewest,
                            SingleReader = false,
                            SingleWriter = false
                        };
                        break;
                    case (OSCQueueDropMode.Wait):

                        SendOptions = new BoundedChannelOptions(BoundedQueueSize)
                        {
                            FullMode = BoundedChannelFullMode.Wait,
                            SingleReader = false,
                            SingleWriter = false
                        };
                        break;
                    case (OSCQueueDropMode.DropWrite):

                        SendOptions = new BoundedChannelOptions(BoundedQueueSize)
                        {
                            FullMode = BoundedChannelFullMode.DropWrite,
                            SingleReader = false,
                            SingleWriter = false
                        };
                        break;
                    default:
                        throw new InvalidOSCDropPolicyException();
                }
                PackagesToSend = Channel.CreateBounded<byte[]>(SendOptions);
                receivedPackages = Channel.CreateBounded<byte[]>(ReceiveOptions);
                packagesToEncode = Channel.CreateBounded<OSCPackage>(SendOptions);
                decodedPackages = Channel.CreateBounded<OSCPackage>(ReceiveOptions);
            }
            
        }
        
        public async Task transferPackageToSend(byte[] package)
        {
            ChannelWriter<byte[]> writer = PackagesToSend.Writer;
            await writer.WriteAsync(package);
            
        }
        public async Task transferReceivedPackage(byte[] package)
        {
            ChannelWriter<byte[]> writer = receivedPackages.Writer;
            await writer.WriteAsync(package);
        }

        public Channel<byte[]> getReceivedPackagesChannel()
        {
            return this.receivedPackages;
        }

        public Channel<byte[]> getPackagesToSendChannel()
        {
            return this.PackagesToSend;
        }

        public Channel<OSCPackage> getDecodedPackagesChannel()
        {
            return this.decodedPackages;
        }


        public Channel<OSCPackage> getPackagesToEncodeChannel()
        {
            return this.packagesToEncode;
        }

        public void shutDown(){
            PackagesToSend.Writer.Complete();
            receivedPackages.Writer.Complete();
            decodedPackages.Writer.Complete();
            packagesToEncode.Writer.Complete();
        }

        
    }
}

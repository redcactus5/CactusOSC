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
    internal class channelManager
    {
        private Channel<byte[]> PackagesToSend;
        private Channel<byte[]> receivedPackages;

        
        public channelManager()
        {
            PackagesToSend = Channel.CreateUnbounded<byte[]>();
            receivedPackages = Channel.CreateUnbounded<byte[]>();
        }
        
        public void transferPackageToSend(byte[] package)
        {
            ChannelWriter<byte[]> writer = PackagesToSend.Writer;
            writer.WriteAsync(package).AsTask().Wait();
            
        }
        public void transferReceivedPackage(byte[] package)
        {
            ChannelWriter<byte[]> writer = receivedPackages.Writer;
            writer.WriteAsync(package).AsTask().Wait();
        }

        public Channel<byte[]> getReceivedPackagesChannel()
        {
            return this.receivedPackages;
        }

        public Channel<byte[]> getPackagesToSendChannel()
        {
            return this.PackagesToSend;
        }
        public void shutDown(){
            PackagesToSend.Writer.Complete();
            receivedPackages.Writer.Complete();
        }
    }
}

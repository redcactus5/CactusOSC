using System;
using System.Collections.Generic;
using System.Text;
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

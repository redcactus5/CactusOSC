
/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/
namespace CactusOSC
{
    public sealed class OSCServer
    {
        private channelManager channelMan;
        private decodeEncodeServer decodeEncodeServer;
        private OSCReceiveServer OSCReceiveServer;
        private OSCSendServer OSCSendServer;
        private string sendIP;
        private ushort sendPort;
        private ushort receivePort;
        private string receiveIP;
        private bool started=false;

        public OSCServer(ushort receivePort, string receiveIP, ushort  sendPort, string sendIP)
        {
            this.receivePort = receivePort;
            this.sendPort = sendPort;
            this.sendIP = sendIP;
            this.receiveIP = receiveIP;

            

        }

        
        //receive a decoded osc package if one is avalable
        public bool tryReceiveOSCPackage(out OSCPackage targetPackage)
        {
            if (started)
            {
                return this.decodeEncodeServer.tryGetDecodedPackage(out targetPackage);
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
                return this.decodeEncodeServer.getDecodedPackageList();
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
                this.decodeEncodeServer.enqueuePackageEncoding(packageToSend).Wait();
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
                this.decodeEncodeServer.enqueuePackageListEncoding(packageListToSend).Wait();
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
                this.decodeEncodeServer.waitForEncodequeueFinish().Wait();
                this.OSCSendServer.waitForSendFinish();
            }
            else
            {
                throw new serverNotStartedException();
            }
        }

        public void startOSCServer()
        {
            if (!this.started)
            {
                if (this.channelMan != null)
                {
                    this.channelMan.shutDown();
                }
                this.channelMan = new channelManager();

                if (this.decodeEncodeServer != null)
                {
                    this.decodeEncodeServer.shutdown();
                }
                this.decodeEncodeServer = new decodeEncodeServer(this.channelMan);
                if (this.OSCReceiveServer != null)
                {
                    this.OSCReceiveServer.shutdownServer();
                }
                this.OSCReceiveServer = new OSCReceiveServer(this.channelMan, this.receiveIP, this.receivePort);
                if (this.OSCSendServer != null)
                {
                    this.OSCSendServer.shutdownServer();
                }
                this.OSCSendServer = new OSCSendServer(this.channelMan, this.sendIP, this.sendPort);

                this.OSCSendServer.start();
                this.OSCReceiveServer.start();
                this.decodeEncodeServer.start().Wait();
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
                if (this.channelMan != null)
                {
                    this.channelMan.shutDown();
                }
                this.channelMan = null;

                if (this.decodeEncodeServer != null)
                {
                    this.decodeEncodeServer.shutdown();
                }
                this.decodeEncodeServer = null;
                if (this.OSCReceiveServer != null)
                {
                    this.OSCReceiveServer.shutdownServer();
                }
                this.OSCReceiveServer = null;
                if (this.OSCSendServer != null)
                {
                    this.OSCSendServer.shutdownServer();
                }
                this.OSCSendServer = null;
                if (this.channelMan != null)
                {
                    this.channelMan.shutDown();
                }
                this.channelMan = null;

                this.started = false;

            }
            else
            {
                throw new serverNotStartedException();
            }
        }
    }
}

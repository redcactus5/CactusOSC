
/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/

using System.Collections.Concurrent;

using System.Threading.Channels;

namespace CactusOSC
{
    internal class decodeEncodeServer
    {
        private Task EncoderServer;
        private Task DecoderServer;

        private Channel<OSCPackage> packagesToEncode;
        private ConcurrentQueue<OSCPackage> decodedPackages;
        private channelManager serverBridge;

        private CancellationTokenSource shutDownTrigger;

        private ConcurrentQueue<OSCPackageCompiler> compilers;
        private OSCPackageInterpreter interpreter;
        private SemaphoreSlim encodeGate;

        private TaskCompletionSource<bool> encodeFinishedTcs;
        private int inFlightCount;
        private SemaphoreSlim inFlightCountGate;


        public decodeEncodeServer(channelManager serverBridge)
        {
            
            this.packagesToEncode = Channel.CreateUnbounded<OSCPackage>();
            this.decodedPackages =  new ConcurrentQueue<OSCPackage>();
            
            
            this.compilers = new ConcurrentQueue<OSCPackageCompiler>();
            
            this.interpreter = new OSCPackageInterpreter();
            this.encodeGate = new SemaphoreSlim(1);
            
            this.serverBridge = serverBridge;

            this.inFlightCountGate = new SemaphoreSlim(1);
            
            
            
        }

        public async Task start()
        {
            if(this.shutDownTrigger != null)
            {
                if (!this.shutDownTrigger.IsCancellationRequested)
                {
                    this.shutDownTrigger.Cancel();
                    await DecoderServer;
                    await EncoderServer;
                }
            }
            this.shutDownTrigger = new CancellationTokenSource();
            
            decodedPackages.Clear();
            

            this.EncoderServer = this.encodeService();
            this.DecoderServer = this.DecodingService();

            if (this.encodeFinishedTcs != null)
            {
                this.encodeFinishedTcs.SetResult(true);
                this.encodeFinishedTcs = null;
            }
            this.encodeFinishedTcs=new TaskCompletionSource<bool>();
            this.inFlightCount = 0;
            this.inFlightCountGate=new SemaphoreSlim(1);


        }


        public void shutdown()
        {
            if (!this.shutDownTrigger.IsCancellationRequested)
            {
                this.shutDownTrigger.Cancel();
                
            }
            if (this.inFlightCountGate.CurrentCount ==0)
            {
                this.inFlightCountGate.Release();
            }
            
        }

        

        public async Task enqueuePackageListEncoding(List<OSCPackage> packageList)
        {
            await encodeGate.WaitAsync();
            ChannelWriter<OSCPackage> writer = this.packagesToEncode.Writer;
            await this.inFlightCountGate.WaitAsync();
            this.inFlightCount += packageList.Count;
            if (this.encodeFinishedTcs == null)
            {
                this.encodeFinishedTcs=new TaskCompletionSource<bool>();
            }
            this.inFlightCountGate.Release();
            for (int i = 0; i < packageList.Count; i++)
            {
                await writer.WriteAsync(packageList[i]);
            }
            
            encodeGate.Release();
        }

        public async Task enqueuePackageEncoding(OSCPackage package)
        {
            ChannelWriter<OSCPackage> writer = this.packagesToEncode.Writer;
            await this.inFlightCountGate.WaitAsync();
            this.inFlightCount++;
            if (this.encodeFinishedTcs == null)
            {
                this.encodeFinishedTcs = new TaskCompletionSource<bool>();
            }
            this.inFlightCountGate.Release();
             await writer.WriteAsync(package);
        }

        
        public bool tryGetDecodedPackage(out OSCPackage package)
        {
            return this.decodedPackages.TryDequeue(out package);
        }
        
        public List<OSCPackage> getDecodedPackageList()
        {
            List<OSCPackage> tempMailbox = new List<OSCPackage>(this.decodedPackages.Count);
            OSCPackage packageCache;
            while(this.decodedPackages.TryDequeue(out packageCache))
            {
                tempMailbox.Add(packageCache);
            }
            return tempMailbox;

        }

        public async Task waitForEncodequeueFinish()
        {
            await this.encodeFinishedTcs.Task;
        }

        private byte[] encodePackage(OSCPackage package)
        {
            OSCPackageCompiler compiler;
            if (!this.compilers.TryDequeue(out compiler))
            {
                compiler = new OSCPackageCompiler();
            }
            if (package.getPackageType() == OSCPackageType.OSCBundle)
            {
                RawOSCPackage tempRawPackage = new RawOSCPackage(package.getSize());
                compiler.convertOSCBundleToByteArray(((OSCBundle)package), tempRawPackage);
                this.compilers.Enqueue(compiler);
                return tempRawPackage.getRawData();

            }
            else
            {
                RawOSCPackage tempRawPackage = new RawOSCPackage(package.getSize());
                compiler.convertOSCMessageToByteArray(((OSCMessage)package), tempRawPackage);
                this.compilers.Enqueue(compiler);
                return tempRawPackage.getRawData();
            }
        }


        private OSCPackage decodePackage(byte[] rawPackage)
        {
            return this.interpreter.convertOSCByteArrayToPackage(rawPackage);
        }


        private async Task DecodingService()
        {
            ChannelReader<byte[]> reader = this.serverBridge.getReceivedPackagesChannel();
            byte[][] input=new byte[1024][];
            int inputIndex = 0;
            
            byte[] inputCache;
            
            OSCPackage[] finishArray=new OSCPackage[1024];
            
            while (!this.shutDownTrigger.IsCancellationRequested)
            {
                await reader.WaitToReadAsync(shutDownTrigger.Token);
                
                
                while(reader.TryRead(out inputCache))
                {
                    input[inputIndex]=(inputCache);
                    inputIndex++;
                    if (inputIndex >= 1024)
                    {
                        break;
                    }
                        
                }
                
                Parallel.For(0,inputIndex,currentElement =>
                {
                    
                    try
                    {
                        OSCPackage packageCache = this.decodePackage(input[currentElement]);
                        finishArray[currentElement] = packageCache;
                    }
                    catch(InvalidPackageException e)
                    {
                        finishArray[currentElement] = null;
                        Console.WriteLine(e);
                        
                    }
                    
                });
                
                for (int i = 0; i < inputIndex; i++)
                {
                    if (finishArray[i] != null)
                    {
                        this.decodedPackages.Enqueue(finishArray[i]);
                    }
                    
                }
                inputIndex = 0;
            }
        }


        private async Task encodeService()
        {
            ChannelReader<OSCPackage> reader = packagesToEncode.Reader;
            OSCPackage[] input = new OSCPackage[1024];
            int inputIndex = 0;

            OSCPackage inputCache;

            byte[][] finishArray = new byte[1024][];
            while (!this.shutDownTrigger.IsCancellationRequested)
            {
                await reader.WaitToReadAsync(shutDownTrigger.Token);
                await encodeGate.WaitAsync();
                encodeGate.Release();
                while (reader.TryRead(out inputCache))
                {
                    input[inputIndex] = (inputCache);
                    inputIndex++;
                    if (inputIndex >= 1024)
                    {
                        break;
                    }

                }

                Parallel.For(0, inputIndex, currentElement =>
                {
                    finishArray[currentElement] = (this.encodePackage(input[currentElement]));
                });

                for (int i = 0; i < inputIndex; i++)
                {
                    this.serverBridge.transferPackageToSend(finishArray[i]);
                }
                await this.inFlightCountGate.WaitAsync();
                if(this.encodeFinishedTcs != null)
                {
                    this.inFlightCount -= inputIndex;
                    if (this.inFlightCount == 0)
                    {
                        this.encodeFinishedTcs.SetResult(true);
                        this.encodeFinishedTcs = null;
                    }
                }
                this.inFlightCountGate.Release();
                inputIndex = 0;
            }
        }

    }
}

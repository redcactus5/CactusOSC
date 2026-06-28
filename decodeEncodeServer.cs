using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
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
        

        public decodeEncodeServer(channelManager serverBridge)
        {
            
            this.packagesToEncode = Channel.CreateUnbounded<OSCPackage>();
            this.decodedPackages =  new ConcurrentQueue<OSCPackage>();
            
            
            this.compilers = new ConcurrentQueue<OSCPackageCompiler>();
            
            this.interpreter = new OSCPackageInterpreter();
            this.encodeGate = new SemaphoreSlim(1);
            
            this.serverBridge = serverBridge;
            
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

        }


        public void shutdown()
        {
            if (!this.shutDownTrigger.IsCancellationRequested)
            {
                this.shutDownTrigger.Cancel();
            }
        }

        

        public void enqueuePackageArrayEncoding(OSCPackage[] packageList)
        {
            encodeGate.Wait();
            ChannelWriter<OSCPackage> writer = this.packagesToEncode.Writer;
            for (int i = 0; i < packageList.Length; i++)
            {
                writer.WriteAsync(packageList[i]).AsTask().Wait();
            }
            
            encodeGate.Release();
        }

        public void enqueuePackageEncoding(OSCPackage package)
        {
            ChannelWriter<OSCPackage> writer = this.packagesToEncode.Writer;
            writer.WriteAsync(package).AsTask().Wait();
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
            OSCPackage packageCache;
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
                        packageCache = this.decodePackage(input[currentElement]);
                        finishArray[currentElement] = packageCache;
                    }
                    catch(InvalidPackageException)
                    {
                        finishArray[currentElement] = null;
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
                inputIndex = 0;
            }
        }

    }
}

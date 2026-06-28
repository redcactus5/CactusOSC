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
        private Channel<byte[]> packagesToDecode;
        private ConcurrentQueue<OSCPackage> decodedPackages;
        private ConcurrentQueue<byte[]> encodedPackages;

        private CancellationTokenSource shutDownTrigger;

        private ConcurrentQueue<OSCPackageCompiler> compilers;
        private OSCPackageInterpreter interpreter;
        private SemaphoreSlim encodeGate;
        private SemaphoreSlim decodeGate;

        public decodeEncodeServer()
        {
            this.packagesToDecode = Channel.CreateUnbounded<byte[]>();
            this.packagesToEncode = Channel.CreateUnbounded<OSCPackage>();
            this.decodedPackages =  new ConcurrentQueue<OSCPackage>();
            this.encodedPackages = new ConcurrentQueue<byte[]>();
            
            this.compilers = new ConcurrentQueue<OSCPackageCompiler>();
            
            this.interpreter = new OSCPackageInterpreter();
            this.encodeGate = new SemaphoreSlim(1);
            this.decodeGate = new SemaphoreSlim(1);
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
            encodedPackages.Clear();
            decodedPackages.Clear();
            if(this.decodeGate.CurrentCount == 0)
            {
                this.decodeGate.Release();
            }
            if(this.encodeGate.CurrentCount == 0)
            {
                this.encodeGate.Release();
            }
            if (this.packagesToDecode != null)
            {
                this.packagesToDecode.Writer.Complete();
            }
            this.packagesToDecode = Channel.CreateUnbounded<byte[]>();
            if(this.packagesToEncode != null)
            {
                this.packagesToEncode.Writer.Complete();
            }
            this.packagesToEncode = Channel.CreateUnbounded<OSCPackage>();

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

        public bool tryGetEncodedPackage(out byte[] target)
        {
            return this.encodedPackages.TryDequeue(out target);
        }
        public bool tryGetDecodedPackage(out OSCPackage target)
        {
            return this.decodedPackages.TryDequeue(out target);
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

        public void enqueuePackageArrayDecoding(byte[][] packageList)
        {
            decodeGate.Wait();
            ChannelWriter<byte[]> writer = this.packagesToDecode.Writer;
            for (int i = 0; i < packageList.Length; i++)
            {
                writer.WriteAsync(packageList[i]).AsTask().Wait();
            }

            decodeGate.Release();
        }

        public void enqueuePackageDecoding(byte[] package)
        {
            ChannelWriter<byte[]> writer = this.packagesToDecode.Writer;
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
            ChannelReader<byte[]> reader = packagesToDecode.Reader;
            byte[][] input=new byte[1024][];
            int inputIndex = 0;
            
            byte[] inputCache;
            
            OSCPackage[] finishArray=new OSCPackage[1024];
            OSCPackage packageCache;
            while (!this.shutDownTrigger.IsCancellationRequested)
            {
                await reader.WaitToReadAsync(shutDownTrigger.Token);
                await decodeGate.WaitAsync();
                decodeGate.Release();
                
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
                    this.encodedPackages.Enqueue(finishArray[i]);
                }
                inputIndex = 0;
            }
        }

    }
}

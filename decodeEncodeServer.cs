
/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/


using System.Threading.Channels;

namespace CactusOSC
{
    internal class DecodeEncodeServer:IDisposable
    {
        private Task EncoderServer;
        private Task DecoderServer;

        private Channel<OSCPackage> packagesToEncode;
        private Channel<OSCPackage> decodedPackages;
        private ChannelManager channelKeeper;

        private CancellationTokenSource shutDownTrigger;

        private OSCPackageCompiler compiler;
        private OSCPackageInterpreter interpreter;
        private SemaphoreSlim encodeGate;

        private TaskCompletionSource<bool> encodeFinishedTcs;
        private int inFlightCount;
        private SemaphoreSlim inFlightCountGate;
        private int CoreCount;
        private const int reserveCores = 4;
        
        public DecodeEncodeServer(ChannelManager channelKeeper)
        {


            this.compiler = new OSCPackageCompiler();
            
            this.interpreter = new OSCPackageInterpreter();
            this.encodeGate = new SemaphoreSlim(1);
            
            this.channelKeeper = channelKeeper;

            this.packagesToEncode = channelKeeper.getPackagesToEncodeChannel();
            this.decodedPackages =  channelKeeper.getDecodedPackagesChannel();
            this.inFlightCountGate = new SemaphoreSlim(1);
            
            
            
        }

        public async Task start(bool parallelMode)
        {
            if(this.shutDownTrigger != null)
            {
                if (!this.shutDownTrigger.IsCancellationRequested)
                {
                    this.shutdown();
                }
            }
            this.shutDownTrigger = new CancellationTokenSource();




            if (this.encodeFinishedTcs != null)
            {
                this.encodeFinishedTcs.SetResult(true);
                this.encodeFinishedTcs = null;

            }

            this.CoreCount = Math.Max(1, (Environment.ProcessorCount-reserveCores));
            
            this.encodeFinishedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.inFlightCount = 0;
            this.inFlightCountGate = new SemaphoreSlim(1);

            if (parallelMode)
            {
                this.EncoderServer = this.ParallelEncodingService();
                this.DecoderServer = this.ParallelDecodingService();
            }
            else
            {
                this.EncoderServer = this.SerialEncodingService();
                this.DecoderServer = this.SerialDecodingService();
            }
            



        }


        public void shutdown()
        {
            
            
            this.Dispose();
            
            
        }

        public void Dispose()
        {
            if (!this.shutDownTrigger.IsCancellationRequested)
            {
                this.shutDownTrigger.Cancel();

            }
            DecoderServer.GetAwaiter().GetResult();
            this.DecoderServer = null;
            EncoderServer.GetAwaiter().GetResult();
            this.EncoderServer = null;
            if (this.shutDownTrigger != null)
            {
                this.shutDownTrigger.Dispose();
                this.encodeGate.Dispose();
                this.inFlightCountGate.Dispose();
                this.encodeFinishedTcs = null;
            }
        }
        

        public async Task enqueuePackageListEncoding(List<OSCPackage> packageList)
        {
            try
            {
                await encodeGate.WaitAsync(this.shutDownTrigger.Token);
                ChannelWriter<OSCPackage> writer = this.packagesToEncode.Writer;
                await this.inFlightCountGate.WaitAsync(this.shutDownTrigger.Token);
                this.inFlightCount += packageList.Count;
                if (((this.encodeFinishedTcs != null) && (this.encodeFinishedTcs.Task.IsCompleted)) || (this.encodeFinishedTcs == null))
                {
                    this.encodeFinishedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                }
                this.inFlightCountGate.Release();
                for (int i = 0; i < packageList.Count; i++)
                {
                    await writer.WriteAsync(packageList[i], this.shutDownTrigger.Token);
                }

                encodeGate.Release();
            }
            catch (OperationCanceledException)
            {

            }
            
        }

        public async Task enqueuePackageEncoding(OSCPackage package)
        {
            try
            {
                ChannelWriter<OSCPackage> writer = this.packagesToEncode.Writer;
                await this.inFlightCountGate.WaitAsync(this.shutDownTrigger.Token);
                this.inFlightCount++;
                if (((this.encodeFinishedTcs != null) && (this.encodeFinishedTcs.Task.IsCompleted)) || (this.encodeFinishedTcs == null))
                {
                    this.encodeFinishedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                }
                this.inFlightCountGate.Release();
                
                await writer.WriteAsync(package, this.shutDownTrigger.Token);
            }
            catch(OperationCanceledException)
            {

            }
            

        }

        
        public bool tryGetDecodedPackage(out OSCPackage package)
        {
            return this.decodedPackages.Reader.TryRead(out package);
        }
        
        public List<OSCPackage> getDecodedPackageList()
        {
            List<OSCPackage> tempMailbox = new List<OSCPackage>(this.decodedPackages.Reader.Count);
            OSCPackage packageCache;
            while(this.decodedPackages.Reader.TryRead(out packageCache))
            {
                tempMailbox.Add(packageCache);
            }
            return tempMailbox;

        }

        public async Task waitForEncodeQueueFinish()
        {
            TaskCompletionSource<bool> tcs = this.encodeFinishedTcs;
            if (tcs != null)
            {
                await tcs.Task;
            }
            
        }

        private byte[] encodePackage(OSCPackage package)
        {
            
            RawOSCPackage tempRawPackage = new RawOSCPackage(package.GetSize());
            
            switch (package.GetPackageType())
            {
                case OSCPackageType.OSCBundle:
                    
                    compiler.convertOSCBundleToByteArray(((OSCBundle)package), tempRawPackage);

                    return tempRawPackage.getRawData();

                case OSCPackageType.OSCMessage:
                    
                    compiler.convertOSCMessageToByteArray(((OSCMessage)package), tempRawPackage);
                    return tempRawPackage.getRawData();

                default:
                    throw new InvalidPackageException();
            }
            
        }


        private OSCPackage decodePackage(byte[] rawPackage)
        {
            return this.interpreter.convertOSCByteArrayToPackage(rawPackage);
        }


        private async Task ParallelDecodingService()
        {
            if (this.DecoderServer != null)
            {
                throw new EncodeDecodeTaskAlreadyRunningException();
            }
            ChannelReader<byte[]> reader = this.channelKeeper.getReceivedPackagesChannel();
            ChannelWriter<OSCPackage> writer = this.decodedPackages.Writer;
            byte[][] input=new byte[1024][];
            int inputIndex = 0;
            ParallelOptions swarmOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = this.CoreCount
            };
            byte[] inputCache;
            
            OSCPackage[] finishArray=new OSCPackage[1024];
            try
            {
                while (!this.shutDownTrigger.IsCancellationRequested)
                {
                    await reader.WaitToReadAsync(shutDownTrigger.Token);


                    while (reader.TryRead(out inputCache))
                    {
                        input[inputIndex] = (inputCache);
                        inputIndex++;
                        if (inputIndex >= 1024)
                        {
                            break;
                        }

                    }

                    Parallel.For(0, inputIndex, swarmOptions, currentElement =>
                    {
                        try
                        {
                            OSCPackage packageCache = this.decodePackage(input[currentElement]);
                            finishArray[currentElement] = packageCache;
                        }
                        catch (InvalidPackageException e)
                        {
                            finishArray[currentElement] = null;
                        }

                    });
                    Array.Clear(input, 0, input.Length);
                    for (int i = 0; i < inputIndex; i++)
                    {
                        if (finishArray[i] != null)
                        {
                            await writer.WriteAsync(finishArray[i], this.shutDownTrigger.Token);
                        }

                    }
                    inputIndex = 0;
                    Array.Clear(finishArray,0, finishArray.Length);

                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (ObjectDisposedException)
            {

            }

        }

        private async Task SerialDecodingService()
        {
            if (this.DecoderServer != null)
            {
                throw new EncodeDecodeTaskAlreadyRunningException();
            }
            ChannelReader<byte[]> reader = this.channelKeeper.getReceivedPackagesChannel();
            ChannelWriter<OSCPackage> writer = this.decodedPackages.Writer;
            byte[][] input = new byte[1024][];
            int inputIndex = 0;
            byte[] inputCache;
            OSCPackage[] finishArray = new OSCPackage[1024];
            try
            {
                while (!this.shutDownTrigger.IsCancellationRequested)
                {
                    await reader.WaitToReadAsync(shutDownTrigger.Token);


                    while (reader.TryRead(out inputCache))
                    {
                        input[inputIndex] = (inputCache);
                        inputIndex++;
                        if (inputIndex >= 1024)
                        {
                            break;
                        }

                    }

                    for(int ProcessesIndex = 0; ProcessesIndex< inputIndex; ProcessesIndex++ ) 
                    {
                        try
                        {
                            OSCPackage packageCache = this.decodePackage(input[ProcessesIndex]);
                            finishArray[ProcessesIndex] = packageCache;
                        }
                        catch (InvalidPackageException e)
                        {
                            finishArray[ProcessesIndex] = null;
                        }

                    }
                    Array.Clear(input, 0, input.Length);
                    for (int i = 0; i < inputIndex; i++)
                    {
                        if (finishArray[i] != null)
                        {
                            await writer.WriteAsync(finishArray[i], this.shutDownTrigger.Token);
                        }

                    }
                    inputIndex = 0;
                    Array.Clear(finishArray, 0, finishArray.Length);

                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (ObjectDisposedException)
            {

            }

        }

        private async Task ParallelEncodingService()
        {
            if (this.EncoderServer != null)
            {
                throw new EncodeDecodeTaskAlreadyRunningException();
            }
            ChannelReader<OSCPackage> reader = packagesToEncode.Reader;
            OSCPackage[] input = new OSCPackage[1024];
            int inputIndex = 0;
            ParallelOptions swarmOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = this.CoreCount
            };
            OSCPackage inputCache;

            byte[][] finishArray = new byte[1024][];
            try
            {
                while (!this.shutDownTrigger.IsCancellationRequested)
                {
                    await reader.WaitToReadAsync(shutDownTrigger.Token);
                    await encodeGate.WaitAsync(this.shutDownTrigger.Token);
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

                    Parallel.For(0, inputIndex, swarmOptions, currentElement =>
                    {
                        finishArray[currentElement] = (this.encodePackage(input[currentElement]));
                    });
                    Array.Clear(input, 0, input.Length);
                    for (int i = 0; i < inputIndex; i++)
                    {
                        await this.channelKeeper.transferPackageToSend(finishArray[i]);
                    }
                    Array.Clear(finishArray, 0, finishArray.Length);
                    await this.inFlightCountGate.WaitAsync(this.shutDownTrigger.Token);
                    if ((this.encodeFinishedTcs != null)&&(!this.encodeFinishedTcs.Task.IsCompleted))
                    {
                        this.inFlightCount -= inputIndex;
                        if (this.inFlightCount == 0)
                        {
                            this.encodeFinishedTcs.SetResult(true);
                            
                        }
                    }
                    this.inFlightCountGate.Release();
                    inputIndex = 0;
                }

            }
            catch (OperationCanceledException)
            {

            }
            catch (ObjectDisposedException){

            } 
        }

        private async Task SerialEncodingService()
        {
            if(this.EncoderServer != null)
            {
                throw new EncodeDecodeTaskAlreadyRunningException();
            }
            ChannelReader<OSCPackage> reader = packagesToEncode.Reader;
            OSCPackage[] input = new OSCPackage[1024];
            int inputIndex = 0;
            OSCPackage inputCache;
            byte[][] finishArray = new byte[1024][];
            try
            {
                while (!this.shutDownTrigger.IsCancellationRequested)
                {
                    await reader.WaitToReadAsync(shutDownTrigger.Token);
                    await encodeGate.WaitAsync(this.shutDownTrigger.Token);
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

                    for(int ProcessIndex=0; ProcessIndex<inputIndex;  ProcessIndex++) 
                    {
                        finishArray[ProcessIndex] = (this.encodePackage(input[ProcessIndex]));
                    }
                    Array.Clear(input, 0, input.Length);
                    for (int i = 0; i < inputIndex; i++)
                    {
                        await this.channelKeeper.transferPackageToSend(finishArray[i]);
                    }
                    Array.Clear(finishArray, 0, finishArray.Length);
                    await this.inFlightCountGate.WaitAsync(this.shutDownTrigger.Token);
                    if ((this.encodeFinishedTcs != null) && (!this.encodeFinishedTcs.Task.IsCompleted))
                    {
                        this.inFlightCount -= inputIndex;
                        if (this.inFlightCount == 0)
                        {
                            this.encodeFinishedTcs.SetResult(true);

                        }
                    }
                    this.inFlightCountGate.Release();
                    inputIndex = 0;
                }

            }
            catch (OperationCanceledException)
            {

            }
            catch (ObjectDisposedException)
            {

            }
        }
    }
}

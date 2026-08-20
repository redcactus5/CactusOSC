using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace CactusOSC
{
    internal class OSCTCPReceiveServer : IDisposable
    {
        private uint MaxMessageSize;
        private bool UnboundedMessageSize;
        private int CurrentMessageSize;
        private ChannelManager ChannelMan;
        private Channel<byte[]> MessageQueue;
        private CancellationTokenSource ShutdownTrigger;
        private Task ReceiveTask;
        private byte[] SizeBuffer;
        private byte[] MessageBuffer;
        private NetworkStream Connection;
        private OSCTCPConnectionManager ConnectionManager;
        private bool ThrowOnOversizedMessage;
        private ErrorAndShutdownCarrier Carrier;
        private CancellationTokenSource linkedTrigger;

        public OSCTCPReceiveServer(OSCTCPConnectionManager ConnectionManager, ChannelManager ChannelMan,bool UnboundedMessageSize, uint MaxMessageSize, bool throwOnOversizedMessage)
        {
            this.MaxMessageSize = MaxMessageSize;
            this.ChannelMan = ChannelMan;
            this.SizeBuffer = new byte[4];
            this.ConnectionManager = ConnectionManager;
            this.Connection=this.ConnectionManager.getStream();
            this.MessageQueue = this.ChannelMan.getReceivedPackagesChannel();
            this.UnboundedMessageSize = UnboundedMessageSize;
            this.ThrowOnOversizedMessage = throwOnOversizedMessage;
        }

        public void Start(ErrorAndShutdownCarrier carrier)
        {
            this.Carrier = carrier;

            ShutdownTrigger = new CancellationTokenSource();
            this.linkedTrigger = CancellationTokenSource.CreateLinkedTokenSource(ShutdownTrigger.Token, carrier.getTokenSource().Token);
            this.ReceiveTask = ReceiveOSC();
        }

        public async Task ReceiveOSC()
        {
            ChannelWriter<byte[]> writer = this.MessageQueue.Writer;
            while (!this.linkedTrigger.IsCancellationRequested)
            {
                

                try
                {
                    await this.Connection.ReadExactlyAsync(this.SizeBuffer, this.linkedTrigger.Token);
                    this.CurrentMessageSize = BinaryPrimitives.ReadInt32BigEndian(this.SizeBuffer);
                    if(this.CurrentMessageSize < 0)
                    {
                        try
                        {
                            throw new OSCInvalidMessageSizeException();
                        }catch(OSCInvalidMessageSizeException e){
                            Carrier.setException(e);
                        }
                        
                    }
                    if (!this.UnboundedMessageSize)
                    {
                        if (this.CurrentMessageSize > ((int)this.MaxMessageSize))
                        {
                            if (this.ThrowOnOversizedMessage)
                            {
                                try
                                {
                                    throw new OSCInvalidMessageSizeException();
                                }
                                catch (OSCInvalidMessageSizeException e)
                                {
                                    Carrier.setException(e);
                                }
                            }
                            else
                            {
                                int count = CurrentMessageSize;

                                byte[] dummyArray = new byte[1024];
                                byte[] finalArray;
                                while (count > 0)
                                {
                                    if (count < 1024)
                                    {
                                        finalArray = new byte[count];
                                        await this.Connection.ReadExactlyAsync(finalArray, this.ShutdownTrigger.Token);
                                        count -= count;
                                    }
                                    else
                                    {
                                        await this.Connection.ReadExactlyAsync(dummyArray, this.ShutdownTrigger.Token);
                                        count -= 1024;
                                        
                                    }

                                }
                            }
                            
                        }
                        else
                        {
                            this.MessageBuffer = new byte[this.CurrentMessageSize];
                            await this.Connection.ReadExactlyAsync(MessageBuffer, this.ShutdownTrigger.Token);
                            await writer.WriteAsync(MessageBuffer, this.ShutdownTrigger.Token);
                        }
                    }
                    else
                    {
                        this.MessageBuffer = new byte[this.CurrentMessageSize];
                        await this.Connection.ReadExactlyAsync(MessageBuffer, this.ShutdownTrigger.Token);
                        await writer.WriteAsync(MessageBuffer, this.ShutdownTrigger.Token);
                    }
                    
                }
                catch (ObjectDisposedException)
                {
                    //socket closed during shutdown
                    break;
                }
                catch (SocketException)
                {
                    //socket closed during shutdown
                    break;
                }
                catch (OperationCanceledException)
                {
                    //user called shutdown
                    break;
                }
                catch (EndOfStreamException e)
                {
                    if (!ShutdownTrigger.IsCancellationRequested)
                    {

                        Carrier.setException(e);
                    }
                    else
                    {
                        break;
                    }
                    
                }
                
            }
        }


        public void Dispose()
        {
            this.Shutdown();
        }

        public void Shutdown()
        {
            if (this.ShutdownTrigger != null)
            {
                if (!this.ShutdownTrigger.IsCancellationRequested)
                {
                    this.ShutdownTrigger.Cancel();
                }
                
            }

            if (this.ReceiveTask != null)
            {
                this.ReceiveTask.GetAwaiter().GetResult();
                this.ReceiveTask = null;
            }
            
            if(this.ShutdownTrigger != null)
            {
                this.ShutdownTrigger.Dispose();
                this.ShutdownTrigger = null;
            }
            if (linkedTrigger != null)
            {
                if (!linkedTrigger.IsCancellationRequested)
                {
                    linkedTrigger.Cancel();

                }
                linkedTrigger.Dispose();
                linkedTrigger = null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace CactusOSC
{
    internal class OSCTCPSendServer : IDisposable
    {
        
        private Task SendTask;
        private OSCTCPConnectionManager ConnectionManager;
        private ChannelManager ChannelMan;
        private Channel<byte[]> SendQueue;
        private TaskCompletionSource<bool> SendFinished;
        private CancellationTokenSource ShutdownTrigger;
        private NetworkStream Connection;
        private ErrorAndShutdownCarrier carrier;
        private CancellationTokenSource linkedToken;

        public OSCTCPSendServer(ChannelManager channelMan,OSCTCPConnectionManager connectionManager)
        {
            this.ConnectionManager = connectionManager;
            this.ChannelMan = channelMan;
            this.Connection=connectionManager.getStream();
            this.SendQueue = this.ChannelMan.getPackagesToSendChannel();
        }

        public void Start(ErrorAndShutdownCarrier carrier)
        {

            this.ShutdownTrigger = new CancellationTokenSource();
            this.linkedToken = CancellationTokenSource.CreateLinkedTokenSource(this.ShutdownTrigger.Token, carrier.getTokenSource().Token);
            this.carrier = carrier;

            this.SendFinished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.SendTask = SendOSC();
        }

        public async Task SendOSC()
        {
            byte[] messageCache;

            while (!this.ShutdownTrigger.IsCancellationRequested)
            {
                try
                {

                    while (this.SendQueue.Reader.TryRead(out messageCache))
                    {
                        if (messageCache != null)
                        {
                            await Connection.WriteAsync(messageCache,this.linkedToken.Token);
                        }

                    }
                    SendFinished.TrySetResult(true);

                    await SendQueue.Reader.WaitToReadAsync(linkedToken.Token);

                    SendFinished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                }
                catch (ChannelClosedException)
                {
                    break;
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
                    //user triggered shutdown
                    break;
                }
                

            }
        }

        public void waitForSendFinish()
        {

            this.AsyncWaitForSendFinish().GetAwaiter().GetResult();

        }

        public async Task AsyncWaitForSendFinish()
        {
            await SendFinished.Task.WaitAsync(ShutdownTrigger.Token);
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
            byte[] garbageDisposal;
            while (SendQueue.Reader.TryRead(out garbageDisposal))
            {

            }
            if (this.SendTask != null)
            {
                this.SendTask.GetAwaiter().GetResult();
                this.SendTask = null;
            }
            if (this.ShutdownTrigger != null)
            {
                this.ShutdownTrigger.Dispose();
                this.ShutdownTrigger = null;
            }
            if (linkedToken != null)
            {
                if (!linkedToken.IsCancellationRequested)
                {
                    linkedToken.Cancel();

                }
                linkedToken.Dispose();
                linkedToken = null;
            }
        }
    }
}

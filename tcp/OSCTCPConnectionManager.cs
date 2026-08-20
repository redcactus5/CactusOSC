using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;



namespace CactusOSC
{
    internal class OSCTCPConnectionManager
    {
        private CancellationTokenSource ShutdownTrigger;
        private CancellationTokenSource endTimerEarly;
        private CancellationTokenSource linkedTokens;
        private TcpListener Listener;
        private TcpClient Connection;
        private NetworkStream AccessPort;
        private Task ConnectionTask;
        private Task ConnectionTimerTask;
        
        

        

        public OSCTCPConnectionManager()
        {
            
            
        }

        
        
        
        
        public async Task AcceptConnection(ushort ListenPort,bool specificAddress, IPAddress address, bool ShouldTimeout, uint Timeout)
        {
            try
            {
                ShutdownTrigger = new CancellationTokenSource();
                if (specificAddress)
                {
                    if(address == null)
                    {
                        this.Listener = new TcpListener(IPAddress.Any, ListenPort);
                    }
                    else
                    {
                        this.Listener = new TcpListener(address, ListenPort);
                    }
                    
                }
                else
                {
                    this.Listener = new TcpListener(IPAddress.Any, ListenPort);
                }
                if (ShouldTimeout)
                {
                    this.endTimerEarly = new CancellationTokenSource();
                    this.Listener.Start();
                    this.linkedTokens = CancellationTokenSource.CreateLinkedTokenSource(ShutdownTrigger.Token, endTimerEarly.Token);
                    this.ConnectionTimerTask = Task.Delay((int)Timeout, linkedTokens.Token);

                    ValueTask<TcpClient> clientTask = Listener.AcceptTcpClientAsync(ShutdownTrigger.Token);
                    this.ConnectionTask = clientTask.AsTask();
                    Task firstToFinish = await Task.WhenAny(this.ConnectionTimerTask, this.ConnectionTask);
                    if (firstToFinish == ConnectionTimerTask)
                    {
                        this.Shutdown();
                        throw new OSCServerConnectionTimeoutException();
                    }
                    else
                    {
                        this.Connection = clientTask.Result;
                        try
                        {
                            endTimerEarly.Cancel();
                            await ConnectionTimerTask;
                        }
                        catch (OperationCanceledException)
                        {

                        }
                        this.ConnectionTimerTask = null;

                    }

                }
                else
                {
                    this.Listener.Start();
                    ValueTask<TcpClient> clientTask = Listener.AcceptTcpClientAsync(ShutdownTrigger.Token);
                    this.ConnectionTask = clientTask.AsTask();
                    await ConnectionTask;
                    this.Connection = clientTask.Result;

                }

                this.Listener.Stop();
                this.AccessPort = this.Connection.GetStream();
                this.ConnectionTask = null;
            }
            catch (OperationCanceledException)
            {

            }
            
            

        }

        public async Task InitiateConnection(IPAddress Address, ushort Port, bool ShouldTimeout, uint Timeout) 
        {
            try
            {
                this.ShutdownTrigger = new CancellationTokenSource();
                this.Connection = new TcpClient();
                if (ShouldTimeout)
                {
                    this.endTimerEarly = new CancellationTokenSource();
                    this.linkedTokens = CancellationTokenSource.CreateLinkedTokenSource(ShutdownTrigger.Token, endTimerEarly.Token);
                    this.ConnectionTimerTask = Task.Delay((int)Timeout, linkedTokens.Token);
                    this.ConnectionTask = Connection.ConnectAsync(Address, Port);
                    Task firstToFinish = await Task.WhenAny(this.ConnectionTimerTask, this.ConnectionTask);
                    if (firstToFinish == ConnectionTimerTask)
                    {
                        this.Shutdown();
                        throw new OSCServerConnectionTimeoutException();
                    }
                    else
                    {
                        try 
                        {
                            endTimerEarly.Cancel();
                            await ConnectionTimerTask;
                        } catch (OperationCanceledException) 
                        { 

                        }
                        
                        ConnectionTimerTask = null;

                    }
                }
                else
                {
                    this.ConnectionTask = Connection.ConnectAsync(Address, Port);
                    await this.ConnectionTask;
                }
                this.AccessPort = this.Connection.GetStream();
                this.ConnectionTask = null;
            }
            catch (OperationCanceledException)
            {

            }
            
        }
        
        
        
        public void Dispose()
        {
            this.Shutdown();
        }

        public NetworkStream getStream()
        {
            return this.AccessPort;
        }
        public void Shutdown()
        {
            if(this.ShutdownTrigger != null)
            {
                if (!this.ShutdownTrigger.IsCancellationRequested)
                {
                    this.ShutdownTrigger.Cancel();
                }
               
            }
            if (this.endTimerEarly != null)
            {
                if (!this.endTimerEarly.IsCancellationRequested)
                {
                    this.endTimerEarly.Cancel();
                }
                
            }
            if (this.linkedTokens != null)
            {
                if (!this.linkedTokens.IsCancellationRequested)
                {
                    this.linkedTokens.Cancel();
                }
                this.linkedTokens.Dispose();
                this.linkedTokens = null;
            }
            if(this.Listener != null)
            {
                this.Listener.Stop();
                this.Listener.Dispose();
                this.Listener = null;
            }
            if (this.AccessPort != null)
            {
            
                this.AccessPort.Close();
                this.AccessPort.Dispose();
                this.AccessPort = null;
            }
            if (this.Connection != null)
            {
                if (this.Connection.Connected)
                {
                    this.Connection.Close();
                }
                this.Connection.Dispose();
                this.Connection = null;
            }
            if (this.ConnectionTask != null)
            {
                try
                {
                    this.ConnectionTask.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {

                }
                
                this.ConnectionTask = null;
            }
            if(this.ConnectionTimerTask != null)
            {
                try { 
                    this.ConnectionTimerTask.GetAwaiter().GetResult(); 
                } catch (OperationCanceledException) {
                    
                }
                
                this.ConnectionTimerTask = null;
            }

            if(this.ShutdownTrigger != null)
            {
                this.ShutdownTrigger.Dispose();
                this.ShutdownTrigger = null;
            }

            if(this.endTimerEarly != null)
            {
                this.endTimerEarly.Dispose();
                this.endTimerEarly = null;
            }
        }

    }
}

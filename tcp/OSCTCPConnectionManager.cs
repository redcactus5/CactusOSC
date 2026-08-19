using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Security.AccessControl;


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
        private ValueTask<TcpClient> ConnectionTask;
        private Task ConnectionTimerTask;
        
        

        

        public OSCTCPConnectionManager()
        {
            
            
        }

        
        
        
        
        public async Task AcceptConnection(ushort ListenPort,bool specificAddress, IPAddress address, bool ShouldTimeout, uint Timeout)
        {
            ShutdownTrigger = new CancellationTokenSource();
            if (specificAddress)
            {
                this.Listener = new TcpListener(address, ListenPort);
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
                this.ConnectionTimerTask = Task.Delay(Timeout,linkedTokens.Token);
                this.ConnectionTask=Listener.AcceptTcpClientAsync(ShutdownTrigger.Token).AsTask();
                Task firstToFinish = await Task.WhenAny(this.ConnectionTimerTask, this.ConnectionTask);
                if (firstToFinish == ConnectionTimerTask)
                {
                    this.ShutDown();
                    throw new OSCServerConnectionTimeoutException();
                }
                else
                {
                    this.Connection = ConnectionTask.Result;
                    endTimerEarly.Cancel();
                    await this.ConnectionTimerTask;
                    this.ConnectionTimerTask = null;
                    
                }
                
            }
            else
            {
                this.Listener.Start();
                this.ConnectionTask = Listener.AcceptTcpClientAsync(ShutdownTrigger.Token);
                await ConnectionTask;
                this.Connection = ConnectionTask.Result;
                
            }

            this.Listener.Stop();
            this.AccessPort = this.Connection.GetStream();
            this.ConnectionTask = null;
            

        }

        public async Task InitiateConnection(IPAddress Address, ushort Port, bool ShouldTimeout, uint Timeout) 
        {
            
            this.ShutdownTrigger=new CancellationTokenSource();
            this.Connection=new TcpClient();
            if (ShouldTimeout)
            {
                this.endTimerEarly=new CancellationTokenSource();
                this.linkedTokens = CancellationTokenSource.CreateLinkedTokenSource(ShutdownTrigger.Token, endTimerEarly.Token);
                this.ConnectionTimerTask = Task.Delay(timeout, linkedTokens.Token);
                this.ConnectionTask = Connection.ConnectAsync(Address, Port);
                Task firstToFinish = await Task.WhenAny(this.ConnectionTimerTask, this.ConnectionTask);
                if (firstToFinish == ConnectionTimerTask)
                {
                    this.ShutDown();
                    throw new OSCServerConnectionTimeoutException();
                }
                else
                {
                    
                    endTimerEarly.Cancel();
                    await ConnectionTimerTask;
                    ConnectionTimerTask = null;

                }
            }
            else
            {
                this.ConnectionTask = Connection.ConnectAsync(address, port);
                await this.ConnectionTask;
            }
            this.AccessPort=this.Connection.GetStream();
            this.ConnectionTask = null;
        }
        
        
        
        public void Dispose()
        {
            this.ShutDown();
        }

        public NetworkStream getStream()
        {
            return this.AccessPort;
        }
        public void ShutDown()
        {
            if(this.ShutdownTrigger != null)
            {
                if (!this.ShutdownTrigger.IsCancellationRequested)
                {
                    this.ShutdownTrigger.Cancel();
                }
                this.ShutdownTrigger.Dispose();
                this.ShutdownTrigger = null;
            }
            if (this.endTimerEarly != null)
            {
                if (!this.endTimerEarly.IsCancellationRequested)
                {
                    this.endTimerEarly.Cancel();
                }
                this.endTimerEarly.Dispose();
                this.endTimerEarly = null;
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
        }

    }
}

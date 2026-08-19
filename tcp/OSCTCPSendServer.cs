using System;
using System.Collections.Generic;
using System.Text;

namespace CactusOSC
{
    internal class OSCTCPSendServer : IDisposable
    {
        private Task ReceiveTask;
        private Task SendTask;
        private OSCTCPConnectionManager ConnectionManager;
    }
}

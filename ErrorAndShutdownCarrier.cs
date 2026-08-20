using System;
using System.Collections.Generic;
using System.Text;

namespace CactusOSC
{
    internal class ErrorAndShutdownCarrier
    {
        private Exception error;
        private CancellationTokenSource tokenSource;
        public ErrorAndShutdownCarrier(CancellationTokenSource tokenSource) {
            this.tokenSource = tokenSource;
        }

        public CancellationTokenSource getTokenSource()
        {
            return tokenSource;
        }
        public Exception getException()
        {
            return Volatile.Read(ref error);
        }

        public void setException(Exception exception)
        {
            if (Volatile.Read(ref error) != null)
            {
                Volatile.Write(ref error, exception);
                tokenSource.Cancel();
            }
            
        }
    }
}

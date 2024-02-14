using System;
using Orangebeard.Client.V3;
using Orangebeard.Client.V3.Entity.TestRun;

namespace Orangebeard.ReqnrollPlugin.EventArguments
{
    public class RunStartedEventArgs : EventArgs
    {
        public RunStartedEventArgs(OrangebeardAsyncV3Client client, StartTestRun request)
        {
            Client = client;
            StartTestRunRequest = request;
        }
        
        public OrangebeardAsyncV3Client Client { get; }

        public StartTestRun StartTestRunRequest { get; }

        public bool Canceled { get; set; }
    }
}

using System;
using Orangebeard.Client.V3;
using Orangebeard.Client.V3.Entity.TestRun;

namespace Orangebeard.ReqnrollPlugin.EventArguments
{
    public class RunFinishedEventArgs : EventArgs
    {
        public RunFinishedEventArgs(OrangebeardAsyncV3Client client, FinishTestRun request)
        {
            Client = client;
            FinishTestRunRequest = request;
        }

        public OrangebeardAsyncV3Client Client { get; }

        public FinishTestRun FinishTestRunRequest { get; }


        public bool Canceled { get; set; }
    }
}

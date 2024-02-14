using System;
using Orangebeard.Client.V3;
using Orangebeard.Client.V3.Entity.Test;

namespace Orangebeard.ReqnrollPlugin.EventArguments
{
    public class TestStartedEventArgs : EventArgs
    {
        public TestStartedEventArgs(OrangebeardAsyncV3Client client, StartTest request)
        {
            Client = client;
            StartTestRequest = request;
        }

        public OrangebeardAsyncV3Client Client { get; }
        public StartTest StartTestRequest { get; }
        public bool Canceled { get; set;}
    }
}

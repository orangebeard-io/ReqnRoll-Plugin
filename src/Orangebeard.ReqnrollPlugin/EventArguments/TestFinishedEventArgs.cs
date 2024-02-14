using System;
using Orangebeard.Client.V3;
using Orangebeard.Client.V3.Entity.Test;

namespace Orangebeard.ReqnrollPlugin.EventArguments
{
    public class TestFinishedEventArgs: EventArgs
    {
        public TestFinishedEventArgs(Guid testGuid, OrangebeardAsyncV3Client client, FinishTest request)
        {
            TestGuid = testGuid;
            Client = client;
            FinishTestRequest = request;
        }

        public Guid TestGuid { get; }
        public OrangebeardAsyncV3Client Client { get; }
        public FinishTest FinishTestRequest { get; }
        public bool Canceled { get; set; }
    }
}

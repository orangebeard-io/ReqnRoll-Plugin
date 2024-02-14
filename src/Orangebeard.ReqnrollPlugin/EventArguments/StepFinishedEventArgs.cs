using System;
using Orangebeard.Client.V3;
using Orangebeard.Client.V3.Entity.Step;

namespace Orangebeard.ReqnrollPlugin.EventArguments
{
    public class StepFinishedEventArgs : EventArgs
    {
        public StepFinishedEventArgs(Guid stepGuid, OrangebeardAsyncV3Client client, FinishStep request)
        {
            StepGuid = stepGuid;
            Client = client;
            FinishStepRequest = request;
        }

        public Guid StepGuid { get; }
        public OrangebeardAsyncV3Client Client { get; }
        public FinishStep FinishStepRequest { get; }
        public bool Canceled { get; set; }
    }
}

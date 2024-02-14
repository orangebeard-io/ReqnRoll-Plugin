using System;
using Orangebeard.Client.V3;
using Orangebeard.Client.V3.Entity.Step;

namespace Orangebeard.ReqnrollPlugin.EventArguments
{
    public class StepStartedEventArgs : EventArgs
    {
        public StepStartedEventArgs(OrangebeardAsyncV3Client client, StartStep request)
        {
            Client = client;
            StartStepRequest = request;
        }
        
        public OrangebeardAsyncV3Client Client { get; }
        public StartStep StartStepRequest { get; }
        public bool Canceled { get; set;}
    }
}

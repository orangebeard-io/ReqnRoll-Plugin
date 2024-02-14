using Orangebeard.Client.V3;
using Orangebeard.Client.V3.Entity.Suite;

namespace Orangebeard.ReqnrollPlugin.EventArguments
{
    public class SuiteStartedEventArgs
    {
        public SuiteStartedEventArgs(OrangebeardAsyncV3Client client, StartSuite request)
        {
            Client = client;
            StartSuiteRequest = request;
        }

        public OrangebeardAsyncV3Client Client { get; }
        public StartSuite StartSuiteRequest { get; }
        public bool Canceled { get; set;}
    }
}
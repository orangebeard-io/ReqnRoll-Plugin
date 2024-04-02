using System.Threading;
using Reqnroll;

namespace Orangebeard.ReqnrollPlugin.LogHandler
{
    public static class ContextHandler
    {
        public static ScenarioStepContext ActiveStepContext { get; set; }

        public static ScenarioContext ActiveScenarioContext { get; set; }
        
        public static FeatureContext ActiveFeatureContext { get; set; }
    }
}
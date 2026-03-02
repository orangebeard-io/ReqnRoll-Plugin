using System.Threading;
using Reqnroll;

namespace Orangebeard.ReqnrollPlugin.LogHandler
{
    public static class ContextHandler
    {
        private static readonly AsyncLocal<ScenarioStepContext> s_activeStepContext = new AsyncLocal<ScenarioStepContext>();
        public static ScenarioStepContext ActiveStepContext
        {
            get => s_activeStepContext.Value;
            set => s_activeStepContext.Value = value;
        }

        private static readonly AsyncLocal<ScenarioContext> s_activeScenarioContext = new AsyncLocal<ScenarioContext>();
        public static ScenarioContext ActiveScenarioContext
        {
            get => s_activeScenarioContext.Value;
            set => s_activeScenarioContext.Value = value;
        }

        private static readonly AsyncLocal<FeatureContext> s_activeFeatureContext = new AsyncLocal<FeatureContext>();
        public static FeatureContext ActiveFeatureContext
        {
            get => s_activeFeatureContext.Value;
            set => s_activeFeatureContext.Value = value;
        }
    }
}
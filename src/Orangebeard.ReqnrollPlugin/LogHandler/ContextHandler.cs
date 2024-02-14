using System.Threading;
using Orangebeard.Client.V3.ClientUtils.Logging;
using Reqnroll;

namespace Orangebeard.ReqnrollPlugin.LogHandler
{
    public class ContextHandler
    {
        private readonly ILogger _logger = LogManager.Instance.GetLogger<ContextHandler>();

        private static readonly AsyncLocal<ScenarioStepContext> _activeStepContext = new AsyncLocal<ScenarioStepContext>();

        public static ScenarioStepContext ActiveStepContext
        {
            get => _activeStepContext.Value;
            set => _activeStepContext.Value = value;
        }

        private static readonly AsyncLocal<ScenarioContext> _activeScenarioContext = new AsyncLocal<ScenarioContext>();

        public static ScenarioContext ActiveScenarioContext
        {
            get => _activeScenarioContext.Value;
            set => _activeScenarioContext.Value = value;
        }

        private static readonly AsyncLocal<FeatureContext> _activeFeatureContext = new AsyncLocal<FeatureContext>();

        public static FeatureContext ActiveFeatureContext
        {
            get => _activeFeatureContext.Value;
            set => _activeFeatureContext.Value = value;
        }
        
    }
}

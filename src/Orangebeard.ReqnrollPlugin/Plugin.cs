using System;
using System.IO;
using Orangebeard.Client.V3.ClientUtils.Logging;
using Orangebeard.Client.V3.OrangebeardConfig;
using Orangebeard.ReqnrollPlugin;
using Reqnroll;
using Reqnroll.Bindings;
using Reqnroll.Infrastructure;
using Reqnroll.Plugins;
using Reqnroll.UnitTestProvider;

[assembly: RuntimePlugin(typeof(Plugin))]
namespace Orangebeard.ReqnrollPlugin
{
    /// <summary>
    /// Registered SpecFlow plugin from configuration file.
    /// </summary>
    internal class Plugin : IRuntimePlugin
    {
        private ILogger _logger;

        public static IConfiguration Config { get; set; }

        public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters, UnitTestProviderConfiguration unitTestProviderConfiguration)
        {
            var currentDirectory = Path.GetDirectoryName(new Uri(typeof(Plugin).Assembly.CodeBase).LocalPath);

            _logger = LogManager.Instance.WithBaseDir(currentDirectory).GetLogger<Plugin>();

            Config = new ConfigurationBuilder().AddDefaults(currentDirectory).Build();

            var isEnabled = Config.GetValue("Enabled", true);

            if (!isEnabled) return;
            
            runtimePluginEvents.CustomizeGlobalDependencies += (sender, e) =>
            {
                e.ReqnrollConfiguration.AdditionalStepAssemblies.Add("Orangebeard.ReqnrollPlugin");
                e.ObjectContainer.RegisterTypeAs<SafeBindingInvoker, IAsyncBindingInvoker>();
                e.ObjectContainer.RegisterTypeAs<OrangebeardOutputHelper, IReqnrollOutputHelper>();
            };

            runtimePluginEvents.CustomizeScenarioDependencies += (sender, e) =>
            {
                e.ObjectContainer.RegisterTypeAs<SkippedStepsHandler, ISkippedStepHandler>();
                e.ObjectContainer.RegisterTypeAs<OrangebeardOutputHelper, IReqnrollOutputHelper>();
            };
        }
    }
}

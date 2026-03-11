﻿using System.IO;
using System.Reflection;
using Orangebeard.Client.V3.ClientUtils.Logging;
using Orangebeard.Client.V3.OrangebeardConfig;
using Orangebeard.ReqnrollPlugin;
using Reqnroll;
using Reqnroll.Bindings;
using Reqnroll.Configuration;
using Reqnroll.ErrorHandling;
using Reqnroll.EnvironmentAccess;
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

        public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters, UnitTestProviderConfiguration unitTestProviderConfiguration)
        {
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            _logger = LogManager.Instance.WithBaseDir(currentDirectory).GetLogger<Plugin>();

            var config = new ConfigurationBuilder().AddDefaults(currentDirectory).Build();

            var isEnabled = config.GetValue("Enabled", true);

            if (!isEnabled) return;
            
            runtimePluginEvents.CustomizeGlobalDependencies += (sender, e) =>
            {
                // Register the configuration as a singleton instance for the test run.
                e.ObjectContainer.RegisterInstanceAs<IConfiguration>(config);

                e.ReqnrollConfiguration.AdditionalStepAssemblies.Add("Orangebeard.ReqnrollPlugin");
                e.ObjectContainer.RegisterFactoryAs<IAsyncBindingInvoker>(c => 
                    new SafeBindingInvoker(
                        c.Resolve<ReqnrollConfiguration>(),
                        c.Resolve<IErrorProvider>(),
                        c.Resolve<IBindingDelegateInvoker>(),
                        c.Resolve<IEnvironmentOptions>()
                    ));
            };

            runtimePluginEvents.CustomizeScenarioDependencies += (sender, e) =>
            {
                e.ObjectContainer.RegisterTypeAs<SkippedStepsHandler, ISkippedStepHandler>();
                e.ObjectContainer.RegisterTypeAs<OrangebeardOutputHelper, IReqnrollOutputHelper>();
            };
        }
    }
}

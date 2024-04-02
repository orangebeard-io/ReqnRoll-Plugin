using Orangebeard.Client.Abstractions.Models;
using Orangebeard.Client.Abstractions.Responses;
using Orangebeard.Shared;
using Orangebeard.ReqnrollPlugin;
using Orangebeard.ReqnrollPlugin.EventArguments;
using System;
using System.IO;
using System.Reflection;
using Reqnroll;

namespace Example.ReqnRoll.Hooks
{
    [Binding]
    public sealed class HooksExample
    {
        // BeforeTestRun hook order should be set to the value that is lower than -20000
        // if you plan to use BeforeRunStarted event.
        [BeforeTestRun(Order = -30000)]
        public static void BeforeTestRunPart()
        {
            OrangebeardAddIn.BeforeRunStarted += OrangebeardAddIn_BeforeRunStarted;
            OrangebeardAddIn.BeforeFeatureStarted += OrangebeardAddIn_BeforeFeatureStarted;
            OrangebeardAddIn.BeforeScenarioStarted += OrangebeardAddIn_BeforeScenarioStarted;
            OrangebeardAddIn.BeforeScenarioFinished += OrangebeardAddIn_BeforeScenarioFinished;

            OrangebeardAddIn.AfterFeatureFinished += OrangebeardAddIn_AfterFeatureFinished;
        }

        private static void OrangebeardAddIn_BeforeRunStarted(object sender, RunStartedEventArgs e)
        {
            e.StartTestRunRequest.Description = $"OS: {Environment.OSVersion.VersionString}";
        }
        
        private static void OrangebeardAddIn_BeforeFeatureStarted(object sender, SuiteStartedEventArgs e)
        {
            // Adding feature tag on runtime
            e.StartSuiteRequest.Attributes.Add(new ItemAttribute { Value = "runtime_feature_tag" });
        }

        private static void OrangebeardAddIn_BeforeScenarioStarted(object sender, TestStartedEventArgs e)
        {
            // Adding scenario tag on runtime
            e.StartTestRequest.Attributes.Add(new ItemAttribute { Value = "runtime_scenario_tag" });
        }
        
    }
}

using Orangebeard.Client.Abstractions.Models;
using Orangebeard.Client.Abstractions.Responses;
using Orangebeard.Shared;
using Orangebeard.SpecFlowPlugin;
using Orangebeard.SpecFlowPlugin.EventArguments;
using System;
using System.IO;
using System.Reflection;
using TechTalk.SpecFlow;

namespace Example.SpecFlow.Hooks
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
            e.StartLaunchRequest.Description = $"OS: {Environment.OSVersion.VersionString}";
        }

        private static void OrangebeardAddIn_BeforeScenarioFinished(object sender, TestItemFinishedEventArgs e)
        {
            if (e.ScenarioContext.TestError != null && e.ScenarioContext.ScenarioInfo.Title == "System Error")
            {
                e.FinishTestItemRequest.Issue = new Issue
                {
                    Type = DefaultIssueType.SystemIssue,
                    Comment = "my custom system error comment"
                };
            }
            // Add message to defect comment
            else if (e.ScenarioContext.TestError != null)
            {
                e.FinishTestItemRequest.Issue = new Issue
                {
                    Type = DefaultIssueType.ToInvestigate,
                    Comment = e.ScenarioContext.TestError.Message
                };
            }
        }

        private static void OrangebeardAddIn_BeforeFeatureStarted(object sender, TestItemStartedEventArgs e)
        {
            // Adding feature tag on runtime
            e.StartTestItemRequest.Attributes.Add(new ItemAttribute { Value = "runtime_feature_tag" });
        }

        private static void OrangebeardAddIn_BeforeScenarioStarted(object sender, TestItemStartedEventArgs e)
        {
            // Adding scenario tag on runtime
            e.StartTestItemRequest.Attributes.Add(new ItemAttribute { Value = "runtime_scenario_tag" });
        }

        [AfterScenario]
        public void AfterScenario(ScenarioContext context)
        {
            if (context.ScenarioExecutionStatus == ScenarioExecutionStatus.TestError)
            {
                var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\cat.png";
                Context.Current.Log.Debug("This cat came from AfterScenario hook {rp#file#" + filePath + "}");
            }
        }

        private static void OrangebeardAddIn_AfterFeatureFinished(object sender, TestItemFinishedEventArgs e)
        {
#if NETCOREAPP
            // Workaround how to avoid issue https://github.com/techtalk/SpecFlow/issues/1348 (launch doesn't finish on .netcore tests)
            e.TestReporter.FinishTask.Wait();
#endif
        }
    }
}

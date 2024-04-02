using System;
using System.Collections.Generic;
using Orangebeard.Client.V3.ClientUtils.Logging;
using Orangebeard.ReqnrollPlugin.EventArguments;
using Orangebeard.ReqnrollPlugin.LogHandler;
using Reqnroll;

namespace Orangebeard.ReqnrollPlugin
{
    public class OrangebeardAddIn
    {
        private static readonly ILogger Logger = LogManager.Instance.GetLogger<OrangebeardAddIn>();

        private static Dictionary<FeatureInfo, Guid> Suites { get; } =
            new Dictionary<FeatureInfo, Guid>(new FeatureInfoEqualityComparer());

        private static Dictionary<FeatureInfo, int> SuiteThreadCount { get; } =
            new Dictionary<FeatureInfo, int>(new FeatureInfoEqualityComparer());

        private static Dictionary<ScenarioInfo, Guid> Tests { get; } =
            new Dictionary<ScenarioInfo, Guid>();

        private static Dictionary<StepInfo, Guid> Steps { get; } = new Dictionary<StepInfo, Guid>();

        // key: log scope ID, value: according test reporter
        public static Dictionary<string, Guid> LogScopes { get; } = new Dictionary<string, Guid>();

        public static Guid? GetCurrentFeatureGuid(FeatureContext context)
        {
            if (context != null && Suites.TryGetValue(context.FeatureInfo, out var guid))
            {
                return guid;
            }

            return null;
        }

        internal static void SetFeatureGuid(FeatureContext context, Guid guid)
        {
            Suites[context.FeatureInfo] = guid;
            SuiteThreadCount[context.FeatureInfo] = 1;
        }

        internal static void RemoveFeatureGuid(FeatureContext context)
        {
            Suites.Remove(context.FeatureInfo);
            SuiteThreadCount.Remove(context.FeatureInfo);
        }

        internal static int IncrementFeatureThreadCount(FeatureContext context)
        {
            return SuiteThreadCount[context.FeatureInfo]
                = SuiteThreadCount.TryGetValue(context.FeatureInfo, out var value) ? value + 1 : 1;
        }

        public static Guid GetScenarioGuid(ScenarioContext context)
        {
            if (context != null && Tests.TryGetValue(context.ScenarioInfo, out var scenarioGuid))
            {
                return scenarioGuid;
            }

            var msg = context == null
                ? "No ScenarioContext!"
                : "Test not found for Scenario: " + context.ScenarioInfo.Title;
            throw new InvalidContextException(msg);
        }

        internal static void SetScenarioGuid(ScenarioContext context, Guid guid)
        {
            Tests[context.ScenarioInfo] = guid;
        }

        internal static void RemoveScenarioGuid(ScenarioContext context)
        {
            Tests.Remove(context.ScenarioInfo);
        }

        public static Guid? GetStepGuid(ScenarioStepContext context)
        {
            if (context != null && Steps.TryGetValue(context.StepInfo, out var stepGuid))
            {
                return stepGuid;
            }

            return null;
        }

        internal static void SetStepGuid(ScenarioStepContext context, Guid guid)
        {
            Steps[context.StepInfo] = guid;
        }

        internal static void RemoveStepGuid(ScenarioStepContext context)
        {
            Steps.Remove(context.StepInfo);
        }

        public delegate void InitializingHandler(object sender, InitializingEventArgs e);

        public static event InitializingHandler Initializing;

        internal static void OnInitializing(object sender, InitializingEventArgs eventArg)
        {
            try
            {
                Initializing?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnInitializing)} event handler: {exp}");
            }
        }

        public delegate void RunStartedHandler(object sender, RunStartedEventArgs e);

        public static event RunStartedHandler BeforeRunStarted;
        public static event RunStartedHandler AfterRunStarted;

        internal static void OnBeforeRunStarted(object sender, RunStartedEventArgs eventArg)
        {
            try
            {
                BeforeRunStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeRunStarted)} event handler: {exp}");
            }
        }

        internal static void OnAfterRunStarted(object sender, RunStartedEventArgs eventArg)
        {
            try
            {
                AfterRunStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterRunStarted)} event handler: {exp}");
            }
        }

        public delegate void RunFinishedHandler(object sender, RunFinishedEventArgs e);

        public static event RunFinishedHandler BeforeRunFinished;
        public static event RunFinishedHandler AfterRunFinished;

        internal static void OnBeforeRunFinished(object sender, RunFinishedEventArgs eventArg)
        {
            try
            {
                BeforeRunFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeRunFinished)} event handler: {exp}");
            }
        }

        internal static void OnAfterRunFinished(object sender, RunFinishedEventArgs eventArg)
        {
            try
            {
                AfterRunFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterRunFinished)} event handler: {exp}");
            }
        }

        public delegate void FeatureStartedHandler(object sender, SuiteStartedEventArgs e);

        public static event FeatureStartedHandler BeforeFeatureStarted;
        public static event FeatureStartedHandler AfterFeatureStarted;

        internal static void OnBeforeFeatureStarted(object sender, SuiteStartedEventArgs eventArg)
        {
            try
            {
                BeforeFeatureStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeFeatureStarted)} event handler: {exp}");
            }
        }

        internal static void OnAfterFeatureStarted(object sender, SuiteStartedEventArgs eventArg)
        {
            try
            {
                AfterFeatureStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterFeatureStarted)} event handler: {exp}");
            }
        }

        public delegate void ScenarioStartedHandler(object sender, TestStartedEventArgs e);

        public static event ScenarioStartedHandler BeforeScenarioStarted;
        public static event ScenarioStartedHandler AfterScenarioStarted;

        internal static void OnBeforeScenarioStarted(object sender, TestStartedEventArgs eventArg)
        {
            try
            {
                BeforeScenarioStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeScenarioStarted)} event handler: {exp}");
            }
        }

        internal static void OnAfterScenarioStarted(object sender, TestStartedEventArgs eventArg)
        {
            try
            {
                AfterScenarioStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterScenarioStarted)} event handler: {exp}");
            }
        }

        public delegate void ScenarioFinishedHandler(object sender, TestFinishedEventArgs e);

        public static event ScenarioFinishedHandler BeforeScenarioFinished;
        public static event ScenarioFinishedHandler AfterScenarioFinished;

        internal static void OnBeforeScenarioFinished(object sender, TestFinishedEventArgs eventArg)
        {
            try
            {
                BeforeScenarioFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeScenarioFinished)} event handler: {exp}");
            }
        }

        internal static void OnAfterScenarioFinished(object sender, TestFinishedEventArgs eventArg)
        {
            try
            {
                AfterScenarioFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterScenarioFinished)} event handler: {exp}");
            }
        }

        public delegate void StepStartedHandler(object sender, StepStartedEventArgs e);

        public static event StepStartedHandler BeforeStepStarted;
        public static event StepStartedHandler AfterStepStarted;

        internal static void OnBeforeStepStarted(object sender, StepStartedEventArgs eventArg)
        {
            try
            {
                BeforeStepStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeStepStarted)} event handler: {exp}");
            }
        }

        internal static void OnAfterStepStarted(object sender, StepStartedEventArgs eventArg)
        {
            try
            {
                AfterStepStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterStepStarted)} event handler: {exp}");
            }
        }

        public delegate void StepFinishedHandler(object sender, StepFinishedEventArgs e);

        public static event StepFinishedHandler BeforeStepFinished;
        public static event StepFinishedHandler AfterStepFinished;

        internal static void OnBeforeStepFinished(object sender, StepFinishedEventArgs eventArg)
        {
            try
            {
                BeforeStepFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeStepFinished)} event handler: {exp}");
            }
        }

        internal static void OnAfterStepFinished(object sender, StepFinishedEventArgs eventArg)
        {
            try
            {
                AfterStepFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterStepFinished)} event handler: {exp}");
            }
        }

        public static (Guid testrun, Guid test, Guid? step) GetCurrentContext()
        {
            var testRun = OrangebeardHooks.GetTestRunGuid();
            var currentTest = GetScenarioGuid(ContextHandler.ActiveScenarioContext);
            var currentStep = GetStepGuid(ContextHandler.ActiveStepContext);

            return (testRun, test: currentTest, step: currentStep);
        }
    }
}
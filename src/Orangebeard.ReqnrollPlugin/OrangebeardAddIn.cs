using System;
using System.Collections.Concurrent;
using Orangebeard.Client.V3.ClientUtils.Logging;
using Orangebeard.ReqnrollPlugin.EventArguments;
using Orangebeard.ReqnrollPlugin.LogHandler;
using Reqnroll;

namespace Orangebeard.ReqnrollPlugin
{
    public class OrangebeardAddIn
    {
        private static readonly ILogger Logger = LogManager.Instance.GetLogger<OrangebeardAddIn>();

        private const string TestGuidKey = "OrangebeardTestGuid";
        private const string StepGuidKey = "OrangebeardStepGuid";

        private static ConcurrentDictionary<FeatureInfo, Guid> Suites { get; } =
            new ConcurrentDictionary<FeatureInfo, Guid>(new FeatureInfoEqualityComparer());

        private static ConcurrentDictionary<FeatureInfo, int> SuiteThreadCount { get; } =
            new ConcurrentDictionary<FeatureInfo, int>(new FeatureInfoEqualityComparer());

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
            Suites.AddOrUpdate(context.FeatureInfo, guid, (featureInfo, oldGuid) => guid);
            SuiteThreadCount.AddOrUpdate(context.FeatureInfo, 1, (featureInfo, oldCount) => 1);
        }

        internal static void RemoveFeatureGuid(FeatureContext context)
        {
            Suites.TryRemove(context.FeatureInfo, out _);
            SuiteThreadCount.TryRemove(context.FeatureInfo, out _);
        }

        internal static int IncrementFeatureThreadCount(FeatureContext context)
        {
            return SuiteThreadCount.AddOrUpdate(context.FeatureInfo, 1, (featureInfo, value) => value + 1);
        }

        internal static int DecrementFeatureThreadCount(FeatureContext context)
        {
            return SuiteThreadCount.AddOrUpdate(context.FeatureInfo, 0, (featureInfo, value) => value - 1);
        }

        public static Guid GetScenarioGuid(ScenarioContext context)
        {
            if (context != null && context.ContainsKey(TestGuidKey))
            {
                return (Guid)context[TestGuidKey];
            }

            var msg = context == null
                ? "No ScenarioContext!"
                : "Test not found for Scenario: " + context.ScenarioInfo.Title;
            throw new InvalidContextException(msg);
        }

        internal static void SetScenarioGuid(ScenarioContext context, Guid guid)
        {
            context[TestGuidKey] = guid;
        }

        internal static void RemoveScenarioGuid(ScenarioContext context)
        {
            context.Remove(TestGuidKey);
        }

        public static Guid? GetStepGuid(ScenarioContext context)
        {
            if (context != null && context.ContainsKey(StepGuidKey))
            {
                return (Guid)context[StepGuidKey];
            }

            return null;
        }

        internal static void SetStepGuid(ScenarioContext context, Guid guid)
        {
            context[StepGuidKey] = guid;
        }

        internal static void RemoveStepGuid(ScenarioContext context)
        {
            context.Remove(StepGuidKey);
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

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Orangebeard.Client.V3;
using Orangebeard.Client.V3.ClientUtils.Logging;
using Orangebeard.Client.V3.Entity;
using Orangebeard.Client.V3.Entity.Log;
using Orangebeard.Client.V3.Entity.Step;
using Orangebeard.Client.V3.Entity.Suite;
using Orangebeard.Client.V3.Entity.Test;
using Orangebeard.Client.V3.Entity.TestRun;
using Orangebeard.Client.V3.OrangebeardConfig;
using Orangebeard.ReqnrollPlugin.EventArguments;
using Orangebeard.ReqnrollPlugin.Extensions;
using Orangebeard.ReqnrollPlugin.Util;
using Reqnroll;
using Attribute = Orangebeard.Client.V3.Entity.Attribute;

namespace Orangebeard.ReqnrollPlugin
{
    [Binding]
    internal class OrangebeardHooks
    {
        private static readonly ILogger Logger = LogManager.Instance.GetLogger<OrangebeardHooks>();
        internal static readonly object _clientLock = new object();

        private static readonly HashSet<string> SkippedExceptionTypes = new HashSet<string>
        {
            "NUnit.Framework.IgnoreException",
            "NUnit.Framework.InconclusiveException",
            "Microsoft.VisualStudio.TestTools.UnitTesting.AssertInconclusiveException"
        };

        private static OrangebeardAsyncV3Client _client;
        private static Guid _testrunGuid;

        internal static OrangebeardAsyncV3Client GetClient()
        {
            return _client;
        }

        internal static Guid GetTestRunGuid()
        {
            return _testrunGuid;
        }

        [BeforeTestRun(Order = -20000)]
        public static void BeforeTestRun(IConfiguration config)
        {
            try
            {
                var args = new InitializingEventArgs(config);
                OrangebeardAddIn.OnInitializing(typeof(OrangebeardHooks), args);
                var effectiveConfig = args.Config;

                var orangebeardConfig = new OrangebeardConfiguration(effectiveConfig).WithListenerIdentification(
                    "Reqnroll Plugin/" +
                    typeof(OrangebeardHooks).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                        .InformationalVersion
                );
                _client = new OrangebeardAsyncV3Client(orangebeardConfig);

                var startTestRun = new StartTestRun()
                {
                    TestSetName = effectiveConfig.GetValue(ConfigurationPath.TestSetName, "Reqnroll Test Run"),
                    StartTime = DateTime.UtcNow,
                    Attributes = new HashSet<Attribute>(effectiveConfig.GetKeyValues("TestSet:Attributes", new Dictionary<string, string>()).Select(a => new Attribute { Key = a.Key, Value = a.Value })),
                    Description = effectiveConfig.GetValue(ConfigurationPath.TestSetDescription, string.Empty)
                };


                var eventArg = new RunStartedEventArgs(_client, startTestRun);
                OrangebeardAddIn.OnBeforeRunStarted(null, eventArg);

                if (eventArg.Canceled) return;
                _testrunGuid = _client.StartTestRun(startTestRun);
                OrangebeardAddIn.OnAfterRunStarted(null, new RunStartedEventArgs(_client, startTestRun));
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [AfterTestRun(Order = 20000)]
        public static void AfterTestRun()
        {
            try
            {
                var finishTestRun = new FinishTestRun();

                var eventArg = new RunFinishedEventArgs(_client, finishTestRun);
                OrangebeardAddIn.OnBeforeRunFinished(null, eventArg);

                if (eventArg.Canceled) return;

                _client.FinishTestRun(_testrunGuid, finishTestRun);
                OrangebeardAddIn.OnAfterRunFinished(null,
                    new RunFinishedEventArgs(_client, finishTestRun));
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [BeforeFeature(Order = -20000)]
        public static void BeforeFeature(FeatureContext featureContext)
        {
            try
            {
                lock (_clientLock)
                {
                    var currentFeature = OrangebeardAddIn.GetCurrentFeatureGuid(featureContext);

                    if (currentFeature != null)
                    {
                        OrangebeardAddIn.IncrementFeatureThreadCount(featureContext);
                        return;
                    }

                    var startSuite = new StartSuite()
                    {
                        TestRunUUID = _testrunGuid,
                        Description = featureContext.FeatureInfo.Description,
                        Attributes = new HashSet<Attribute>(featureContext.FeatureInfo.Tags?.Select(tag =>
                        {
                            var parts = tag.Split(new[] { ':' }, 2);
                            return parts.Length == 2
                                ? new Attribute { Key = parts[0], Value = parts[1] }
                                : new Attribute { Value = tag };
                        }) ?? Enumerable.Empty<Attribute>()),
                        SuiteNames = new[] { featureContext.FeatureInfo.Title },
                    };

                    var eventArg = new SuiteStartedEventArgs(_client, startSuite);
                    OrangebeardAddIn.OnBeforeFeatureStarted(null, eventArg);

                    if (eventArg.Canceled) return;

                    currentFeature = _client.StartSuite(startSuite)[0];
                    OrangebeardAddIn.SetFeatureGuid(featureContext, currentFeature.Value);

                    OrangebeardAddIn.OnAfterFeatureStarted(null,
                        new SuiteStartedEventArgs(_client, startSuite));
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [AfterFeature(Order = 20000)]
        public static void AfterFeature(FeatureContext featureContext)
        {
            try
            {
                lock (_clientLock)
                {
                    var remaining = OrangebeardAddIn.DecrementFeatureThreadCount(featureContext);
                    if (remaining <= 0)
                    {
                        OrangebeardAddIn.RemoveFeatureGuid(featureContext);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }


        [BeforeScenario(Order = -20000)]
        public static void BeforeScenario(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            try
            {
                var currentFeature = OrangebeardAddIn.GetCurrentFeatureGuid(featureContext);

                if (currentFeature == null) return;

                var startTest = new StartTest()
                {
                    TestRunUUID = _testrunGuid,
                    SuiteUUID = currentFeature.Value,
                    TestName = scenarioContext.ScenarioInfo.Title,
                    Description = scenarioContext.ScenarioInfo.Description,
                    TestType = TestType.TEST,
                    StartTime = DateTime.UtcNow,
                    Attributes = new HashSet<Attribute>(scenarioContext.ScenarioInfo.Tags?.Select(tag =>
                    {
                        var parts = tag.Split(new[] { ':' }, 2);
                        return parts.Length == 2
                            ? new Attribute { Key = parts[0], Value = parts[1] }
                            : new Attribute { Value = tag };
                    }) ?? Enumerable.Empty<Attribute>())
                };

                // fetch scenario parameters (from Examples block)
                var arguments = scenarioContext.ScenarioInfo.Arguments;
                if (arguments != null && arguments.Count > 0)
                {
                    var testNameWithParams = new StringBuilder(scenarioContext.ScenarioInfo.Title);

                    var parameters = (
                            from DictionaryEntry argument in arguments
                            select new KeyValuePair<string, string>(argument.Key.ToString(), argument.Value.ToString()))
                        .ToList();

                    // append args to test name
                    testNameWithParams.Append(" (");
                    testNameWithParams.Append(string.Join(", ", parameters.Select(kv => kv.Value)));
                    testNameWithParams.Append(")");
                    
                    if (testNameWithParams.Length > 1024)
                    {
                        testNameWithParams.Length = 1021;
                        testNameWithParams.Append("...");
                    }

                    startTest.TestName = testNameWithParams.ToString();

                    // append scenario outline parameters to description
                    var parametersInfo = new StringBuilder();
                    parametersInfo.Append("|");
                    foreach (var p in parameters)
                    {
                        parametersInfo.Append(p.Key);

                        parametersInfo.Append("|");
                    }

                    parametersInfo.AppendLine();
                    parametersInfo.Append("|");
                    foreach (var unused in parameters)
                    {
                        parametersInfo.Append("---");
                        parametersInfo.Append("|");
                    }

                    parametersInfo.AppendLine();
                    parametersInfo.Append("|");
                    foreach (var p in parameters)
                    {
                        parametersInfo.Append("**");
                        parametersInfo.Append(p.Value);
                        parametersInfo.Append("**");

                        parametersInfo.Append("|");
                    }

                    if (string.IsNullOrEmpty(startTest.Description))
                    {
                        startTest.Description = parametersInfo.ToString();
                    }
                    else
                    {
                        startTest.Description = parametersInfo + Environment.NewLine + Environment.NewLine +
                                                startTest.Description;
                    }
                }

                var eventArg = new TestStartedEventArgs(_client, startTest);
                OrangebeardAddIn.OnBeforeScenarioStarted(null, eventArg);

                if (eventArg.Canceled) return;

                Guid currentScenario;
                lock (_clientLock)
                {
                    currentScenario = _client.StartTest(startTest);
                }
                OrangebeardAddIn.SetScenarioGuid(scenarioContext, currentScenario);

                OrangebeardAddIn.OnAfterScenarioStarted(null,
                    new TestStartedEventArgs(_client, startTest));
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [AfterScenario]
        public static void AfterScenario(ScenarioContext scenarioContext)
        {
            try
            {
                var currentScenario = OrangebeardAddIn.GetScenarioGuid(scenarioContext);

                if (scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.UndefinedStep)
                {
                    lock (_clientLock)
                    {
                        _ = _client.Log(new Log
                        {
                            TestRunUUID = _testrunGuid,
                            TestUUID = currentScenario,
                            Message = new MissingStepDefinitionException().Message,
                            LogLevel = LogLevel.ERROR,
                            LogTime = DateTime.UtcNow,
                            LogFormat = LogFormat.PLAIN_TEXT
                        });
                    }
                }

                TestStatus status;
                if (scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.OK)
                {
                    status = TestStatus.PASSED;
                }
                else
                {
                    var exceptionTypeFullName = scenarioContext.TestError?.GetType().FullName;
                    if (scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.Skipped ||
                        scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.UndefinedStep ||
                        (exceptionTypeFullName != null && SkippedExceptionTypes.Contains(exceptionTypeFullName)))
                        status = TestStatus.SKIPPED;
                    else
                        status = TestStatus.FAILED;
                }

                var finishTest = new FinishTest
                {
                    TestRunUUID = _testrunGuid,
                    EndTime = DateTime.UtcNow,
                    Status = status
                };

                var eventArg = new TestFinishedEventArgs(currentScenario, _client, finishTest);
                OrangebeardAddIn.OnBeforeScenarioFinished(null, eventArg);

                if (eventArg.Canceled) return;

                lock (_clientLock)
                {
                    _client.FinishTest(currentScenario, finishTest);
                }

                OrangebeardAddIn.OnAfterScenarioFinished(null,
                    new TestFinishedEventArgs(currentScenario, _client, finishTest));
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [AfterScenario(Order = 30000)]
        public static void AfterScenarioTearDown(ScenarioContext scenarioContext)
        {
            OrangebeardAddIn.RemoveScenarioGuid(scenarioContext);
        }

        [BeforeStep(Order = -20000)]
        public static void BeforeStep(ScenarioContext scenarioContext)
        {
            try
            {
                var stepContext = scenarioContext.StepContext;
                var currentScenario = OrangebeardAddIn.GetScenarioGuid(scenarioContext);

                var startStep = new StartStep
                {
                    TestRunUUID = _testrunGuid,
                    TestUUID = currentScenario,
                    StepName = stepContext.StepInfo.GetCaption(),
                    StartTime = PreciseUtcTime.UtcNow
                };

                var eventArg = new StepStartedEventArgs(_client, startStep);
                OrangebeardAddIn.OnBeforeStepStarted(null, eventArg);

                if (eventArg.Canceled) return;

                Guid step;
                lock (_clientLock)
                {
                    step = _client.StartStep(startStep);
                }
                OrangebeardAddIn.SetStepGuid(scenarioContext, step);

                // step parameters
                var formattedParameters = stepContext.StepInfo.GetFormattedParameters();
                if (!string.IsNullOrEmpty(formattedParameters))
                {
                    lock (_clientLock)
                    {
                        _client.Log(new Log
                        {
                            TestRunUUID = _testrunGuid,
                            TestUUID = currentScenario,
                            StepUUID = step,
                            Message = formattedParameters,
                            LogLevel = LogLevel.INFO,
                            LogTime = DateTime.UtcNow,
                            LogFormat = LogFormat.MARKDOWN
                        });
                    }
                }

                OrangebeardAddIn.OnAfterStepStarted(null, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [AfterStep(Order = 20000)]
        public static void AfterStep(ScenarioContext scenarioContext)
        {
            try
            {
                var stepContext = scenarioContext.StepContext;
                var currentScenario = OrangebeardAddIn.GetScenarioGuid(scenarioContext);
                var currentStep = OrangebeardAddIn.GetStepGuid(scenarioContext);

                if (stepContext.Status == ScenarioExecutionStatus.TestError)
                {
                    lock (_clientLock)
                    {
                        _client.Log(new Log
                        {
                            TestRunUUID = _testrunGuid,
                            TestUUID = currentScenario,
                            StepUUID = currentStep.Value,
                            Message = scenarioContext.TestError?.ToString(),
                            LogLevel = LogLevel.ERROR,
                            LogTime = DateTime.UtcNow,
                            LogFormat = LogFormat.PLAIN_TEXT
                        });
                    }
                }
                else if (stepContext.Status == ScenarioExecutionStatus.BindingError)
                {
                    lock (_clientLock)
                    {
                        _client.Log(new Log
                        {
                            TestRunUUID = _testrunGuid,
                            TestUUID = currentScenario,
                            StepUUID = currentStep.Value,
                            Message = scenarioContext.TestError?.Message,
                            LogLevel = LogLevel.ERROR,
                            LogTime = DateTime.UtcNow,
                            LogFormat = LogFormat.PLAIN_TEXT
                        });
                    }
                }

                var finishStep = new FinishStep
                {
                    TestRunUUID = _testrunGuid,
                    EndTime = PreciseUtcTime.UtcNow,
                    Status = TestStatus.PASSED
                };

                if (scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.TestError)
                    finishStep.Status = TestStatus.FAILED;
                else if (scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.Skipped)
                    finishStep.Status = TestStatus.SKIPPED;

                var eventArg = new StepFinishedEventArgs(currentStep.Value, _client, finishStep);
                OrangebeardAddIn.OnBeforeStepFinished(null, eventArg);

                if (eventArg.Canceled) return;
                lock (_clientLock)
                {
                    _client.FinishStep(currentStep.Value, finishStep);
                }

                OrangebeardAddIn.RemoveStepGuid(scenarioContext);
                OrangebeardAddIn.OnAfterStepFinished(null, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }
    }
}
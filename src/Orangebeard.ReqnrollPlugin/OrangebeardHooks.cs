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
using Orangebeard.ReqnrollPlugin.LogHandler;
using Orangebeard.ReqnrollPlugin.Util;
using Reqnroll;
using Attribute = Orangebeard.Client.V3.Entity.Attribute;

namespace Orangebeard.ReqnrollPlugin
{
    [Binding]
    internal class OrangebeardHooks : Steps
    {
        private static readonly ILogger Logger = LogManager.Instance.GetLogger<OrangebeardHooks>();

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
        public static void BeforeTestRun()
        {
            try
            {
                var config = Initialize();

                var startTestRun = new StartTestRun()
                {
                    TestSetName = config.GetValue(ConfigurationPath.TestSetName, "Reqnroll Test Run"),
                    StartTime = DateTime.UtcNow,
                    Attributes = new HashSet<Attribute>(new HashSet<KeyValuePair<string, string>>(
                            config.GetKeyValues("TestSet:Attributes", new HashSet<KeyValuePair<string, string>>()))
                        .Select(a => new Attribute() { Key = a.Key, Value = a.Value }).ToList()),
                    Description = config.GetValue(ConfigurationPath.TestSetDescription, string.Empty)
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

        private static IConfiguration Initialize()
        {
            var args = new InitializingEventArgs(Plugin.Config);

            OrangebeardAddIn.OnInitializing(typeof(OrangebeardHooks), args);

            var orangebeardConfig = new OrangebeardConfiguration(Plugin.Config).WithListenerIdentification(
                "Reqnroll Plugin/" +
                typeof(OrangebeardHooks).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion
            );
            _client = new OrangebeardAsyncV3Client(orangebeardConfig);


            return args.Config;
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
                ContextHandler.ActiveFeatureContext = featureContext;

                lock (LockHelper.GetLock(
                          FeatureInfoEqualityComparer.GetFeatureInfoHashCode(featureContext.FeatureInfo)))
                {
                    var currentFeature = OrangebeardAddIn.GetCurrentFeatureGuid(featureContext);

                    if (currentFeature == null)
                    {
                        var startSuite = new StartSuite()
                        {
                            TestRunUUID = _testrunGuid,
                            Description = featureContext.FeatureInfo.Description,
                            Attributes = new HashSet<Attribute>(featureContext.FeatureInfo.Tags
                                ?.Select(t => new Attribute() { Value = t }) ?? new HashSet<Attribute>()),
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
                    else
                    {
                        OrangebeardAddIn.IncrementFeatureThreadCount(featureContext);
                    }
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
                lock (LockHelper.GetLock(
                          FeatureInfoEqualityComparer.GetFeatureInfoHashCode(featureContext.FeatureInfo)))
                {
                    var currentFeature = OrangebeardAddIn.GetCurrentFeatureGuid(featureContext);
                    OrangebeardAddIn.RemoveFeatureGuid(featureContext, currentFeature.Value);
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }

            finally
            {
                ContextHandler.ActiveFeatureContext = null;
            }
        }


        [BeforeScenario(Order = -20000)]
        public void BeforeScenario()
        {
            try
            {
                ContextHandler.ActiveScenarioContext = this.ScenarioContext;

                var currentFeature = OrangebeardAddIn.GetCurrentFeatureGuid(this.FeatureContext);

                if (currentFeature == null) return;

                var startTest = new StartTest()
                {
                    TestRunUUID = _testrunGuid,
                    SuiteUUID = currentFeature.Value,
                    TestName = this.ScenarioContext.ScenarioInfo.Title,
                    Description = this.ScenarioContext.ScenarioInfo.Description,
                    TestType = TestType.TEST,
                    StartTime = DateTime.UtcNow,
                    Attributes = new HashSet<Attribute>(this.ScenarioContext.ScenarioInfo.Tags
                        ?.Select(tag => new Attribute { Value = tag }) ?? new HashSet<Attribute>())
                };

                // fetch scenario parameters (from Examples block)
                var arguments = this.ScenarioContext.ScenarioInfo.Arguments;
                if (arguments != null && arguments.Count > 0)
                {
                    var testNameWithParams = new StringBuilder(ScenarioContext.ScenarioInfo.Title);

                    var parameters = (
                            from DictionaryEntry argument in arguments
                            select new KeyValuePair<string, string>(argument.Key.ToString(), argument.Value.ToString()))
                        .ToList();

                    // append args to test name
                    testNameWithParams.Append(" (");
                    testNameWithParams.Append(string.Join(", ", parameters.Select(kv => kv.Value)));
                    testNameWithParams.Append(")");

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
                OrangebeardAddIn.OnBeforeScenarioStarted(this, eventArg);

                if (eventArg.Canceled) return;

                var currentScenario = _client.StartTest(startTest);
                OrangebeardAddIn.SetScenarioGuid(this.ScenarioContext, currentScenario);

                OrangebeardAddIn.OnAfterScenarioStarted(this,
                    new TestStartedEventArgs(_client, startTest));
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [AfterScenario(Order = 20000)]
        public void AfterScenario()
        {
            try
            {
                var currentScenario = OrangebeardAddIn.GetScenarioGuid(this.ScenarioContext);
                
                if (this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.UndefinedStep)
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

                var status = ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.OK
                    ? TestStatus.PASSED
                    : ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.Skipped ||
                      ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.UndefinedStep
                        ? TestStatus.SKIPPED
                        : TestStatus.FAILED;

                var finishTest = new FinishTest
                {
                    TestRunUUID = _testrunGuid,
                    EndTime = DateTime.UtcNow,
                    Status = status
                };

                var eventArg = new TestFinishedEventArgs(currentScenario, _client, finishTest);
                OrangebeardAddIn.OnBeforeScenarioFinished(this, eventArg);

                if (eventArg.Canceled) return;

                _client.FinishTest(currentScenario, finishTest);

                OrangebeardAddIn.OnAfterScenarioFinished(this,
                    new TestFinishedEventArgs(currentScenario, _client, finishTest));

                OrangebeardAddIn.RemoveScenarioGuid(this.ScenarioContext, currentScenario);
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
            finally
            {
                ContextHandler.ActiveScenarioContext = null;
            }
        }

        [BeforeStep(Order = -20000)]
        public void BeforeStep()
        {
            try
            {
                ContextHandler.ActiveStepContext = this.StepContext;

                var currentScenario = OrangebeardAddIn.GetScenarioGuid(this.ScenarioContext);

                var startStep = new StartStep
                {
                    TestRunUUID = _testrunGuid,
                    TestUUID = currentScenario,
                    StepName = StepContext.StepInfo.GetCaption(),
                    StartTime = PreciseUtcTime.UtcNow
                };

                var eventArg = new StepStartedEventArgs(_client, startStep);
                OrangebeardAddIn.OnBeforeStepStarted(this, eventArg);

                if (eventArg.Canceled) return;

                var step = _client.StartStep(startStep);
                OrangebeardAddIn.SetStepGuid(this.StepContext, step);

                // step parameters
                var formattedParameters = this.StepContext.StepInfo.GetFormattedParameters();
                if (!string.IsNullOrEmpty(formattedParameters))
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

                OrangebeardAddIn.OnAfterStepStarted(this, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [AfterStep(Order = 20000)]
        public void AfterStep()
        {
            try
            {
                var currentScenario = OrangebeardAddIn.GetScenarioGuid(ScenarioContext);
                var currentStep = OrangebeardAddIn.GetStepGuid(StepContext);

                if (StepContext.Status == ScenarioExecutionStatus.TestError)
                {
                    _client.Log(new Log
                    {
                        TestRunUUID = _testrunGuid,
                        TestUUID = currentScenario,
                        StepUUID = currentStep.Value,
                        Message = ScenarioContext.TestError?.ToString(),
                        LogLevel = LogLevel.ERROR,
                        LogTime = DateTime.UtcNow,
                        LogFormat = LogFormat.PLAIN_TEXT
                    });
                }
                else if (this.StepContext.Status == ScenarioExecutionStatus.BindingError)
                {
                    _client.Log(new Log
                    {
                        TestRunUUID = _testrunGuid,
                        TestUUID = currentScenario,
                        StepUUID = currentStep.Value,
                        Message = ScenarioContext.TestError?.Message,
                        LogLevel = LogLevel.ERROR,
                        LogTime = DateTime.UtcNow,
                        LogFormat = LogFormat.PLAIN_TEXT
                    });
                }

                var finishStep = new FinishStep
                {
                    TestRunUUID = _testrunGuid,
                    EndTime = PreciseUtcTime.UtcNow,
                    Status = TestStatus.PASSED
                };

                if (ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.TestError)
                    finishStep.Status = TestStatus.FAILED;
                else if (ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.Skipped)
                    finishStep.Status = TestStatus.SKIPPED;

                var eventArg = new StepFinishedEventArgs(currentStep.Value, _client, finishStep);
                OrangebeardAddIn.OnBeforeStepFinished(this, eventArg);

                if (eventArg.Canceled) return;
                _client.FinishStep(currentStep.Value, finishStep);
               
                OrangebeardAddIn.RemoveStepGuid(this.StepContext, currentStep.Value);
                OrangebeardAddIn.OnAfterStepFinished(this, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
            finally
            {
                ContextHandler.ActiveStepContext = null;
            }
        }
    }
}
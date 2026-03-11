using System;
using Orangebeard.Client.V3.Entity;
using Orangebeard.Client.V3.Entity.Step;
using Orangebeard.ReqnrollPlugin.Extensions;
using Orangebeard.ReqnrollPlugin.Util;
using Reqnroll;
using Reqnroll.Infrastructure;

namespace Orangebeard.ReqnrollPlugin
{
    public class SkippedStepsHandler : ISkippedStepHandler
    {
        public void Handle(ScenarioContext scenarioContext)
        {
            try
            {
                var testRunGuid = OrangebeardHooks.GetTestRunGuid();
                var testGuid = OrangebeardAddIn.GetScenarioGuid(scenarioContext);
                var stepGuid = OrangebeardAddIn.GetStepGuid(scenarioContext);

                var skippedStep = new StartStep
                {
                    TestRunUUID = testRunGuid,
                    TestUUID = testGuid,
                    StepName = scenarioContext.StepContext.StepInfo.GetCaption(),
                    StartTime = PreciseUtcTime.UtcNow
                };

                if (stepGuid.HasValue)
                {
                    skippedStep.ParentStepUUID = stepGuid.Value;
                }

                Guid skippedStepGuid;
                lock (OrangebeardHooks._clientLock)
                {
                    skippedStepGuid = OrangebeardHooks.GetClient().StartStep(skippedStep);
                    OrangebeardHooks.GetClient().FinishStep(skippedStepGuid, new FinishStep
                    {
                        TestRunUUID = testRunGuid,
                        Status = TestStatus.SKIPPED,
                        EndTime = PreciseUtcTime.UtcNow
                    });
                }
            }
            catch (Exception)
            {
                // Orangebeard context not available; skip reporting
            }
        }
    }
}

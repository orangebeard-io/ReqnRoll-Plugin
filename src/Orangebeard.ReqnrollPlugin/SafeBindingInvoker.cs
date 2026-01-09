using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Reqnroll;
using Reqnroll.Bindings;
using Reqnroll.Configuration;
using Reqnroll.ErrorHandling;
using Reqnroll.EnvironmentAccess;
using Reqnroll.Infrastructure;
using Reqnroll.Tracing;

namespace Orangebeard.ReqnrollPlugin
{
    internal class SafeBindingInvoker : BindingInvoker
    {
        public SafeBindingInvoker(ReqnrollConfiguration reqnrollConfiguration, IErrorProvider errorProvider, IBindingDelegateInvoker synchronousBindingDelegateInvoker, IEnvironmentOptions environmentOptions)
            : base(reqnrollConfiguration, errorProvider, synchronousBindingDelegateInvoker, environmentOptions)
        {
        }

        public override async Task<object> InvokeBindingAsync(IBinding binding, IContextManager contextManager, object[] arguments, ITestTracer testTracer, DurationHolder durationHolder)
        {
            object result = null;

            try
            {
                result = await base.InvokeBindingAsync(binding, contextManager, arguments, testTracer, durationHolder);
            }
            catch (Exception ex)
            {
                PreserveStackTrace(ex);

                if (!(binding is IHookBinding hookBinding))
                {
                    throw;
                }

                if (hookBinding.HookType == HookType.BeforeScenario
                    || hookBinding.HookType == HookType.BeforeScenarioBlock
                    || hookBinding.HookType == HookType.BeforeStep
                    || hookBinding.HookType == HookType.AfterStep
                    || hookBinding.HookType == HookType.AfterScenario
                    || hookBinding.HookType == HookType.AfterScenarioBlock)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    // we don't have access to the stopwatch from the base method, so we can't get the real duration.
                    // this is a limitation of the new Reqnroll version.
                    testTracer.TraceError(ex, stopwatch.Elapsed);
                    SetTestError(contextManager.ScenarioContext, ex);
                }
            }

            return result;
        }

        private static void SetTestError(ScenarioContext context, Exception ex)
        {
            if (context != null && context.TestError == null)
            {
                context.GetType().GetProperty("ScenarioExecutionStatus")
                    ?.SetValue(context, ScenarioExecutionStatus.TestError);

                context.GetType().GetProperty("TestError")
                    ?.SetValue(context, ex);
            }
        }

        private static void PreserveStackTrace(Exception ex)
        {
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(ex, Array.Empty<object>());
        }
    }
}

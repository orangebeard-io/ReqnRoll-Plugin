using System;
using System.IO;
using System.Text.RegularExpressions;
using Orangebeard.Client.V3.Entity;
using Orangebeard.Client.V3.Entity.Attachment;
using Orangebeard.Client.V3.Entity.Log;
using Orangebeard.Client.V3.MimeTypes;
using Reqnroll;
using Reqnroll.Events;
using Reqnroll.Infrastructure;
using Reqnroll.Tracing;

namespace Orangebeard.ReqnrollPlugin
{
    public class OrangebeardOutputHelper : IReqnrollOutputHelper
    {
        private readonly ReqnrollOutputHelper _baseHelper;
        private readonly IContextManager _contextManager;

        private const string FilePathPattern = @"((((?<!\w)[A-Z,a-z]:)|(\.{0,2}\\))([^\b%\/\|:\n<>""']*))";

        public OrangebeardOutputHelper(ITestThreadExecutionEventPublisher testThreadExecutionEventPublisher,
            ITraceListener traceListener, IReqnrollAttachmentHandler reqnrollAttachmentHandler,
            IContextManager contextManager)
        {
            _contextManager = contextManager;
            _baseHelper = new ReqnrollOutputHelper(testThreadExecutionEventPublisher, traceListener,
                reqnrollAttachmentHandler, contextManager);
        }

        public void AddAttachment(string filePath)
        {
            try
            {
                SendAttachment(GetAttachmentFileFromPath(filePath), null);
            }
            catch (Exception)
            {
                // Orangebeard context not available; skip reporting
            }

            _baseHelper.AddAttachment(filePath);
        }

        public void WriteLine(string message)
        {
            try
            {
                SendLog(message);
            }
            catch (Exception)
            {
                // Orangebeard context not available; skip reporting
            }

            _baseHelper.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            try
            {
                SendLog(string.Format(format, args));
            }
            catch (Exception)
            {
                // Orangebeard context not available; skip reporting
            }

            _baseHelper.WriteLine(format, args);
        }

        private void SendLog(string message)
        {
            var match = Regex.Match(message, FilePathPattern);
            if (match.Success) //Look only at first match, as we support max 1 attachment per log entry
            {
                var filePath = match.Value;
                if (!Path.IsPathRooted(filePath))
                {
                    filePath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + filePath;
                }

                SendAttachment(GetAttachmentFileFromPath(filePath), message);
            }
            else
            {
                SendLog(message, LogLevel.INFO);
            }
        }

        private Guid SendLog(string message, LogLevel level)
        {
            var scenarioContext = _contextManager.ScenarioContext;
            var testRunGuid = OrangebeardHooks.GetTestRunGuid();
            var testGuid = OrangebeardAddIn.GetScenarioGuid(scenarioContext);
            var stepGuid = OrangebeardAddIn.GetStepGuid(scenarioContext);

            var log = new Log
            {
                TestRunUUID = testRunGuid,
                TestUUID = testGuid,
                Message = message,
                LogLevel = level,
                LogTime = DateTime.UtcNow,
                LogFormat = LogFormat.MARKDOWN
            };

            if (stepGuid.HasValue)
            {
                log.StepUUID = stepGuid.Value;
            }

            lock (OrangebeardHooks._clientLock)
            {
                return OrangebeardHooks.GetClient().Log(log);
            }
        }

        private void SendAttachment(AttachmentFile attachment, string message)
        {
            if (attachment == null) return;

            var scenarioContext = _contextManager.ScenarioContext;
            var testRunGuid = OrangebeardHooks.GetTestRunGuid();
            var testGuid = OrangebeardAddIn.GetScenarioGuid(scenarioContext);
            var stepGuid = OrangebeardAddIn.GetStepGuid(scenarioContext);

            if (message == null)
            {
                message = "Attachment: " + attachment.Name;
            }

            var logItem = SendLog(message, LogLevel.INFO);

            var attachmentMeta = new AttachmentMetaData
            {
                TestRunUUID = testRunGuid,
                TestUUID = testGuid,
                AttachmentTime = DateTime.UtcNow,
                LogUUID = logItem
            };

            if (stepGuid.HasValue)
            {
                attachmentMeta.StepUUID = stepGuid.Value;
            }

            lock (OrangebeardHooks._clientLock)
            {
                OrangebeardHooks.GetClient().SendAttachment(new Attachment
                {
                    File = attachment,
                    MetaData = attachmentMeta
                });
            }
        }

        private AttachmentFile GetAttachmentFileFromPath(string filePath)
        {
            try
            {
                return new AttachmentFile
                {
                    ContentType = MimeTypeMap.GetMimeType(Path.GetExtension(filePath)),
                    Name = Path.GetFileName(filePath),
                    Content = File.ReadAllBytes(filePath)
                };
            }
            catch (Exception e)
            {
                SendLog($"\r\nFailed to attach {filePath} ({e.Message})", LogLevel.WARN);
                return null;
            }
        }
    }
}

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

        private const string FilePathPattern = @"((((?<!\w)[A-Z,a-z]:)|(\.{0,2}\\))([^\b%\/\|:\n<>""']*))";

        public OrangebeardOutputHelper(ITestThreadExecutionEventPublisher testThreadExecutionEventPublisher,
            ITraceListener traceListener, IReqnrollAttachmentHandler reqnrollAttachmentHandler)
        {
            _baseHelper = new ReqnrollOutputHelper(testThreadExecutionEventPublisher, traceListener,
                reqnrollAttachmentHandler);
        }

        public void AddAttachment(string filePath)
        {
            SendAttachment(GetAttachmentFileFromPath(filePath), null);
            _baseHelper.AddAttachment(filePath);
        }

        public void WriteLine(string text)
        {
            SendLog(text);
            _baseHelper.WriteLine(text);
        }

        public void WriteLine(string format, params object[] args)
        {
            SendLog(string.Format(format, args));
            _baseHelper.WriteLine(format, args);
        }

        private static void SendLog(string message)
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

        private static Guid SendLog(string message, LogLevel level)
        {
            var context = OrangebeardAddIn.GetCurrentContext();

            var log = new Log
            {
                TestRunUUID = context.testrun,
                TestUUID = context.test,
                Message = message,
                LogLevel = level,
                LogTime = DateTime.UtcNow,
                LogFormat = LogFormat.MARKDOWN
            };

            if (context.step.HasValue)
            {
                log.StepUUID = context.step.Value;
            }

            return OrangebeardHooks.GetClient().Log(log);
        }

        private static void SendAttachment(AttachmentFile attachment, string message)
        {
            var context = OrangebeardAddIn.GetCurrentContext();
            if (attachment == null) return;

            var logItem = SendLog(message, LogLevel.INFO);

            var attachmentMeta = new AttachmentMetaData
            {
                TestRunUUID = context.testrun,
                TestUUID = context.test,
                AttachmentTime = DateTime.UtcNow,
                LogUUID = logItem
            };

            if (context.step.HasValue)
            {
                attachmentMeta.StepUUID = context.step.Value;
            }

            OrangebeardHooks.GetClient().SendAttachment(new Attachment
            {
                File = attachment,
                MetaData = attachmentMeta
            });
        }

        private static AttachmentFile GetAttachmentFileFromPath(string filePath)
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
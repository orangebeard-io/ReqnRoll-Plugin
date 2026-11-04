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
            // Binary payload support: WriteLine("pdf", bytes) or WriteLine("pdf", bytes, "report.pdf")
            // Any byte[]/Stream argument is sent as an attachment; the first string argument after it is the file name.
            if (TryExtractBinary(args, out var content, out var fileName, out var textArgs))
            {
                var message = textArgs.Length > 0 ? SafeFormat(format, textArgs) : format;
                try
                {
                    var attachment = new AttachmentFile
                    {
                        Name = fileName ?? DefaultFileName(format, content),
                        Content = content
                    };
                    attachment.ContentType = MimeTypeMap.GetMimeType(Path.GetExtension(attachment.Name));
                    SendAttachment(attachment, message);
                }
                catch (Exception)
                {
                    // Orangebeard context not available; skip reporting
                }

                _baseHelper.WriteLine(message);
                return;
            }

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

        internal static string SafeFormat(string format, object[] args)
        {
            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        internal static bool TryExtractBinary(object[] args, out byte[] content, out string fileName, out object[] textArgs)
        {
            content = null;
            fileName = null;
            textArgs = args ?? new object[0];
            if (args == null) return false;

            var index = Array.FindIndex(args, a => a is byte[] || a is Stream);
            if (index < 0) return false;

            if (args[index] is byte[] bytes)
            {
                content = bytes;
            }
            else
            {
                var stream = (Stream)args[index];
                if (stream.CanSeek) stream.Position = 0;
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    content = ms.ToArray();
                }
            }

            // Optional file name: the string argument directly following the binary one
            var rest = new System.Collections.Generic.List<object>(args);
            rest.RemoveAt(index);
            if (index < rest.Count && rest[index] is string name && !string.IsNullOrWhiteSpace(name))
            {
                fileName = name;
                rest.RemoveAt(index);
            }

            textArgs = rest.ToArray();
            return true;
        }

        internal static string DefaultFileName(string label, byte[] content)
        {
            var baseName = string.IsNullOrWhiteSpace(label) ? "attachment" : label.Trim();
            foreach (var c in Path.GetInvalidFileNameChars()) baseName = baseName.Replace(c, '_');
            if (!string.IsNullOrEmpty(Path.GetExtension(baseName))) return baseName;

            var ext = SniffExtension(content);
            if (ext == null && baseName.Equals("pdf", StringComparison.OrdinalIgnoreCase)) ext = ".pdf";
            return baseName + (ext ?? ".bin");
        }

        internal static string SniffExtension(byte[] c)
        {
            if (c == null || c.Length < 4) return null;
            if (c[0] == '%' && c[1] == 'P' && c[2] == 'D' && c[3] == 'F') return ".pdf";
            if (c[0] == 0x89 && c[1] == 'P' && c[2] == 'N' && c[3] == 'G') return ".png";
            if (c[0] == 0xFF && c[1] == 0xD8) return ".jpg";
            if (c[0] == 'P' && c[1] == 'K') return ".zip";
            return null;
        }

        private void SendLog(string message)
        {
            var match = Regex.Match(message, FilePathPattern);
            if (match.Success && LooksLikePath(match.Value)) //Look only at first match, as we support max 1 attachment per log entry
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

        // Guards against false positives, e.g. XML/text containing "X:" that the regex picks up
        private static bool LooksLikePath(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate)) return false;
            if (candidate.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;
            if (candidate.IndexOfAny(new[] { '<', '>', '|', '"', '*', '?', '\r', '\n' }) >= 0) return false;
            return !string.IsNullOrEmpty(Path.GetFileName(candidate.TrimEnd()));
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

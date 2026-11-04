using System.IO;
using System.Text;
using NUnit.Framework;
using Orangebeard.ReqnrollPlugin;

namespace Orangebeard.ReqnrollPlugin.Tests
{
    [TestFixture]
    public class OutputHelperBinaryTests
    {
        private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7 content");

        [Test]
        public void NoBinaryArgument_IsNotTreatedAsBinary()
        {
            var result = OrangebeardOutputHelper.TryExtractBinary(new object[] { "a", 1 }, out var content, out var name, out var text);

            Assert.That(result, Is.False);
            Assert.That(content, Is.Null);
            Assert.That(name, Is.Null);
            Assert.That(text, Is.EqualTo(new object[] { "a", 1 }));
        }

        [Test]
        public void NullArgs_IsNotTreatedAsBinary()
        {
            Assert.That(OrangebeardOutputHelper.TryExtractBinary(null, out _, out _, out var text), Is.False);
            Assert.That(text, Is.Empty);
        }

        [Test]
        public void ByteArray_IsExtracted_WithoutFileName()
        {
            var result = OrangebeardOutputHelper.TryExtractBinary(new object[] { Pdf }, out var content, out var name, out var text);

            Assert.That(result, Is.True);
            Assert.That(content, Is.SameAs(Pdf));
            Assert.That(name, Is.Null);
            Assert.That(text, Is.Empty);
        }

        [Test]
        public void StringAfterBinary_IsUsedAsFileName()
        {
            OrangebeardOutputHelper.TryExtractBinary(new object[] { Pdf, "report.pdf" }, out _, out var name, out var text);

            Assert.That(name, Is.EqualTo("report.pdf"));
            Assert.That(text, Is.Empty);
        }

        [Test]
        public void StringBeforeBinary_IsKeptAsFormatArgument()
        {
            OrangebeardOutputHelper.TryExtractBinary(new object[] { "x", Pdf }, out _, out var name, out var text);

            Assert.That(name, Is.Null);
            Assert.That(text, Is.EqualTo(new object[] { "x" }));
        }

        [Test]
        public void BlankFileName_IsIgnored()
        {
            OrangebeardOutputHelper.TryExtractBinary(new object[] { Pdf, "  " }, out _, out var name, out _);

            Assert.That(name, Is.Null);
        }

        [Test]
        public void Stream_IsReadFromStart()
        {
            using (var stream = new MemoryStream(Pdf))
            {
                stream.Position = stream.Length; // already consumed
                OrangebeardOutputHelper.TryExtractBinary(new object[] { stream, "a.pdf" }, out var content, out var name, out _);

                Assert.That(content, Is.EqualTo(Pdf));
                Assert.That(name, Is.EqualTo("a.pdf"));
            }
        }

        [Test]
        public void DefaultFileName_PdfContent_GetsPdfExtension()
        {
            Assert.That(OrangebeardOutputHelper.DefaultFileName("pdf", Pdf), Is.EqualTo("pdf.pdf"));
            Assert.That(OrangebeardOutputHelper.DefaultFileName("Invoice", Pdf), Is.EqualTo("Invoice.pdf"));
        }

        [Test]
        public void DefaultFileName_LabelPdf_WithUnknownContent_StillGetsPdfExtension()
        {
            Assert.That(OrangebeardOutputHelper.DefaultFileName("PDF", new byte[] { 1, 2, 3, 4, 5 }), Is.EqualTo("PDF.pdf"));
        }

        [Test]
        public void DefaultFileName_UnknownContent_FallsBackToBin()
        {
            Assert.That(OrangebeardOutputHelper.DefaultFileName("data", new byte[] { 1, 2, 3, 4, 5 }), Is.EqualTo("data.bin"));
        }

        [Test]
        public void DefaultFileName_KeepsExistingExtension()
        {
            Assert.That(OrangebeardOutputHelper.DefaultFileName("report.pdf", Pdf), Is.EqualTo("report.pdf"));
        }

        [Test]
        public void DefaultFileName_SanitizesInvalidCharacters_AndHandlesEmptyLabel()
        {
            Assert.That(OrangebeardOutputHelper.DefaultFileName("a/b", Pdf), Is.EqualTo("a_b.pdf"));
            Assert.That(OrangebeardOutputHelper.DefaultFileName("", Pdf), Is.EqualTo("attachment.pdf"));
        }

        [TestCase(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0 }, ".png")]
        [TestCase(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, ".jpg")]
        [TestCase(new byte[] { 0x50, 0x4B, 3, 4 }, ".zip")]
        [TestCase(new byte[] { 1, 2 }, null)]
        public void SniffExtension_DetectsKnownFormats(byte[] content, string expected)
        {
            Assert.That(OrangebeardOutputHelper.SniffExtension(content), Is.EqualTo(expected));
        }

        [Test]
        public void SafeFormat_FormatsArguments_AndSurvivesBadFormat()
        {
            Assert.That(OrangebeardOutputHelper.SafeFormat("n={0}", new object[] { 5 }), Is.EqualTo("n=5"));
            Assert.That(OrangebeardOutputHelper.SafeFormat("{0} {1}", new object[] { 5 }), Is.EqualTo("{0} {1}"));
        }
    }
}

using System.Net.Http.Headers;
using Digi.Recruitment.Module.Domain.AI.Multinet;
using Xunit;

namespace Digi.Recruitment.Module.Tests.Multinet
{
    /// <summary>
    /// Regression cover for a live failure: a valid PDF was rejected with
    /// 422 "File type '' is not supported".
    ///
    /// The CV was named "Dominic Alvarez — Cybersecurity Analyst.pdf". The em
    /// dash is non-ASCII, so .NET RFC-2047-encodes the whole file name into a
    /// base64 word for the Content-Disposition header. Base64 has no dot, so
    /// the parser at the far end read the suffix as empty and refused the file.
    ///
    /// The service was right to refuse it — the defect was entirely ours, in
    /// what we put on the wire.
    /// </summary>
    public class ResumeTransportFileNameTests
    {
        /// <summary>What actually reaches the service's `filename` parameter.</summary>
        private static string FileNameOnTheWire(string fileName)
        {
            var form = new MultipartFormDataContent();
            var part = new StreamContent(new MemoryStream(new byte[] { 1, 2, 3 }));
            form.Add(part, "file", ResumeUploadValidator.ToTransportFileName(fileName));
            return part.Headers.ContentDisposition!.FileName!.Trim('"');
        }

        [Theory]
        [InlineData("Dominic Alvarez — Cybersecurity Analyst.pdf")]   // em dash, the reported case
        [InlineData("Zoë Fernández CV.pdf")]                          // accents
        [InlineData("履歴書.pdf")]                                     // no Latin characters at all
        [InlineData("résumé (final) v2.docx")]
        [InlineData("plain_name.pdf")]
        public void ExtensionSurvivesTheWire(string original)
        {
            var onTheWire = FileNameOnTheWire(original);

            Assert.DoesNotContain("=?utf-8?", onTheWire);
            Assert.EndsWith(Path.GetExtension(original).ToLowerInvariant(), onTheWire);

            // The service reads the suffix exactly this way.
            var suffix = Path.GetExtension(onTheWire);
            Assert.False(string.IsNullOrEmpty(suffix), $"suffix was empty for '{onTheWire}'");
        }

        [Fact]
        public void AccentsKeepTheirBaseLetterRatherThanBecomingUnderscores()
        {
            Assert.Equal("Zoe_Fernandez_CV.pdf", ResumeUploadValidator.ToTransportFileName("Zoë Fernández CV.pdf"));
        }

        [Fact]
        public void NonLatinNameFallsBackInsteadOfBeingRejected()
        {
            // A CV named entirely in another script is a legitimate upload, not
            // a bad one. It must still parse.
            Assert.Equal("resume.pdf", ResumeUploadValidator.ToTransportFileName("履歴書.pdf"));
        }

        [Fact]
        public void RunsOfPunctuationCollapseRatherThanStacking()
        {
            Assert.Equal("Dominic_Alvarez_Cybersecurity_Analyst.pdf",
                ResumeUploadValidator.ToTransportFileName("Dominic Alvarez — Cybersecurity Analyst.pdf"));
        }

        [Fact]
        public void AsciiNamesAreLeftAlone()
        {
            Assert.Equal("taha_zaidi.pdf", ResumeUploadValidator.ToTransportFileName("taha_zaidi.pdf"));
        }

        [Fact]
        public void PathIsStrippedSoOnlyTheLeafTravels()
        {
            Assert.Equal("cv.pdf",
                ResumeUploadValidator.ToTransportFileName("superadmin/recruitment/resumes/documents/cv.pdf"));
        }
    }
}

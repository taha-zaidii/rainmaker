using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace Digi.Core.AI.Providers.Generic
{
    /// <summary>
    /// Multinet's service accepts resume bytes directly and parses them
    /// server-side; a general-purpose chat model only accepts text. This is the
    /// one place a generic provider turns a file into the text it prompts with.
    ///
    /// PDF only, deliberately. DOCX text extraction needs a second dependency
    /// (OpenXML) for a format that is a shrinking minority of uploads; rather
    /// than add that surface for an edge case, an unsupported extension fails
    /// cleanly and says so, instead of silently mis-extracting.
    /// </summary>
    internal static class ResumeTextExtractor
    {
        public static (bool Success, string Text, string? Error) TryExtractText(byte[] fileBytes, string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".txt" => (true, Encoding.UTF8.GetString(fileBytes), null),
                ".pdf" => TryExtractPdf(fileBytes),
                _ => (false, string.Empty,
                    $"'{extension}' resumes are not yet supported by this provider — only .pdf and .txt. " +
                    "Switch this company to Multinet's AI service for full format support.")
            };
        }

        private static (bool Success, string Text, string? Error) TryExtractPdf(byte[] fileBytes)
        {
            try
            {
                using var stream = new MemoryStream(fileBytes);
                using var reader = new PdfReader(stream);
                using var pdfDoc = new PdfDocument(reader);

                var text = new StringBuilder();
                for (var page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                {
                    text.AppendLine(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page)));
                }

                var result = text.ToString();
                return string.IsNullOrWhiteSpace(result)
                    ? (false, string.Empty, "No readable text found — the PDF may be a scanned image rather than text.")
                    : (true, result, null);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Could not read the PDF: {ex.Message}");
            }
        }
    }
}

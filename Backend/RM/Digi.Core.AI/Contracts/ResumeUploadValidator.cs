using System.IO;
using Digi.Core.AI.Configuration;

namespace Digi.Core.AI.Contracts
{
    /// <summary>
    /// Local gate for resume uploads, mirroring the AI service's own checks.
    ///
    /// The service already validates extension, size and magic bytes and answers
    /// 413/422. We repeat the checks here for two reasons: a recruiter gets the
    /// rejection instantly instead of after a queue wait, and a renamed payload
    /// never reaches the parser surface or our blob store at all. The service
    /// stays the authority — this is defence in depth, not a replacement.
    /// </summary>
    public static class ResumeUploadValidator
    {
        /// <summary>
        /// Magic-byte prefixes per extension. A .docx is a ZIP container, so it
        /// shares the PK signature. Mirrors _MAGIC_ACCEPT in the service.
        /// </summary>
        private static readonly Dictionary<string, byte[][]> Signatures =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },                   // %PDF
                [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
                [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
                [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
                [".docx"] = new[]
                {
                    new byte[] { 0x50, 0x4B, 0x03, 0x04 },   // PK\x03\x04  normal zip
                    new byte[] { 0x50, 0x4B, 0x05, 0x06 },   // empty archive
                    new byte[] { 0x50, 0x4B, 0x07, 0x08 }    // spanned archive
                }
            };

        /// <summary>Longest signature we need to read to make a decision.</summary>
        public const int MagicByteWindow = 8;

        /// <summary>Content type to declare for a given extension on the multipart part.</summary>
        public static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        /// <summary>
        /// Validate a candidate upload. <paramref name="header"/> may be shorter
        /// than <see cref="MagicByteWindow"/>; a file too short to carry a
        /// signature is rejected as a mismatch rather than assumed valid.
        /// Pass an empty span to skip the content check (e.g. when re-submitting
        /// a blob already validated on the way in).
        /// </summary>
        public static AiError? Validate(
            string? fileName,
            long sizeBytes,
            ReadOnlySpan<byte> header,
            MultinetAiOptions options)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return new AiError(AiErrorCode.BadRequest, "A file name is required.", Retryable: false);
            }

            if (sizeBytes <= 0)
            {
                return new AiError(AiErrorCode.RejectedLocally, "The file is empty.", Retryable: false);
            }

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension) ||
                !options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                var accepted = string.Join(", ", options.AllowedExtensions);
                return new AiError(
                    AiErrorCode.UnsupportedFileType,
                    $"'{extension}' files are not supported. Accepted types: {accepted}.",
                    Retryable: false,
                    ServiceErrorCode: "unsupported_file_type");
            }

            if (sizeBytes > options.MaxUploadBytes)
            {
                var actualMb = sizeBytes / 1024d / 1024d;
                return new AiError(
                    AiErrorCode.FileTooLarge,
                    $"The file is {actualMb:0.#} MB. The limit is {options.MaxUploadMegabytes} MB.",
                    Retryable: false,
                    ServiceErrorCode: "file_too_large");
            }

            if (!header.IsEmpty && !MatchesSignature(extension, header))
            {
                return new AiError(
                    AiErrorCode.ContentTypeMismatch,
                    $"The file contents do not look like a real {extension} file. " +
                    "It may be corrupt, or renamed from another format.",
                    Retryable: false,
                    ServiceErrorCode: "content_type_mismatch");
            }

            return null;
        }

        /// <summary>True when the leading bytes match one of the signatures registered for the extension.</summary>
        public static bool MatchesSignature(string extension, ReadOnlySpan<byte> header)
        {
            if (!Signatures.TryGetValue(extension, out var candidates))
            {
                return false;
            }

            foreach (var signature in candidates)
            {
                if (header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reduce a file name to plain ASCII for the multipart
        /// <c>Content-Disposition</c> header, keeping the extension intact.
        ///
        /// This is not cosmetic. .NET RFC-2047-encodes any file name holding a
        /// non-ASCII character, so "Dominic Alvarez — Cybersecurity Analyst.pdf"
        /// (em dash, U+2014) goes on the wire as:
        ///
        ///     filename="=?utf-8?B?RG9taW5pYyBBbHZhcmV6IOKAlCBDeWJlcnNlY3VyaXR5IEFuYWx5c3QucGRm?="
        ///
        /// The base64 payload contains no dot, so the service reads the suffix
        /// as empty and answers 422 <c>File type '' is not supported</c> — for a
        /// perfectly valid PDF. .NET also emits a correct RFC-5987
        /// <c>filename*</c>, but a parser that reads only <c>filename</c> never
        /// sees it, and we do not control the parser at the other end.
        ///
        /// Sending ASCII means both forms agree and no encoding is triggered.
        /// The name here is transport packaging only — the candidate's real file
        /// name is preserved in blob storage and on the parsing record, so
        /// nothing is lost by flattening it.
        /// </summary>
        public static string ToTransportFileName(string? fileName)
        {
            var name = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(name))
            {
                return "resume";
            }

            var extension = Path.GetExtension(name);
            var stem = Path.GetFileNameWithoutExtension(name);

            // FormD splits "é" into "e" + a combining accent, so dropping the
            // marks keeps a readable "Fernandez" instead of "Fern_ndez".
            var builder = new System.Text.StringBuilder(stem.Length);
            foreach (var ch in stem.Normalize(System.Text.NormalizationForm.FormD))
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch)
                    == System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '_')
                {
                    builder.Append(ch);
                }
                else if (builder.Length > 0 && builder[^1] != '_')
                {
                    builder.Append('_');
                }
            }

            var safeStem = builder.ToString().Trim('_');

            // A name written entirely in a non-Latin script flattens to nothing.
            // That is a legitimate CV, not a bad upload, so it gets a neutral
            // stem rather than a rejection.
            if (safeStem.Length == 0)
            {
                safeStem = "resume";
            }
            else if (safeStem.Length > 100)
            {
                safeStem = safeStem[..100];
            }

            // The extension was already checked against AllowedExtensions, so it
            // is ASCII by construction.
            return safeStem + extension.ToLowerInvariant();
        }
    }
}

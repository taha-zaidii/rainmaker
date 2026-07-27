namespace Digi.Recruitment.Module.Domain.AI.Multinet
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
    }
}

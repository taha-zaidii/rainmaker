namespace Digi.Recruitment.Module.Domain.AI.Multinet
{
    /// <summary>
    /// Cleans free-text field values before they are sent to the AI service.
    ///
    /// The contract asks callers not to send placeholder strings — "N/A", "-",
    /// "string" — and to omit the field instead. That is not pedantry: the
    /// service treats what it receives as a fact a human asserted, so a literal
    /// "N/A" in <c>experienceRequired</c> becomes a genuine constraint on the
    /// generated text rather than the absence of one.
    ///
    /// ERP forms are full of these. Required fields get "-" typed into them to
    /// get past validation, and API testers leave Swagger's "string" default in
    /// place. Filtering them here means every feature gets it for free.
    /// </summary>
    public static class MultinetAiText
    {
        /// <summary>
        /// Values that mean "nothing was entered". Matched only against the WHOLE
        /// trimmed value, case-insensitively — a substring match would eat real
        /// content such as the job title "Nil Desperandum Analyst" or a skill
        /// list containing "None of the above".
        /// </summary>
        private static readonly HashSet<string> Placeholders = new(StringComparer.OrdinalIgnoreCase)
        {
            "n/a", "n.a.", "n.a", "na", "none", "null", "nil", "undefined",
            "-", "--", "---", "_", ".", "..", "...", "?", "??",
            "tbd", "tba", "to be decided", "to be determined", "to be confirmed",
            "string", "not applicable", "not specified", "unspecified", "empty"
        };

        /// <summary>
        /// Returns the trimmed value, or null when it is blank or a placeholder.
        /// Callers should omit the field entirely when this returns null.
        /// </summary>
        public static string? Clean(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return Placeholders.Contains(trimmed) ? null : trimmed;
        }

        /// <summary>
        /// Cleans a list, dropping placeholders, blanks and duplicates. Returns
        /// null rather than an empty list, so the field is omitted from the
        /// payload instead of being sent as <c>[]</c> — an empty array reads as
        /// "there are no valid options", which is a different claim.
        /// </summary>
        public static List<string>? Clean(IEnumerable<string>? values)
        {
            if (values is null)
            {
                return null;
            }

            var cleaned = values
                .Select(Clean)
                .Where(v => v is not null)
                .Select(v => v!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return cleaned.Count == 0 ? null : cleaned;
        }
    }
}

using Microsoft.Extensions.Options;

namespace Digi.Recruitment.Module.Domain.AI.Multinet
{
    /// <summary>
    /// Contract-shaped canned responses, used when <c>MultinetAI:StubMode</c> is on.
    ///
    /// This exists so that neither frontend nor backend work ever blocks on the
    /// GPU being busy, and so a demo degrades gracefully with the AI service
    /// completely offline. It is a first-class part of the design, not a mock:
    /// it returns the same envelope, the same schema version, and a deliberately
    /// MIXED provenance map so the review UI's flagging path is exercised on
    /// every run rather than only when a real scan happens to escalate.
    ///
    /// The canned profile is synthetic. No real candidate data is embedded here —
    /// resumes and anything derived from them never enter source control.
    /// </summary>
    public sealed class StubMultinetAiClient : IMultinetAiClient
    {
        private readonly MultinetAiOptions _options;
        private readonly ILogger<StubMultinetAiClient> _logger;

        public StubMultinetAiClient(
            IOptions<MultinetAiOptions> options,
            ILogger<StubMultinetAiClient> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool IsStub => true;

        public Task<AiResult<ServiceHealth>> GetHealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AiResult<ServiceHealth>.Ok(new ServiceHealth
            {
                Status = "healthy",
                Service = "hrms-resume-parser (STUB)",
                Version = "1.1.0"
            }));

        public Task<AiResult<ServiceReadiness>> GetReadinessAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AiResult<ServiceReadiness>.Ok(new ServiceReadiness
            {
                Status = "ready",
                Model = "qwen3.5:27b (STUB)",
                Backend = "stub",
                LlmBackend = new LlmBackendStatus { Reachable = true, Version = "stub" }
            }));

        public Task<AiResult<ServiceVersion>> GetVersionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AiResult<ServiceVersion>.Ok(new ServiceVersion
            {
                ServiceVersion_ = "1.1.0",
                SchemaVersion = ProfileSchemaVersions.Supported,
                Model = "qwen3.5:27b (STUB)",
                Backend = "stub"
            }));

        public async Task<AiResult<ParseResumeResult>> ExtractResumeAsync(
            Stream content,
            string fileName,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            // Stub mode still enforces the real upload rules. Otherwise the error
            // paths would only ever be tested against the live service, which is
            // exactly the situation stub mode exists to avoid.
            var header = new byte[ResumeUploadValidator.MagicByteWindow];
            var read = 0;
            var size = 1L;

            if (content is { CanRead: true, CanSeek: true })
            {
                size = content.Length;
                var origin = content.Position;
                read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken)
                    .ConfigureAwait(false);
                content.Position = origin;
            }

            var localError = ResumeUploadValidator.Validate(
                fileName, size, new ReadOnlySpan<byte>(header, 0, read), _options);

            if (localError is not null)
            {
                return AiResult<ParseResumeResult>.Fail(localError);
            }

            // Feel like the real thing: a real parse takes 40–90 s, and a UI that
            // is only ever tested against an instant response looks broken later.
            if (_options.StubLatencySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.StubLatencySeconds), cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger.LogInformation(
                "STUB parse of {File} — returning canned ProfileSchema {Version}. " +
                "No call left this process and no GPU was used.",
                Path.GetFileName(fileName), ProfileSchemaVersions.Supported);

            return AiResult<ParseResumeResult>.Ok(BuildCannedResult(fileName));
        }

        public Task<AiResult<CandidateIndexResult>> ListCandidatesAsync(
            string? apiKey = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(AiResult<CandidateIndexResult>.Ok(new CandidateIndexResult
            {
                Count = 2,
                Candidates = new List<CandidateIndexEntry>
                {
                    new() { ProfileId = "stub_candidate_alpha", Name = "Ayesha Khan (stub)" },
                    new() { ProfileId = "stub_candidate_beta", Name = "Bilal Ahmed (stub)" }
                }
            }));

        public Task<AiResult<RankResult>> RankAsync(
            string jobDescription,
            int topK,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            var jd = jobDescription?.Trim() ?? string.Empty;
            if (jd.Length < 30)
            {
                return Task.FromResult(AiResult<RankResult>.Fail(
                    AiErrorCode.BadRequest,
                    "The job description must be at least 30 characters for ranking to be meaningful."));
            }

            if (topK is < 1 or > 50)
            {
                return Task.FromResult(AiResult<RankResult>.Fail(
                    AiErrorCode.BadRequest, "topK must be between 1 and 50."));
            }

            var ranking = new List<System.Text.Json.JsonElement>
            {
                ToElement(new
                {
                    profile_id = "stub_candidate_alpha",
                    name = "Ayesha Khan (stub)",
                    score = 0.87,
                    section_scores = new { skills = 0.91, experience = 0.84, summary = 0.79, projects = 0.82 }
                }),
                ToElement(new
                {
                    profile_id = "stub_candidate_beta",
                    name = "Bilal Ahmed (stub)",
                    score = 0.61,
                    section_scores = new { skills = 0.58, experience = 0.66, summary = 0.55, projects = 0.60 }
                })
            };

            return Task.FromResult(AiResult<RankResult>.Ok(new RankResult
            {
                ModelVersion = "stub-embeddings-1",
                SectionWeights = new Dictionary<string, double>
                {
                    ["skills"] = 0.45, ["experience"] = 0.35, ["summary"] = 0.10, ["projects"] = 0.10
                },
                Ranking = ranking.Take(topK).ToList()
            }));
        }

        public async Task<AiResult<ScoreResult>> ScoreAsync(
            string profileId,
            string jobDescription,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return AiResult<ScoreResult>.Fail(AiErrorCode.BadRequest, "A profile id is required.");
            }

            if ((jobDescription?.Trim().Length ?? 0) < 30)
            {
                return AiResult<ScoreResult>.Fail(
                    AiErrorCode.BadRequest,
                    "The job description must be at least 30 characters for scoring to be meaningful.");
            }

            if (_options.StubLatencySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.StubLatencySeconds), cancellationToken)
                    .ConfigureAwait(false);
            }

            return AiResult<ScoreResult>.Ok(new ScoreResult
            {
                // Stays false on purpose: HR has not signed the rubric off, so
                // the UI must keep showing scores as advisory.
                RubricSignedOff = false,
                Candidate = "Ayesha Khan (stub)",
                Payload = new Dictionary<string, System.Text.Json.JsonElement>
                {
                    ["overall_score"] = ToElement(72),
                    ["max_score"] = ToElement(100),
                    ["dimensions"] = ToElement(new[]
                    {
                        new { name = "Skill match", score = 8, max = 10, rationale = "Stubbed rationale." },
                        new { name = "Relevant experience", score = 7, max = 10, rationale = "Stubbed rationale." },
                        new { name = "Domain depth", score = 6, max = 10, rationale = "Stubbed rationale." }
                    }),
                    ["rubric_version"] = ToElement("placeholder-0")
                }
            });
        }

        private static System.Text.Json.JsonElement ToElement(object value) =>
            System.Text.Json.JsonSerializer.SerializeToElement(value);

        /// <summary>
        /// A synthetic profile that deliberately covers the awkward cases: a null
        /// field, an empty collection, a comma-joined technologies string, and a
        /// provenance map where several fields are NOT model-verified.
        /// </summary>
        private static ParseResumeResult BuildCannedResult(string fileName) => new()
        {
            Status = "success",
            Data = new CandidateProfile
            {
                Name = "Ayesha Khan",
                Email = "ayesha.khan@example.test",
                Phone = "+92 300 1234567",
                Location = "Karachi, Pakistan",
                Headline = "Full-Stack Engineering & Data Platforms",
                Summary = "Synthetic profile served by stub mode. Engineer with four years " +
                          "building .NET and Angular line-of-business systems.",
                SpokenLanguages = new List<string> { "English", "Urdu" },
                Links = new List<string>
                {
                    "https://github.com/example-stub",
                    "https://linkedin.com/in/example-stub"
                },
                Skills = new List<string>
                {
                    "C#", ".NET 8", "ASP.NET Core", "Angular", "TypeScript",
                    "SQL Server", "Dapper", "Docker", "Azure DevOps"
                },
                Education = new List<EducationNode>
                {
                    new()
                    {
                        Institution = "NED University of Engineering & Technology",
                        Degree = "BE Software Engineering",
                        Duration = "Sep 2018 - Aug 2022",
                        Gpa = "3.62 / 4.00"
                    },
                    new()
                    {
                        Institution = "Adamjee Government Science College",
                        Degree = "Pre-Engineering",
                        Duration = null,          // legitimately absent
                        Gpa = null
                    }
                },
                Experience = new List<ExperienceNode>
                {
                    new()
                    {
                        Company = "Example Systems",
                        Role = "Software Engineer",
                        Duration = "Jul 2022 - Present",
                        Location = "Karachi (Hybrid)",
                        Achievements = new List<string>
                        {
                            "Cut month-end report generation from 40 minutes to under 3.",
                            "Introduced integration tests around the billing module."
                        }
                    },
                    new()
                    {
                        Company = "Example Labs",
                        Role = "Intern, Backend",
                        Duration = "Feb 2022 - Jun 2022",
                        Location = null,
                        Achievements = new List<string>()   // legitimately empty
                    }
                },
                Projects = new List<ProjectNode>
                {
                    new()
                    {
                        Name = "Warehouse stock reconciliation",
                        Technologies = "ASP.NET Core, SQL Server, Angular",  // comma-joined string, per contract
                        Description = new List<string>
                        {
                            "Reconciled batch-level stock across three warehouses nightly."
                        }
                    }
                },
                CertificationsAndAwards = new List<string>
                {
                    "AZ-204: Developing Solutions for Microsoft Azure",
                    "Runner-up, University Hackathon 2021"
                }
            },
            Meta = new ParseMeta
            {
                SchemaVersion = ProfileSchemaVersions.Supported,
                ExtractionRoute = "text",
                // Mixed on purpose: phone, skills and location must render as
                // "needs review" in the UI, name/email/experience must not.
                FieldProvenance = new Dictionary<string, string>
                {
                    ["name"] = "llm",
                    ["email"] = "llm",
                    ["phone"] = "regex",
                    ["location"] = "llm_unverified",
                    ["headline"] = "llm",
                    ["summary"] = "llm",
                    ["skills"] = "vision_escalation",
                    ["education"] = "llm",
                    ["experience"] = "llm",
                    ["projects"] = "llm",
                    ["certifications_and_awards"] = "llm"
                },
                Stage1DoclingMs = 812.4,
                Stage3OllamaMs = 2_140.7,
                TotalWallMs = 3_106.9,
                PromptTokens = 3_284,
                OutputTokens = 917,
                RetriesUsed = 0,
                DoclingCoverage = 0.97,
                ValidationPassed = true
            }
        };
    }
}

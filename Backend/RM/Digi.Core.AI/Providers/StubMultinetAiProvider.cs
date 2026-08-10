using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Digi.Core.AI.Contracts;
using Digi.Core.AI.Configuration;

namespace Digi.Core.AI.Providers
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
    public sealed class StubMultinetAiProvider : IAIServiceProvider
    {
        private readonly MultinetAiOptions _options;
        private readonly ILogger<StubMultinetAiProvider> _logger;

        public StubMultinetAiProvider(
            IOptions<MultinetAiOptions> options,
            ILogger<StubMultinetAiProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool IsStub => true;

        /// <summary>
        /// Always valid, and advertises the full capability set so the settings
        /// page's feature toggles are all reachable offline. The service name
        /// carries "(STUB)" so nobody mistakes a stubbed green tick for proof
        /// that a real key works.
        /// </summary>
        public Task<AiResult<KeyVerification>> VerifyKeyAsync(
            string? apiKey = null,
            Uri? baseUriOverride = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(AiResult<KeyVerification>.Ok(new KeyVerification
            {
                Valid = true,
                Service = "hrms-ai-service (STUB)",
                ServiceVersion = "1.1.0",
                SchemaVersion = ProfileSchemaVersions.Supported,
                Capabilities = new List<string>
                {
                    MultinetAiEndpoints.Capabilities.ParserExtract,
                    MultinetAiEndpoints.Capabilities.JobRequisitionGenerate,
                    MultinetAiEndpoints.Capabilities.ScreeningScreen,
                    MultinetAiEndpoints.Capabilities.InterviewQuestions,
                    MultinetAiEndpoints.Capabilities.MatchingRank,
                    MultinetAiEndpoints.Capabilities.ScoringScore
                }
            }));

        /// <summary>
        /// A canned requisition draft shaped exactly like the real one — including
        /// every null-by-design field left null. That is the point: if the wizard
        /// is only ever built against a fully-populated response, the empty-state
        /// handling for those fields never gets written, and the gap only shows up
        /// in production where those fields are ALWAYS empty.
        /// </summary>
        public async Task<AiResult<JobRequisitionResult>> GenerateJobRequisitionAsync(
            JobRequisitionRequest request,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null, // ignored — stub mode has no real model to select
            CancellationToken cancellationToken = default)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.JobTitle))
            {
                return AiResult<JobRequisitionResult>.Fail(
                    AiErrorCode.BadRequest,
                    "A job title is required before a job description can be generated.");
            }

            // Generation is the slowest thing the portal does — ~13 s warm, ~35 s
            // cold. A spinner only tested against an instant stub looks broken the
            // first time it meets the real service.
            if (_options.StubLatencySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.StubLatencySeconds), cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger.LogInformation(
                "STUB job requisition generated for '{JobTitle}'. No call left this process and no GPU was used.",
                request.JobTitle);

            return AiResult<JobRequisitionResult>.Ok(new JobRequisitionResult
            {
                Status = "success",
                ExecutionTimeMs = 13_402,
                CompanyId = request.CompanyId,
                ReviewRequired = true,
                Data = new JobRequisitionData
                {
                    BasicInfo = new JobRequisitionBasicInfo
                    {
                        // Verbatim echoes, exactly as the real service does.
                        JobTitle = request.JobTitle,
                        Department = request.Department,
                        Designation = request.Designation,
                        JobSummary =
                            $"We are seeking a {request.JobTitle} to join our team. This summary is " +
                            "produced by stub mode and is not a real generation.",
                        JobCategory = request.JobCategoryOptions?.FirstOrDefault(),
                        Vacancies = 1,
                        EmploymentType = null,   // null by design — HR's decision
                        Grade = null             // null by design — HR's decision
                    },
                    Requirements = new JobRequisitionRequirements
                    {
                        ExperienceYears = new JobRequisitionRange { Minimum = 3, Maximum = 6 },
                        AgeLimits = null,        // never populated — protected attribute
                        KeyResponsibilities = new List<string>
                        {
                            "Design, build and maintain services in the product area.",
                            "Collaborate with QA and product on release readiness.",
                            "Review peers' changes and keep the codebase healthy."
                        },
                        Requirements = new List<string>
                        {
                            "Proven delivery experience in a comparable role.",
                            "Comfortable owning a feature from design through support."
                        },
                        Qualifications = new List<string>
                        {
                            "Bachelor's degree in Computer Science or a related field."
                        },
                        Skills = MultinetAiText.Clean(
                                     request.KeySkills?.Split(',', StringSplitOptions.RemoveEmptyEntries))
                                 ?? new List<string> { "C#", ".NET", "Angular", "SQL Server" }
                    },
                    Compensation = new JobRequisitionCompensation
                    {
                        Location = "Karachi, Pakistan",
                        Benefits = null,         // null by design
                        BudgetType = null,       // null by design
                        BudgetLineId = null      // null by design
                    },
                    Publishing = new JobRequisitionPublishing
                    {
                        Justification = null,    // null by design
                        IsPublicJob = false,     // a human publishes
                        Status = "Draft",        // always
                        ClosingDate = null       // null by design
                    }
                },
                Meta = new JobRequisitionMeta
                {
                    ServiceVersion = "1.1.0 (STUB)",
                    CacheHit = false,
                    ExperienceSource = string.IsNullOrWhiteSpace(request.ExperienceRequired)
                        ? "derived_by_model"
                        : "parsed_from_request",
                    JobCategorySource = request.JobCategoryOptions?.Count > 0
                        ? "selected_from_options"
                        : "generated",
                    WorkMode = "Hybrid"
                }
            });
        }

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
            string? model = null, // ignored — stub mode has no real model to select
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

        public async Task<AiResult<ParseResumeResult>> ExtractResumeByUrlAsync(
            string documentUrl,
            string? candidateId = null,
            string? applicationId = null,
            string? requisitionId = null,
            string? companyId = null,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null, // ignored — stub mode has no real model to select
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(documentUrl))
            {
                return AiResult<ParseResumeResult>.Fail(
                    AiErrorCode.BadRequest, "No document URL was supplied.");
            }

            if (_options.StubLatencySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.StubLatencySeconds), cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger.LogInformation(
                "STUB parse (URL) of {Url} — returning canned ProfileSchema {Version}. " +
                "No call left this process and no GPU was used.",
                documentUrl, ProfileSchemaVersions.Supported);

            return AiResult<ParseResumeResult>.Ok(BuildCannedResult(Path.GetFileName(documentUrl)));
        }

        public async Task<AiResult<ScreenCandidateResult>> ScreenCandidateAsync(
            ScreenCandidateRequest request,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null, // ignored — stub mode has no real model to select
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return AiResult<ScreenCandidateResult>.Fail(
                    AiErrorCode.BadRequest, "Screening request cannot be null.");
            }

            if (_options.StubLatencySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(_options.StubLatencySeconds, 2)), cancellationToken)
                    .ConfigureAwait(false);
            }

            var threshold = request.Threshold > 0 ? request.Threshold : 80;
            var matchScore = 85;
            var isShortlisted = matchScore >= threshold;

            _logger.LogInformation(
                "STUB screening for '{JobTitle}' — returning canned result (Score: {Score}, Shortlisted: {Shortlisted}).",
                request.JobTitle, matchScore, isShortlisted);

            return AiResult<ScreenCandidateResult>.Ok(new ScreenCandidateResult
            {
                Status = "success",
                MatchScore = matchScore,
                Shortlisted = isShortlisted,
                ThresholdUsed = threshold,
                MatchedSkills = new List<string> { "C#", ".NET 8", "ASP.NET Core", "SQL Server", "REST API" },
                MissingSkills = new List<string> { "Kubernetes" },
                Reasons = new List<ScreeningReason>
                {
                    new()
                    {
                        Kind = "match",
                        Detail = "Strong experience in backend .NET software engineering matching position requirements.",
                        Evidence = "5+ years developing ASP.NET Core web services and SQL microservices."
                    },
                    new()
                    {
                        Kind = "gap",
                        Detail = "Lacks direct production experience with Kubernetes orchestration.",
                        Evidence = "Resume highlights Docker containerization but no explicit Kubernetes deployment."
                    }
                },
                ReviewRequired = true,
                Advisory = true,
                ExecutionTimeMs = 1200
            });
        }

        public async Task<AiResult<InterviewQuestionsResult>> GenerateInterviewQuestionsAsync(
            InterviewQuestionsRequest request,
            string? apiKey = null,
            Uri? baseUriOverride = null,
            string? model = null, // ignored — stub mode has no real model to select
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return AiResult<InterviewQuestionsResult>.Fail(
                    AiErrorCode.BadRequest, "Interview questions request cannot be null.");
            }

            if (_options.StubLatencySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(_options.StubLatencySeconds, 2)), cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger.LogInformation(
                "STUB generating interview questions for '{JobTitle}'. No GPU call was made.",
                request.JobTitle);

            var bank = new Dictionary<string, List<InterviewQuestionItem>>
            {
                ["technical"] = new()
                {
                    new()
                    {
                        Question = "How do you manage dependency injection lifetimes (Transient vs Scoped vs Singleton) in ASP.NET Core microservices?",
                        WhatToListFor = "Clear understanding of memory leaks, DbContext scoping issues, and service provider resolution.",
                        GroundedIn = "jd"
                    },
                    new()
                    {
                        Question = "Can you describe how you implement asynchronous streaming and resilience retries with Polly in C#?",
                        WhatToListFor = "Knowledge of CancellationToken propagation, exponential backoff, and circuit breakers.",
                        GroundedIn = "jd"
                    }
                },
                ["behavioral"] = new()
                {
                    new()
                    {
                        Question = "Describe a situation where a production deployment encountered a critical error. How did you diagnose and resolve it under pressure?",
                        WhatToListFor = "Structured troubleshooting, log inspection, root cause isolation, and post-mortem ownership.",
                        GroundedIn = "candidate_profile"
                    }
                },
                ["role_specific"] = new()
                {
                    new()
                    {
                        Question = "Given a high-throughput ERP queue, how would you design database indexing and batch processing to optimize performance?",
                        WhatToListFor = "Understanding execution plans, index fragmentation, locking vs non-locking reads, and batch transaction bounds.",
                        GroundedIn = "jd"
                    }
                }
            };

            return AiResult<InterviewQuestionsResult>.Ok(new InterviewQuestionsResult
            {
                Status = "success",
                QuestionBank = bank,
                ReviewRequired = true,
                Advisory = true,
                ExecutionTimeMs = 1450
            });
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

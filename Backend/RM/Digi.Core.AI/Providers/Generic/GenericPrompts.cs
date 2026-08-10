using System.Text.Json;
using Digi.Core.AI.Contracts;

namespace Digi.Core.AI.Providers.Generic
{
    /// <summary>
    /// Prompt templates shared by every general-purpose chat provider (OpenAI,
    /// Anthropic, Gemini, and OpenAI-compatible "custom" backends). The target
    /// JSON shape is identical across vendors — it is Multinet's own contract, so
    /// the wizard, the review UI and the database mapping do not need to know
    /// which provider answered. Only the HTTP transport differs per vendor.
    /// </summary>
    internal static class GenericPrompts
    {
        private static readonly JsonSerializerOptions Compact = new() { WriteIndented = false };

        public static string JobRequisitionSystemPrompt =>
            "You are drafting a DRAFT job requisition for a human recruiter to review and edit. " +
            "Reply with ONLY a single JSON object — no markdown fences, no commentary before or after. " +
            "Never invent an age range or age limit for the role; that field must always be null. " +
            "Never mark the requisition as published or public; a human always publishes it.";

        public static string BuildJobRequisitionPrompt(JobRequisitionRequest request) => $$"""
            Draft a job requisition for the role below. Return ONLY this JSON shape (fill in values, keep every key):
            {
              "status": "success",
              "review_required": true,
              "data": {
                "step_1_basic_info": {
                  "job_title": "{{Escape(request.JobTitle)}}",
                  "department": {{JsonString(request.Department)}},
                  "designation": {{JsonString(request.Designation)}},
                  "job_summary": "string, 2-4 sentences",
                  "job_category": {{JobCategoryHint(request.JobCategoryOptions)}},
                  "vacancies": 1,
                  "employment_type": null,
                  "grade": null
                },
                "step_2_requirements": {
                  "experience_years": { "minimum": number or null, "maximum": number or null },
                  "age_limits": null,
                  "key_responsibilities": ["string", "..."],
                  "requirements": ["string", "..."],
                  "qualifications": ["string", "..."],
                  "skills": ["string", "..."]
                },
                "step_3_compensation": {
                  "location": "string or null",
                  "benefits": null,
                  "budget_type": null,
                  "budget_line_id": null
                },
                "step_4_publishing": {
                  "justification": null,
                  "is_public_job": false,
                  "status": "Draft",
                  "closing_date": null
                }
              }
            }

            Context supplied by the recruiter (omit anything not given, do not invent a substitute for a blank field):
            - Job title: {{request.JobTitle}}
            - Department: {{request.Department ?? "(not given)"}}
            - Designation: {{request.Designation ?? "(not given)"}}
            - Experience required (use these numbers verbatim if given): {{request.ExperienceRequired ?? "(not given, use your judgement)"}}
            - Key skills mentioned: {{request.KeySkills ?? "(not given)"}}
            - Additional context: {{SerializeOrEmpty(request.AdditionalContext)}}
            """;

        public static string ResumeExtractionSystemPrompt =>
            "You extract structured data from resumes. Reply with ONLY a single JSON object — no markdown " +
            "fences, no commentary. Never fabricate a value that is not present in the resume text; use null " +
            "or an empty array instead. A sparse resume is a valid, normal result.";

        public static string BuildResumeExtractionPrompt(string resumeText) => $$"""
            Extract this resume into ONLY this JSON shape (fill in what is present, use null/[] for what is not):
            {
              "status": "success",
              "data": {
                "name": "string",
                "email": "string or null",
                "phone": "string or null",
                "location": "string or null",
                "headline": "string or null",
                "summary": "string or null",
                "spoken_languages": ["string"],
                "links": ["string"],
                "skills": ["string"],
                "education": [{ "institution": "string", "degree": "string", "duration": "string or null", "gpa": "string or null" }],
                "experience": [{ "company": "string", "role": "string", "duration": "string", "location": "string or null", "achievements": ["string"] }],
                "projects": [{ "name": "string", "technologies": "comma-joined string or null", "description": ["string"] }],
                "certifications_and_awards": ["string"]
              }
            }

            Resume text:
            {{resumeText}}
            """;

        public static string ScreeningSystemPrompt =>
            "You screen a candidate profile against a job's requirements. Reply with ONLY a single JSON " +
            "object — no markdown fences, no commentary. Ground every reason in specific evidence from the " +
            "candidate profile or the job requirements; never invent evidence.";

        public static string BuildScreeningPrompt(ScreenCandidateRequest request) => $$"""
            Score this candidate against the job below. Return ONLY this JSON shape:
            {
              "status": "success",
              "match_score": number 0-100,
              "matched_skills": ["string"],
              "missing_skills": ["string"],
              "reasons": [{ "kind": "match or gap", "detail": "string", "evidence": "string, quote or paraphrase the source" }]
            }

            Job title: {{request.JobTitle}}
            Job requirements: {{string.Join("; ", request.JobRequirements)}}
            Key skills sought: {{string.Join(", ", request.KeySkills)}}
            Experience required: {{request.ExperienceRequired ?? "(not specified)"}}

            Candidate profile (JSON):
            {{SerializeOrEmpty(request.CandidateProfile)}}
            """;

        public static string InterviewQuestionsSystemPrompt =>
            "You write interview questions grounded in a specific job description and, when given one, a " +
            "candidate's background. Reply with ONLY a single JSON object — no markdown fences, no commentary.";

        public static string BuildInterviewQuestionsPrompt(InterviewQuestionsRequest request)
        {
            var categories = request.Categories.Count > 0
                ? request.Categories
                : new List<string> { "technical", "behavioral", "role_specific" };

            var categoryKeys = string.Join(", ", categories.Select(c => $"\"{c}\""));

            return $$"""
                Write {{request.QuestionsPerCategory}} interview questions for each of these categories: {{categoryKeys}}.
                Return ONLY this JSON shape, with one array per category under "question_bank":
                {
                  "status": "success",
                  "question_bank": {
                    {{categoryKeys}}: [{ "question": "string", "what_to_listen_for": "string", "grounded_in": "jd or profile" }]
                  }
                }

                Job title: {{request.JobTitle}}
                Job description: {{request.JobDescription}}
                Key skills: {{string.Join(", ", request.KeySkills)}}
                Candidate profile (JSON, omit if not relevant to a category): {{SerializeOrEmpty(request.CandidateProfile)}}
                """;
        }

        private static string Escape(string value) => value.Replace("\"", "\\\"");

        private static string JsonString(string? value) =>
            value is null ? "null" : $"\"{Escape(value)}\"";

        private static string JobCategoryHint(List<string>? options) =>
            options is { Count: > 0 }
                ? $"one of: {string.Join(", ", options)}"
                : "string";

        private static string SerializeOrEmpty(object? value) =>
            value is null ? "{}" : JsonSerializer.Serialize(value, Compact);
    }
}

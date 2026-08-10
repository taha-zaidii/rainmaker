using Digi.Core.AI.Contracts;

namespace Digi.Core.AI.Providers.Generic
{
    /// <summary>
    /// A general-purpose chat model has no built-in notion that age limits are a
    /// protected attribute or that a job requisition it drafts must stay a Draft.
    /// Multinet's service enforces those rules server-side; a generic provider
    /// has only the prompt asking nicely. This re-asserts the same advisory
    /// invariants client-side, exactly like the mapping already applied to
    /// Multinet's own responses — the model's output is a first draft, never
    /// the final word on what ships.
    /// </summary>
    internal static class GenericResultInvariants
    {
        public static void Enforce(JobRequisitionResult result, JobRequisitionRequest request)
        {
            result.ReviewRequired = true;

            if (result.Data is null)
            {
                return;
            }

            if (result.Data.BasicInfo is not null)
            {
                // Verbatim echoes — never trust the model's paraphrase for these.
                result.Data.BasicInfo.JobTitle = request.JobTitle;
                result.Data.BasicInfo.Department = request.Department;
                result.Data.BasicInfo.Designation = request.Designation;
                result.Data.BasicInfo.Vacancies = 1;
                result.Data.BasicInfo.EmploymentType = null;
                result.Data.BasicInfo.Grade = null;
            }

            if (result.Data.Requirements is not null)
            {
                // Age is a protected attribute. No generic model gets to propose one.
                result.Data.Requirements.AgeLimits = null;
            }

            if (result.Data.Compensation is not null)
            {
                result.Data.Compensation.Benefits = null;
                result.Data.Compensation.BudgetType = null;
                result.Data.Compensation.BudgetLineId = null;
            }

            if (result.Data.Publishing is not null)
            {
                result.Data.Publishing.Justification = null;
                result.Data.Publishing.IsPublicJob = false;
                result.Data.Publishing.Status = "Draft";
                result.Data.Publishing.ClosingDate = null;
            }
        }

        public static void Enforce(ScreenCandidateResult result, int threshold, int executionTimeMs)
        {
            result.ReviewRequired = true;
            result.Advisory = true;
            result.ThresholdUsed = threshold;

            // The model is asked for a score, not for a pass/fail judgement — that
            // way "shortlisted" is always the same >= comparison Multinet's own
            // contract uses, not whatever the model felt like on the day.
            result.Shortlisted = result.MatchScore >= threshold;
            result.ExecutionTimeMs = executionTimeMs;
        }

        public static void Enforce(InterviewQuestionsResult result, int executionTimeMs)
        {
            result.ReviewRequired = true;
            result.Advisory = true;
            result.ExecutionTimeMs = executionTimeMs;
        }
    }
}

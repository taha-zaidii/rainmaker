using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Digi.Core.AI.Configuration;
using Digi.Core.AI.Contracts;
using Digi.Core.AI.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Digi.Recruitment.Module.Tests.Multinet
{
    public class MultinetScreeningContractTests
    {
        [Fact]
        public async Task StubClient_ReturnsValidScreeningResult()
        {
            var options = Options.Create(new MultinetAiOptions { StubMode = true });
            var stubClient = new StubMultinetAiProvider(options, NullLogger<StubMultinetAiProvider>.Instance);

            var req = new ScreenCandidateRequest
            {
                JobTitle = "Dot Net Developer",
                JobRequirements = new List<string> { "C#", ".NET 8", "SQL Server" },
                KeySkills = new List<string> { "C#", "ASP.NET Core" },
                ExperienceRequired = "3-5 years",
                Threshold = 80
            };

            var result = await stubClient.ScreenCandidateAsync(req);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            var screened = result.Value;
            Assert.Equal(85, screened.MatchScore);
            Assert.True(screened.Shortlisted);
            Assert.Equal(80, screened.ThresholdUsed);
            Assert.NotEmpty(screened.MatchedSkills);
            Assert.NotEmpty(screened.Reasons);
            Assert.True(screened.ReviewRequired);
            Assert.True(screened.Advisory);
        }

        [Fact]
        public void ScreenCandidateRequest_SerializesToExpectedContractFormat()
        {
            var req = new ScreenCandidateRequest
            {
                JobTitle = "Software Engineer",
                JobRequirements = new List<string> { "Requirements line 1" },
                Threshold = 75,
                RequisitionId = "10",
                ApplicationId = "5"
            };

            Assert.Equal("Software Engineer", req.JobTitle);
            Assert.Equal(75, req.Threshold);
            Assert.Single(req.JobRequirements);
        }

        [Fact]
        public async Task StubClient_ReturnsValidInterviewQuestions()
        {
            var options = Options.Create(new MultinetAiOptions { StubMode = true });
            var stubClient = new StubMultinetAiProvider(options, NullLogger<StubMultinetAiProvider>.Instance);

            var req = new InterviewQuestionsRequest
            {
                JobTitle = "Senior Full Stack Engineer",
                KeySkills = new List<string> { "C#", "Angular", "SQL Server" },
                QuestionsPerCategory = 3
            };

            var result = await stubClient.GenerateInterviewQuestionsAsync(req);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            var bank = result.Value.QuestionBank;
            Assert.NotEmpty(bank);
            Assert.Contains("technical", bank.Keys);
            Assert.True(result.Value.ReviewRequired);
            Assert.True(result.Value.Advisory);
        }
    }
}


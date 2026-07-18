using CareerCopilot.Models;

namespace CareerCopilot.Services;

/// <summary>
/// Stub implementation — returns hard-coded sample jobs.
/// Replace with a real JobSpy integration or HTTP calls to job board APIs.
/// </summary>
public class JobCollectorStub : IJobCollector
{
    public Task<IEnumerable<Job>> CollectAsync(CancellationToken ct = default)
    {
        var jobs = new List<Job>
        {
            new()
            {
                Title = "Senior .NET Developer",
                Company = "Acme Corp",
                Location = "London (Hybrid)",
                Url = "https://example.com/job/1",
                Source = "LinkedIn",
                Description = "We are looking for a Senior .NET Developer with strong C# and Azure experience. " +
                              "You will design and build REST APIs, work with SQL Server and Azure services, " +
                              "and collaborate with cross-functional teams. Kubernetes experience is a plus."
            },
            new()
            {
                Title = "Backend Engineer",
                Company = "TechStart Ltd",
                Location = "Remote",
                Url = "https://example.com/job/2",
                Source = "Indeed",
                Description = "Backend Engineer role in a fast-growing fintech. " +
                              "Must have Python or Java; .NET knowledge helpful. " +
                              "Experience with Docker and microservices required."
            },
            new()
            {
                Title = "Cloud Architect",
                Company = "BigBank PLC",
                Location = "Manchester",
                Url = "https://example.com/job/3",
                Source = "Manual",
                Description = "Cloud Architect to lead AWS migration. " +
                              "Requires AWS Solutions Architect certification. " +
                              "Active security clearance required."
            }
        };

        return Task.FromResult<IEnumerable<Job>>(jobs);
    }
}

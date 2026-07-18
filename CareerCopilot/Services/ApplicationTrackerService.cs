using CareerCopilot.Data;
using CareerCopilot.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Services;

public class ApplicationTrackerService : IApplicationTracker
{
    private readonly AppDbContext _db;

    public ApplicationTrackerService(AppDbContext db) => _db = db;

    public async Task<ApplicationRecord> UpsertAsync(int jobId, ApplicationStatus status, string? notes = null, CancellationToken ct = default)
    {
        var record = await _db.ApplicationRecords.FirstOrDefaultAsync(r => r.JobId == jobId, ct);

        if (record is null)
        {
            record = new ApplicationRecord { JobId = jobId };
            _db.ApplicationRecords.Add(record);
        }

        record.Status    = status;
        record.UpdatedAt = DateTime.UtcNow;

        if (notes is not null)
            record.Notes = notes;

        if (status == ApplicationStatus.Applied && record.AppliedAt is null)
            record.AppliedAt = DateTime.UtcNow;

        if (status is ApplicationStatus.Interview or ApplicationStatus.PhoneScreen && record.InterviewAt is null)
            record.InterviewAt = DateTime.UtcNow;

        if (status is ApplicationStatus.Offer or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn)
        {
            record.DecisionAt  = DateTime.UtcNow;
            record.GotOffer    = status == ApplicationStatus.Offer;
            record.GotInterview = record.GotInterview ?? (record.InterviewAt is not null);
        }

        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task<IEnumerable<ApplicationRecord>> GetAllAsync(CancellationToken ct = default) =>
        await _db.ApplicationRecords
                 .Include(r => r.Job)
                 .OrderByDescending(r => r.UpdatedAt)
                 .ToListAsync(ct);

    public async Task<ApplicationRecord?> GetByJobAsync(int jobId, CancellationToken ct = default) =>
        await _db.ApplicationRecords
                 .Include(r => r.Job)
                 .FirstOrDefaultAsync(r => r.JobId == jobId, ct);
}

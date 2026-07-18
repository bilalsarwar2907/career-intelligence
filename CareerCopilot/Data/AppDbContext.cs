using CareerCopilot.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<FitAnalysis> FitAnalyses => Set<FitAnalysis>();
    public DbSet<ApplicationRecord> ApplicationRecords => Set<ApplicationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>()
            .HasOne(j => j.FitAnalysis)
            .WithOne(f => f.Job)
            .HasForeignKey<FitAnalysis>(f => f.JobId);

        modelBuilder.Entity<Job>()
            .HasOne(j => j.ApplicationRecord)
            .WithOne(a => a.Job)
            .HasForeignKey<ApplicationRecord>(a => a.JobId);
    }
}

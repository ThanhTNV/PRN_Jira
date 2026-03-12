using Microsoft.EntityFrameworkCore;
using PRN_Jira.Models;

namespace PRN_Jira.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<SrsDocument> SrsDocuments => Set<SrsDocument>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Username).IsRequired().HasMaxLength(100);
            e.Property(a => a.PasswordHash).IsRequired();
            e.Property(a => a.JiraAccessToken).IsRequired();
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.JiraProjectId).IsRequired();
            e.Property(p => p.JiraBaseUrl).IsRequired();
            e.Property(p => p.JiraEmail).IsRequired();
            e.HasIndex(p => new { p.AccountId, p.JiraProjectId }).IsUnique();

            e.HasOne(p => p.Account)
                .WithMany(a => a.Projects)
                .HasForeignKey(p => p.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SrsDocument>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Description).HasMaxLength(500);
            e.Property(d => d.SnapshotJson).IsRequired();

            e.HasOne(d => d.Account)
             .WithMany(a => a.SrsDocuments)
             .HasForeignKey(d => d.AccountId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(d => d.Project)
             .WithMany(p => p.SrsDocuments)
             .HasForeignKey(d => d.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(d => new { d.ProjectId, d.VersionNumber }).IsUnique();
        });
    }
}

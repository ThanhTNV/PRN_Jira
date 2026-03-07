using Microsoft.EntityFrameworkCore;
using PRN_Jira.Models;

namespace PRN_Jira.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<SrsDocument> SrsDocuments => Set<SrsDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.Email).IsUnique();
            e.HasIndex(a => a.JiraProjectId).IsUnique();
            e.Property(a => a.Username).IsRequired().HasMaxLength(100);
            e.Property(a => a.Email).IsRequired().HasMaxLength(200);
            e.Property(a => a.PasswordHash).IsRequired();
            e.Property(a => a.JiraProjectId).IsRequired();
            e.Property(a => a.JiraAccessToken).IsRequired();
            e.Property(a => a.JiraBaseUrl).IsRequired();
            e.Property(a => a.JiraEmail).IsRequired();
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
            e.HasIndex(d => new { d.AccountId, d.VersionNumber }).IsUnique();
        });
    }
}

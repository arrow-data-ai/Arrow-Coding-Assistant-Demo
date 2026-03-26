using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Chronocode.Data.Models;

namespace Chronocode.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<ChargeCode> ChargeCodes { get; set; }
    public DbSet<ActivityChargeCode> ActivityChargeCodes { get; set; }
    public DbSet<ProjectEngineer> ProjectEngineers { get; set; }
    public DbSet<ProjectManager> ProjectManagers { get; set; }
    public DbSet<WorkAuthorizationArtifact> WorkAuthorizationArtifacts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Project relationships
        builder.Entity<Project>()
            .HasOne(p => p.Owner)
            .WithMany(u => u.OwnedProjects)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure ProjectTask relationships
        builder.Entity<ProjectTask>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Activity relationships
        builder.Entity<Activity>()
            .HasOne(a => a.Task)
            .WithMany(t => t.Activities)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Activity>()
            .HasOne(a => a.User)
            .WithMany(u => u.Activities)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure ChargeCode relationships
        builder.Entity<ChargeCode>()
            .HasOne(cc => cc.Project)
            .WithMany(p => p.ChargeCodes)
            .HasForeignKey(cc => cc.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure ActivityChargeCode relationships
        builder.Entity<ActivityChargeCode>()
            .HasOne(acc => acc.Activity)
            .WithMany(a => a.ActivityChargeCodes)
            .HasForeignKey(acc => acc.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ActivityChargeCode>()
            .HasOne(acc => acc.ChargeCode)
            .WithMany(cc => cc.ActivityChargeCodes)
            .HasForeignKey(acc => acc.ChargeCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure ProjectEngineer relationships
        builder.Entity<ProjectEngineer>()
            .HasOne(pe => pe.Project)
            .WithMany(p => p.Engineers)
            .HasForeignKey(pe => pe.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectEngineer>()
            .HasOne(pe => pe.User)
            .WithMany(u => u.EngineerAssignments)
            .HasForeignKey(pe => pe.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure ProjectManager relationships
        builder.Entity<ProjectManager>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.Managers)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectManager>()
            .HasOne(pm => pm.User)
            .WithMany(u => u.ManagerAssignments)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Add unique constraints
        builder.Entity<ChargeCode>()
            .HasIndex(cc => new { cc.Code, cc.ProjectId })
            .IsUnique();

        builder.Entity<ProjectEngineer>()
            .HasIndex(pe => new { pe.ProjectId, pe.UserId })
            .IsUnique();

        builder.Entity<ProjectManager>()
            .HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique();

        // Configure WorkAuthorizationArtifact relationships
        builder.Entity<WorkAuthorizationArtifact>()
            .HasOne(waa => waa.ChargeCode)
            .WithMany(cc => cc.WorkAuthorizationArtifacts)
            .HasForeignKey(waa => waa.ChargeCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkAuthorizationArtifact>()
            .HasOne(waa => waa.UploadedByUser)
            .WithMany(u => u.UploadedArtifacts)
            .HasForeignKey(waa => waa.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

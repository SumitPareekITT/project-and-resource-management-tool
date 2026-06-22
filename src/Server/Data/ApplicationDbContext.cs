using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
    public DbSet<ActivityTag> ActivityTags => Set<ActivityTag>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<TimesheetNotificationLog> TimesheetNotificationLogs => Set<TimesheetNotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureUserProfiles(modelBuilder);
        ConfigureRbac(modelBuilder);
        ConfigureSkills(modelBuilder);
        ConfigureProjects(modelBuilder);
        ConfigureAllocations(modelBuilder);
        ConfigureTimesheets(modelBuilder);
        ConfigureSystemConfiguration(modelBuilder);
        ConfigureTimesheetNotifications(modelBuilder);
        Seed(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Username).IsUnique();
            entity.Property(user => user.Username).HasMaxLength(80).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        });
    }

    private static void ConfigureUserProfiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(profile => profile.UserId).IsUnique();
            entity.HasIndex(profile => profile.Email).IsUnique();
            entity.Property(profile => profile.FullName).HasMaxLength(150).IsRequired();
            entity.Property(profile => profile.Email).HasMaxLength(200).IsRequired();
            entity.Property(profile => profile.Department).HasMaxLength(100).IsRequired();
            entity.Property(profile => profile.Designation).HasMaxLength(100).IsRequired();
            entity.Property(profile => profile.ResourceStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(profile => profile.CurrentUtilizationPercent).HasPrecision(5, 2);

            entity.HasOne(profile => profile.User)
                .WithOne(user => user.Profile)
                .HasForeignKey<UserProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(profile => profile.ManagerUser)
                .WithMany()
                .HasForeignKey(profile => profile.ManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRbac(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(role => role.RoleName).IsUnique();
            entity.Property(role => role.RoleName).HasMaxLength(50).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(200);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(permission => permission.PermissionCode).IsUnique();
            entity.Property(permission => permission.PermissionCode).HasMaxLength(100).IsRequired();
            entity.Property(permission => permission.Description).HasMaxLength(250);
            entity.Property(permission => permission.HttpMethod).HasMaxLength(10);
            entity.Property(permission => permission.RoutePattern).HasMaxLength(200);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });
        });

        modelBuilder.Entity<UserRoleAssignment>(entity =>
        {
            entity.HasKey(assignment => new { assignment.UserId, assignment.RoleId });
        });
    }

    private static void ConfigureSkills(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasIndex(skill => skill.Name).IsUnique();
            entity.Property(skill => skill.Name).HasMaxLength(100).IsRequired();
            entity.Property(skill => skill.Category).HasConversion<string>().HasMaxLength(40);
        });

        modelBuilder.Entity<UserSkill>(entity =>
        {
            entity.HasKey(userSkill => new { userSkill.UserId, userSkill.SkillId });
            entity.Property(userSkill => userSkill.ProficiencyLevel).HasConversion<string>().HasMaxLength(40);
            entity.Property(userSkill => userSkill.YearsOfExperience).HasPrecision(4, 1);
        });

        modelBuilder.Entity<ActivityTag>(entity =>
        {
            entity.HasIndex(tag => tag.Name).IsUnique();
            entity.Property(tag => tag.Name).HasMaxLength(120).IsRequired();
            entity.Property(tag => tag.Category).HasConversion<string>().HasMaxLength(40);
        });
    }

    private static void ConfigureProjects(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(project => project.Name).HasMaxLength(150).IsRequired();
            entity.Property(project => project.ClientName).HasMaxLength(150);
            entity.Property(project => project.Description).HasMaxLength(1000);
            entity.Property(project => project.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(project => project.HealthStatus).HasConversion<string>().HasMaxLength(40);

            entity.HasOne(project => project.ManagerUser)
                .WithMany(user => user.ManagedProjects)
                .HasForeignKey(project => project.ManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Milestone>(entity =>
        {
            entity.Property(milestone => milestone.Title).HasMaxLength(150).IsRequired();
            entity.Property(milestone => milestone.Description).HasMaxLength(1000);
            entity.Property(milestone => milestone.Status).HasConversion<string>().HasMaxLength(40);
        });
    }

    private static void ConfigureAllocations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Allocation>(entity =>
        {
            entity.Property(allocation => allocation.UtilizationPercentage).HasPrecision(5, 2);
            entity.Property(allocation => allocation.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(allocation => new { allocation.UserId, allocation.FromDate, allocation.ToDate });

            entity.HasOne(allocation => allocation.User)
                .WithMany(user => user.Allocations)
                .HasForeignKey(allocation => allocation.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(allocation => allocation.CreatedByUser)
                .WithMany(user => user.CreatedAllocations)
                .HasForeignKey(allocation => allocation.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTimesheets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Timesheet>(entity =>
        {
            entity.HasIndex(timesheet => new { timesheet.UserId, timesheet.WeekStartDate }).IsUnique();
            entity.Property(timesheet => timesheet.TotalHours).HasPrecision(5, 2);
            entity.Property(timesheet => timesheet.Status).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(timesheet => timesheet.User)
                .WithMany(user => user.Timesheets)
                .HasForeignKey(timesheet => timesheet.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TimesheetEntry>(entity =>
        {
            entity.Property(entry => entry.HoursWorked).HasPrecision(5, 2);
            entity.Property(entry => entry.Notes).HasMaxLength(1000);
            entity.HasMany(entry => entry.ActivityTags)
                .WithMany(tag => tag.TimesheetEntries)
                .UsingEntity("TimesheetEntryActivityTags");
        });
    }

    private static void ConfigureSystemConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemConfiguration>(entity =>
        {
            entity.HasIndex(configuration => configuration.Key).IsUnique();
            entity.Property(configuration => configuration.Key).HasMaxLength(100).IsRequired();
            entity.Property(configuration => configuration.Value).HasMaxLength(1000);
            entity.Property(configuration => configuration.Description).HasMaxLength(500);
        });
    }

    private static void ConfigureTimesheetNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimesheetNotificationLog>(entity =>
        {
            entity.Property(log => log.RecipientEmail).HasMaxLength(200).IsRequired();
            entity.Property(log => log.RecipientRole).HasMaxLength(50).IsRequired();
            entity.Property(log => log.Subject).HasMaxLength(250).IsRequired();
            entity.Property(log => log.Body).HasMaxLength(4000).IsRequired();
            entity.Property(log => log.NotificationType).HasConversion<string>().HasMaxLength(30);
        });
    }

    private static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(SeedData.Users);
        modelBuilder.Entity<UserProfile>().HasData(RbacSeedData.UserProfiles);
        modelBuilder.Entity<Role>().HasData(RbacSeedData.Roles);
        modelBuilder.Entity<Permission>().HasData(RbacSeedData.Permissions);
        modelBuilder.Entity<RolePermission>().HasData(RbacSeedData.RolePermissions);
        modelBuilder.Entity<UserRoleAssignment>().HasData(RbacSeedData.UserRoleAssignments);
        modelBuilder.Entity<ActivityTag>().HasData(SeedData.ActivityTags);
        modelBuilder.Entity<Skill>().HasData(SeedData.Skills);
        modelBuilder.Entity<SystemConfiguration>().HasData(SeedData.SystemConfigurations);
    }
}

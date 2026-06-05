using Microsoft.EntityFrameworkCore;
using ProjectResourceManagement.Server.Models;

namespace ProjectResourceManagement.Server.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
    public DbSet<ActivityTag> ActivityTags => Set<ActivityTag>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureEmployees(modelBuilder);
        ConfigureSkills(modelBuilder);
        ConfigureProjects(modelBuilder);
        ConfigureAllocations(modelBuilder);
        ConfigureTimesheets(modelBuilder);
        ConfigureSystemConfiguration(modelBuilder);
        Seed(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(200).IsRequired();
            entity.Property(user => user.Username).HasMaxLength(80).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(30);
        });
    }

    private static void ConfigureEmployees(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(employee => employee.UserId).IsUnique();
            entity.Property(employee => employee.FullName).HasMaxLength(150).IsRequired();
            entity.Property(employee => employee.Email).HasMaxLength(200).IsRequired();
            entity.Property(employee => employee.Department).HasMaxLength(100).IsRequired();
            entity.Property(employee => employee.Designation).HasMaxLength(100).IsRequired();
            entity.Property(employee => employee.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(employee => employee.CurrentUtilizationPercent).HasPrecision(5, 2);

            entity.HasOne(employee => employee.User)
                .WithOne(user => user.EmployeeProfile)
                .HasForeignKey<Employee>(employee => employee.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(employee => employee.Manager)
                .WithMany(user => user.ManagedEmployees)
                .HasForeignKey(employee => employee.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<EmployeeSkill>(entity =>
        {
            entity.HasKey(employeeSkill => new { employeeSkill.EmployeeId, employeeSkill.SkillId });
            entity.Property(employeeSkill => employeeSkill.ProficiencyLevel).HasConversion<string>().HasMaxLength(40);
            entity.Property(employeeSkill => employeeSkill.YearsOfExperience).HasPrecision(4, 1);
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

            entity.HasOne(project => project.Manager)
                .WithMany(user => user.ManagedProjects)
                .HasForeignKey(project => project.ManagerId)
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
            entity.HasIndex(allocation => new { allocation.EmployeeId, allocation.FromDate, allocation.ToDate });

            entity.HasOne(allocation => allocation.CreatedByManager)
                .WithMany()
                .HasForeignKey(allocation => allocation.CreatedByManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTimesheets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Timesheet>(entity =>
        {
            entity.HasIndex(timesheet => new { timesheet.EmployeeId, timesheet.WeekStartDate }).IsUnique();
            entity.Property(timesheet => timesheet.TotalHours).HasPrecision(5, 2);
            entity.Property(timesheet => timesheet.Status).HasConversion<string>().HasMaxLength(30);
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

    private static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(SeedData.Users);
        modelBuilder.Entity<ActivityTag>().HasData(SeedData.ActivityTags);
        modelBuilder.Entity<SystemConfiguration>().HasData(SeedData.SystemConfigurations);
    }
}

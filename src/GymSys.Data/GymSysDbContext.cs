using GymSys.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymSys.Data;

public class GymSysDbContext(DbContextOptions<GymSysDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<PTAssignment> PTAssignments => Set<PTAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GymSysDbContext).Assembly);
        SeedData.Apply(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
    }
}

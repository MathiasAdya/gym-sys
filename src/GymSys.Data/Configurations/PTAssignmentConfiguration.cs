using GymSys.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSys.Data.Configurations;

public class PTAssignmentConfiguration : IEntityTypeConfiguration<PTAssignment>
{
    public void Configure(EntityTypeBuilder<PTAssignment> b)
    {
        b.ToTable("PTAssignments");
        b.HasKey(x => x.AssignmentId);
        // Satu member maksimal satu PT aktif (FR-10).
        b.HasIndex(x => x.MemberId).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_PT_MemberAktif");
        b.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);

        b.HasOne(x => x.Member).WithMany(m => m.PTAssignments).HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Trainer).WithMany(t => t.Assignments).HasForeignKey(x => x.TrainerId).OnDelete(DeleteBehavior.Restrict);
    }
}

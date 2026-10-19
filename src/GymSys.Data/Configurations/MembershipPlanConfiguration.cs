using GymSys.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSys.Data.Configurations;

public class MembershipPlanConfiguration : IEntityTypeConfiguration<MembershipPlan>
{
    public void Configure(EntityTypeBuilder<MembershipPlan> b)
    {
        b.ToTable("MembershipPlans", t =>
        {
            t.HasCheckConstraint("CK_Plans_DurasiHari", "[DurasiHari] > 0");
            t.HasCheckConstraint("CK_Plans_Harga", "[Harga] >= 0");
        });
        b.HasKey(x => x.PlanId);
        b.HasIndex(x => x.NamaPaket).IsUnique();
        b.Property(x => x.NamaPaket).HasMaxLength(50);
        b.Property(x => x.Deskripsi).HasMaxLength(255);
        b.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
    }
}

using ClinicPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicPOS.Infrastructure.Persistence.Configurations;

public class UserBranchConfiguration : IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.ToTable("user_branches");
        builder.HasKey(ub => new { ub.UserId, ub.BranchId });

        builder.HasOne(ub => ub.User).WithMany(u => u.UserBranches)
            .HasForeignKey(ub => ub.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ub => ub.Branch).WithMany()
            .HasForeignKey(ub => ub.BranchId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ub => ub.BranchId).HasDatabaseName("ix_user_branches_branch_id");
    }
}

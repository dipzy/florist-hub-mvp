using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class OrderChangeProposalConfiguration : IEntityTypeConfiguration<OrderChangeProposal>
{
    public void Configure(EntityTypeBuilder<OrderChangeProposal> builder)
    {
        builder.Property(x => x.RevisedTotal).HasPrecision(18, 2);
    }
}

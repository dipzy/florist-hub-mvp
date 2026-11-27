using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class PortfolioArrangementItemConfiguration : IEntityTypeConfiguration<PortfolioArrangementItem>
{
    public void Configure(EntityTypeBuilder<PortfolioArrangementItem> builder) { }
}

using FloristHub.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloristHub.DAL.Configurations;

public class PortfolioArrangementPhotoConfiguration : IEntityTypeConfiguration<PortfolioArrangementPhoto>
{
    public void Configure(EntityTypeBuilder<PortfolioArrangementPhoto> builder) { }
}

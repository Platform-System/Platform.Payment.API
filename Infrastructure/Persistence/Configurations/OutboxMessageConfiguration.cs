using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Payment.API.Infrastructure.Persistence.Models;

namespace Platform.Payment.API.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageModel>
{
    public void Configure(EntityTypeBuilder<OutboxMessageModel> builder)
    {
        builder.Property(x => x.MessageType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.Error)
            .HasMaxLength(4000);

        builder.HasIndex(x => new { x.ProcessedAt, x.CreatedAt });
    }
}

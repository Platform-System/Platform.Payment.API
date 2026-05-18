using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Payment.API.Infrastructure.Persistence.Models;

namespace Platform.Payment.API.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransactionModel>
{
    public void Configure(EntityTypeBuilder<PaymentTransactionModel> builder)
    {
        builder.Property(x => x.ReferenceType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.PaymentLinkId)
            .HasMaxLength(255);

        builder.Property(x => x.CheckoutUrl)
            .HasMaxLength(2048);

        builder.Property(x => x.Currency)
            .HasMaxLength(16);

        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId, x.Status });
        builder.HasIndex(x => new { x.Provider, x.ReferenceCode, x.Status });
        builder.HasIndex(x => x.PaymentLinkId)
            .HasFilter("\"PaymentLinkId\" IS NOT NULL");
    }
}

using Microsoft.EntityFrameworkCore;
using Platform.BuildingBlocks.Abstractions;
using Platform.Infrastructure.Data;
using Platform.Payment.API.Infrastructure.Persistence.Models;

namespace Platform.Payment.API.Infrastructure.Data;

public sealed class PaymentDbContext : BaseDbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options, ICurrentUserProvider? currentUserProvider = null)
        : base(options, currentUserProvider)
    {
    }

    public DbSet<PaymentTransactionModel> Payments { get; set; }
    public DbSet<OutboxMessageModel> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

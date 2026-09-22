using Light.Exceptions;
using StarterKit.Currencies.Api.Domain.Currencies;
using StarterKit.Currencies.Api.Domain.ExchangeRates;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Currencies.Api.Data;

public class CurrencyDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    DbContextOptions<CurrencyDbContext> options) :
    BaseDbContext(options)
{
    public const string Schema = "currency";

    public virtual DbSet<Currency> Currencies => Set<Currency>();

    public virtual DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    public override int SaveChanges()
    {
        EnsureExchangeRatesAreAppendOnly();
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnsureExchangeRatesAreAppendOnly();
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<Currency>(entity =>
        {
            entity.ToTable(name: "Currencies");

            // At most one base currency, enforced by the database as well as by the domain.
            entity.HasIndex(x => x.IsBase)
                .IsUnique()
                .HasProviderFilter(
                    Database,
                    "[IsBase] = 1",
                    "\"IsBase\" = true");

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.Id).HasMaxLength(CurrencyLimits.CodeLength);

            entity.Property(x => x.Name).HasMaxLength(CurrencyLimits.NameMaxLength);

            entity.Property(x => x.Symbol).HasMaxLength(CurrencyLimits.SymbolMaxLength);
        });

        builder.Entity<ExchangeRate>(entity =>
        {
            entity.ToTable(name: "ExchangeRates");

            // Also serves the "rate effective at time T" lookup (equality on code, range/order on date).
            entity.HasIndex(x => new { x.CurrencyCode, x.EffectiveFrom }).IsUnique();

            entity.ConfigureAuditableEntity<ExchangeRate, long>();

            entity.Property(x => x.CurrencyCode).HasMaxLength(CurrencyLimits.CodeLength);

            entity.Property(x => x.Rate).HasPrecision(CurrencyLimits.RatePrecision, CurrencyLimits.RateScale);

            entity.Property(x => x.RecordedBy).HasMaxLength(450);

            entity.Property(x => x.Note).HasMaxLength(CurrencyLimits.NoteMaxLength);

            entity.HasOne<Currency>()
                .WithMany()
                .HasForeignKey(x => x.CurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>Backstop for the append-only rule: a recorded rate can be neither changed nor removed.</summary>
    private void EnsureExchangeRatesAreAppendOnly()
    {
        ChangeTracker.DetectChanges();

        var illegal = ChangeTracker.Entries<ExchangeRate>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);

        if (illegal)
            throw new ConflictException("Exchange rates are append-only; record a newer rate to correct one.");
    }
}

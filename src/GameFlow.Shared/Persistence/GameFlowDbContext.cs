using GameFlow.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Shared.Persistence;

public sealed class GameFlowDbContext(DbContextOptions<GameFlowDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionEvent> TransactionEvents => Set<TransactionEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FailedMessage> FailedMessages => Set<FailedMessage>();
    public DbSet<CacheEntry> Cache => Set<CacheEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ExternalPlayerId).IsUnique();
            entity.Property(x => x.ExternalPlayerId).HasMaxLength(64);
            entity.Property(x => x.Username).HasMaxLength(120);
            entity.Property(x => x.Country).HasMaxLength(8);
            entity.Property(x => x.Currency).HasMaxLength(8);
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ExternalGameId).IsUnique();
            entity.Property(x => x.ExternalGameId).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Provider).HasMaxLength(120);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ExternalTransactionId).IsUnique();
            entity.HasIndex(x => x.CorrelationId).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.ExternalTransactionId).HasMaxLength(64);
            entity.Property(x => x.CorrelationId).HasMaxLength(64);
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.FailureReason).HasMaxLength(512);
            entity.HasOne(x => x.Player)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.PlayerId);
            entity.HasOne(x => x.Game)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.GameId);
        });

        modelBuilder.Entity<TransactionEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.TransactionId);
            entity.Property(x => x.EventType).HasMaxLength(80);
            entity.Property(x => x.Message).HasMaxLength(256);
            entity.HasOne(x => x.Transaction)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.TransactionId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EntityType);
            entity.Property(x => x.Action).HasMaxLength(80);
            entity.Property(x => x.Actor).HasMaxLength(80);
            entity.Property(x => x.EntityType).HasMaxLength(80);
            entity.Property(x => x.EntityId).HasMaxLength(64);
        });

        modelBuilder.Entity<FailedMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MessageType).HasMaxLength(120);
            entity.Property(x => x.Reason).HasMaxLength(512);
        });

        modelBuilder.Entity<CacheEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CacheKey).IsUnique();
            entity.Property(x => x.CacheKey).HasMaxLength(160);
        });
    }
}

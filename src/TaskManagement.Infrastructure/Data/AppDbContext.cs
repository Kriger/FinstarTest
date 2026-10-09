using Microsoft.EntityFrameworkCore;
using DomainTask = TaskManagement.Domain.Models.Task;
using TaskManagement.Domain.Outbox;
using TaskManagement.Infrastructure.Idempotency;

namespace TaskManagement.Infrastructure.Data;

/// <summary>
/// Контекст базы данных для приложения управления задачами.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Набор задач.
    /// </summary>
    public DbSet<DomainTask> Tasks => Set<DomainTask>();

    /// <summary>
    /// Набор сообщений Outbox.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Набор записей идемпотентности.
    /// </summary>
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="AppDbContext"/>.
    /// </summary>
    /// <param name="options">Опции контекста.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DomainTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.CreatedBy)
                .IsRequired();

            entity.Property(e => e.UpdatedBy)
                .IsRequired();

            entity.Property(e => e.RowVersion)
                .IsRowVersion();

            entity.HasQueryFilter(t => !t.IsDeleted);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.SentAt);

            entity.Property(e => e.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(e => e.Key);

            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.HttpMethod)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.RequestBody)
                .IsRequired();

            entity.Property(e => e.ResponseBody);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}

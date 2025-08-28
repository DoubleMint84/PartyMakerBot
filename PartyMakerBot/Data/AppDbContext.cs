using Microsoft.EntityFrameworkCore;
using PartyMakerBot.Model;

namespace PartyMakerBot.Data;

public class AppDbContext : DbContext
{
    private readonly string _connectionString;

    public AppDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<QueueItem> QueueItems { get; set; } = null!;
    public DbSet<DownloadedUrl> DownloadedUrls { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Уникальный индекс для DownloadedUrl.Url — удобно для поиска существующих загрузок
        modelBuilder.Entity<DownloadedUrl>()
            .HasIndex(d => d.Url)
            .IsUnique();

        modelBuilder.Entity<DownloadedUrl>()
            .HasMany(d => d.QueueItems)
            .WithOne(q => q.DownloadedUrl)
            .HasForeignKey(q => q.DownloadedUrlId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
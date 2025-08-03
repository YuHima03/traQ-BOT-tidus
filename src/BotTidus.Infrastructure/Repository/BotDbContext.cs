using Microsoft.EntityFrameworkCore;

namespace BotTidus.Infrastructure.Repository;

public partial class BotDbContext(DbContextOptions<BotDbContext> options) : DbContext(options)
{
    public virtual DbSet<DiscordWebhook> DiscordWebhooks { get; set; }

    public virtual DbSet<MessageFaceScore> MessageFaceScores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_uca1400_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<DiscordWebhook>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("current_timestamp()");
            entity.Property(e => e.PostUrl).HasComment("Webhook URL");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp()");
        });

        modelBuilder.Entity<MessageFaceScore>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PRIMARY");

            entity.Property(e => e.MessageId).HasComment("message uuid");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("current_timestamp()");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp()");
            entity.Property(e => e.UserId).HasComment("message author user uuid");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

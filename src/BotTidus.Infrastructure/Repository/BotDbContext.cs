using BotTidus.Domain;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.Infrastructure.Repository;

public partial class BotDbContext(DbContextOptions<BotDbContext> options) : DbContext(options), IRepository
{
    public virtual DbSet<DiscordWebhook_Repo> DiscordWebhooks { get; set; }

    public virtual DbSet<MessageFaceScore_Repo> MessageFaceScores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_uca1400_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<DiscordWebhook_Repo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("current_timestamp()");
            entity.Property(e => e.PostUrl).HasComment("Webhook URL");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp()");
        });

        modelBuilder.Entity<MessageFaceScore_Repo>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PRIMARY");

            entity.Property(e => e.MessageId).HasComment("message uuid");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("current_timestamp()");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("current_timestamp()");
            entity.Property(e => e.UserId).HasComment("message author user uuid");
        });

        modelBuilder.Entity<Domain.MessageFaceScores.UserFaceCount>(builder =>
        {
            builder.HasNoKey();
            builder.Property(e => e.UserId);
            builder.Property(e => e.NegativePhraseCount);
            builder.Property(e => e.NegativeReactionCount);
            builder.Property(e => e.PositivePhraseCount);
            builder.Property(e => e.PositiveReactionCount);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotTidus.RepositoryImpl.Models
{
    [Table("discord_webhooks")]
    internal class DiscordWebhook
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("post_url")]
        public string? PostUrl { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("is_enabled")]
        public bool IsEnabled { get; set; }

        [Column("notifies_on_flags")]
        public int NotifiesOnFlags { get; set; }

        [Column("created_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime UpdatedAt { get; set; }

        public Domain.DiscordWebhook.DiscordWebhook ToDomainObject()
        {
            return new Domain.DiscordWebhook.DiscordWebhook(
                Id,
                Uri.TryCreate(PostUrl, UriKind.Absolute, out var uri) ? uri : null,
                UserId,
                IsEnabled,
                (Domain.DiscordWebhook.MessageFilter)NotifiesOnFlags,
                CreatedAt,
                UpdatedAt
            );
        }
    }
}

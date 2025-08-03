using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.Infrastructure.Repository;

[Table("discord_webhooks")]
[Index("IsEnabled", Name = "is_enabled")]
[Index("UserId", Name = "user_id")]
public partial class DiscordWebhook
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Webhook URL
    /// </summary>
    [Column("post_url")]
    [StringLength(255)]
    public string? PostUrl { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("is_enabled")]
    public bool IsEnabled { get; set; }

    [Column("notifies_on_flags", TypeName = "int(11)")]
    public int NotifiesOnFlags { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }
}

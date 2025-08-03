using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BotTidus.Infrastructure.Repository;

[Table("message_face_scores")]
[Index("UserId", Name = "idx_user")]
public partial class MessageFaceScore_Repo
{
    /// <summary>
    /// message uuid
    /// </summary>
    [Key]
    [Column("message_id")]
    public Guid MessageId { get; set; }

    /// <summary>
    /// message author user uuid
    /// </summary>
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("positive_phrase_count", TypeName = "int(10) unsigned")]
    public uint PositivePhraseCount { get; set; }

    [Column("negative_phrase_count", TypeName = "int(10) unsigned")]
    public uint NegativePhraseCount { get; set; }

    [Column("positive_reaction_count", TypeName = "int(10) unsigned")]
    public uint PositiveReactionCount { get; set; }

    [Column("negative_reaction_count", TypeName = "int(10) unsigned")]
    public uint NegativeReactionCount { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }
}

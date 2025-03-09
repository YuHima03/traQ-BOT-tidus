﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotTidus.RepositoryImpl.Models
{
    [Table("message_face_scores")]
    sealed class MessageFaceScore
    {
        [Column("user_id")]
        [Key]
        public Guid UserId { get; set; }

        [Column("message_id")]
        public Guid MessageId { get; set; }

        [Column("positive_phrase_count")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint PositivePhraseCount { get; set; }

        [Column("negative_phrase_count")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint NegativePhraseCount { get; set; }

        [Column("positive_reaction_count")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint PositiveReactionCount { get; set; }

        [Column("negative_reaction_count")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint NegativeReactionCount { get; set; }

        [Column("created_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime UpdatedAt { get; set; }
    }
}

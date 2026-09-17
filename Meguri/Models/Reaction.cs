using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("Reactions")]
    [Index(nameof(UserId))]
    [Index(nameof(PostId))]
    [Index(nameof(CommentId))]
    [Index(nameof(ImageId))]
    [Index(nameof(MessageId))]
    [Index(nameof(Type))]
    [Index(nameof(Created))]
    [Index(nameof(UserId), nameof(PostId), nameof(Type), IsUnique = true)]
    [Index(nameof(UserId), nameof(CommentId), nameof(Type), IsUnique = true)]
    [Index(nameof(UserId), nameof(ImageId), nameof(Type), IsUnique = true)]
    [Index(nameof(UserId), nameof(MessageId), nameof(Type), IsUnique = true)]
    [Index(nameof(PostId), nameof(Type))]
    [Index(nameof(CommentId), nameof(Type))]
    public class Reaction {
        public long Id { get; set; }

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public ReactionType Type { get; set; }

        // 対象の Post（Post へのリアクションの場合）
        public long? PostId { get; set; }
        public Post? Post { get; set; }

        // 対象の Comment（Comment へのリアクションの場合）
        public long? CommentId { get; set; }
        public Comment? Comment { get; set; }

        // 対象の Image（Image へのリアクションの場合）
        public long? ImageId { get; set; }
        public Image? Image { get; set; }

        // 対象の Message（Message へのリアクションの場合）
        public long? MessageId { get; set; }
        public Message? Message { get; set; }

        public DateTime Created { get; set; }
    }

}

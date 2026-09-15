using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("Reactions")]
    [Index(nameof(UserId))]
    [Index(nameof(DocId))]
    [Index(nameof(CommentId))]
    [Index(nameof(ImageId))]
    [Index(nameof(MessageId))]
    [Index(nameof(Type))]
    [Index(nameof(Created))]
    public class Reaction {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public ReactionType Type { get; set; }

        // 対象の Doc（Doc へのリアクションの場合）
        public int? DocId { get; set; }
        public Doc? Doc { get; set; }

        // 対象の Comment（Comment へのリアクションの場合）
        public int? CommentId { get; set; }
        public Comment? Comment { get; set; }

        // 対象の Image（Image へのリアクションの場合）
        public int? ImageId { get; set; }
        public Image? Image { get; set; }

        // 対象の Message（Message へのリアクションの場合）
        public int? MessageId { get; set; }
        public Message? Message { get; set; }

        public DateTime Created { get; set; }
    }

}

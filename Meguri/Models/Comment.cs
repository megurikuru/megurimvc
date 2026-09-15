using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("Comments")]
    [Index(nameof(DocId))]
    [Index(nameof(ImageId))]
    [Index(nameof(ParentId))]
    [Index(nameof(Created))]
    [Index(nameof(DocId), nameof(Number))]
    public class Comment {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        /// スレッド内の通しレス番号（>>1, >>2...）
        public int Number { get; set; }

        public string Text { get; set; }

        /// 論理削除フラグ（削除されたレスのアンカー破壊防止用）
        public bool IsDeleted { get; set; } = false;

        // 対象の Doc（Doc へのコメントの場合）
        public int? DocId { get; set; }
        public Doc Doc { get; set; }

        // 対象の Image（Image へのコメントの場合）
        public int? ImageId { get; set; }
        public Image Image { get; set; }

        // コメントへの返信（スレッド構造）
        public int? ParentId { get; set; }
        public Comment Parent { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        // --- 順序付き添付画像 ---
        public ICollection<CommentImage> CommentImages { get; set; } = new List<CommentImage>();

        // Commentに付けられたリアクション一覧
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

        [NotMapped]
        public ICollection<Image> Images => CommentImages
            .OrderBy(ci => ci.DisplayOrder)
            .Select(ci => ci.Image)
            .ToList();
    }

}

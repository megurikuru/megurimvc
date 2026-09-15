using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("Comments")]
    [Index(nameof(DocId))]
    [Index(nameof(ImageId))]
    [Index(nameof(ParentId))]
    [Index(nameof(Created))]
    public class Comment {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string Text { get; set; }

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
    }

}

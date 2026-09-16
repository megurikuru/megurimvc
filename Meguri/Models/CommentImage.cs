using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("CommentImages")]
    [Index(nameof(CommentId), nameof(ImageId), IsUnique = true)]
    public class CommentImage {
        public long Id { get; set; }

        public long CommentId { get; set; }
        public Comment Comment { get; set; } = null!;

        public long ImageId { get; set; }
        public Image Image { get; set; } = null!;

        /// 画像の表示順（0, 1, 2...）
        public int DisplayOrder { get; set; }
    }

}

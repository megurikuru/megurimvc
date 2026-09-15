using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("CommentImages")]
    [Index(nameof(CommentId), nameof(ImageId), IsUnique = true)]
    public class CommentImage {
        public int Id { get; set; }

        public int CommentId { get; set; }
        public Comment Comment { get; set; } = null!;

        public int ImageId { get; set; }
        public Image Image { get; set; } = null!;

        /// 画像の表示順（0, 1, 2...）
        public int DisplayOrder { get; set; }
    }

}

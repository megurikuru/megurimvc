using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("MessageImages")]
    [Index(nameof(MessageId), nameof(ImageId), IsUnique = true)]
    public class MessageImage {
        public int Id { get; set; }

        public int MessageId { get; set; }
        public Message Message { get; set; } = null!;

        public int ImageId { get; set; }
        public Image Image { get; set; } = null!;

        /// 画像の表示順（0, 1, 2...）
        public int DisplayOrder { get; set; }
    }

}

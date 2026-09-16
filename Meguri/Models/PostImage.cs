using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {
    [Table("PostImages")]
    [Index(nameof(ImageId))]
    public class PostImage {
        public long Id { get; set; }

        public long PostId { get; set; }
        public Post Post { get; set; } = null!;

        public long ImageId { get; set; }
        public Image Image { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }
}

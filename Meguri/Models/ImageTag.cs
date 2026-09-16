using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {
    [Table("ImageTags")]
    [Index(nameof(TagId))]
    public class ImageTag {
        public int Id { get; set; }

        public int ImageId { get; set; }
        public Image Image { get; set; } = null!;

        public long TagId { get; set; }
        public Tag Tag { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }
}

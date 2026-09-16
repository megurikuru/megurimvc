using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {
    [Table("ImageTags")]
    [Index(nameof(TagId))]
    public class ImageTag {
        public long Id { get; set; }

        public long ImageId { get; set; }
        public Image Image { get; set; } = null!;

        public long TagId { get; set; }
        public Tag Tag { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }
}

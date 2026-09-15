using System.ComponentModel.DataAnnotations.Schema;

namespace Meguri.Models {
    [Table("ImageTags")]
    public class ImageTag {
        public int Id { get; set; }

        public int ImageId { get; set; }
        public Image Image { get; set; } = null!;

        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }
}

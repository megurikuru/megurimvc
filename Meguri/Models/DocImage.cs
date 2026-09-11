using System.ComponentModel.DataAnnotations.Schema;

namespace Meguri.Models {
    [Table("DocImages")]
    public class DocImage {
        public int Id { get; set; }

        public int DocId { get; set; }
        public Doc Doc { get; set; } = null!;

        public int ImageId { get; set; }
        public Image Image { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }
}

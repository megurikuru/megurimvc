using System.ComponentModel.DataAnnotations.Schema;

namespace Meguri.Models {
    [Table("DocTags")]
    public class DocTag {
        public int Id { get; set; }

        public int DocId { get; set; }
        public Doc Doc { get; set; } = null!;

        public long TagId { get; set; }
        public Tag Tag { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }
}

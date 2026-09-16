using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {
    [Table("DocTags")]
    [Index(nameof(TagId))]
    public class DocTag {
        public int Id { get; set; }

        public int DocId { get; set; }
        public Doc Doc { get; set; } = null!;

        public long TagId { get; set; }
        public Tag Tag { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }
}

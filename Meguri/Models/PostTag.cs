using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {
    [Table("PostTags")]
    [Index(nameof(TagId))]
    public class PostTag {
        public long Id { get; set; }

        public long PostId { get; set; }
        public Post Post { get; set; } = null!;

        public long TagId { get; set; }
        public Tag Tag { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }
}

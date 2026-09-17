using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

/// Postと概念タグを結びつけるクラス
namespace Meguri.Models {
    [Table("PostTags")]
    [Index(nameof(TagConceptId))]
    public class PostTag {
        public long Id { get; set; }

        public long PostId { get; set; }
        public Post Post { get; set; } = null!;

        public long TagConceptId { get; set; }
        public TagConcept TagConcept { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }
}

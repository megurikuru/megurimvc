using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    /// Imageと概念タグを結びつけるクラス
    [Table("ImageTags")]
    [Index(nameof(TagConceptId))]
    public class ImageTag {
        public long Id { get; set; }

        public long ImageId { get; set; }
        public Image Image { get; set; } = null!;

        public long TagConceptId { get; set; }
        public TagConcept TagConcept { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }

}

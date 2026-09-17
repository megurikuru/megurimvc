using System;
using System.Collections.Generic;

// タグの概念（実体・カテゴリ）を表すクラス
namespace Meguri.Models {
    public class TagConcept {
        public long Id { get; set; }
        public string Category { get; set; } = "general";
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // 所有者（作成者）
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        // ナビゲーションプロパティ
        public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public virtual ICollection<TagRelationship> SubjectRelationships { get; set; } = new List<TagRelationship>();
        public virtual ICollection<TagRelationship> ObjectRelationships { get; set; } = new List<TagRelationship>();
        public virtual ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
        public virtual ICollection<ImageTag> ImageTags { get; set; } = new List<ImageTag>();
    }
}

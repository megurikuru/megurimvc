using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {
    [Table("Images")]
    [Index(nameof(Created))]
    [Index(nameof(IsPublic))]
    [Index(nameof(IsPublic), nameof(Created))]
    public class Image { 
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = false;
        public bool IsSexual  { get; set; } = false;
        public bool IsViolence  { get; set; } = false;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        // PostとImageは中間テーブルを介した多対多の関係
        public ICollection<PostImage> PostImages { get; set; } = new List<PostImage>();

        // ImageとTagは中間テーブルを介した多対多の関係
        public ICollection<ImageTag> ImageTags { get; set; } = new List<ImageTag>();

        // Imageに付けられたコメント一覧
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        // CommentとImageは中間テーブルを介した多対多の関係
        public ICollection<CommentImage> CommentImages { get; set; } = new List<CommentImage>();

        // MessageとImageは中間テーブルを介した多対多の関係
        public ICollection<MessageImage> MessageImages { get; set; } = new List<MessageImage>();

        // ユーザーのアイコン候補として登録されている中間一覧
        public ICollection<UserImage> UserImages { get; set; } = new List<UserImage>();

        // Imageに付けられたリアクション一覧
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

        // 直接のTag一覧が必要な場合は、ImageTagsから取り出す
        [NotMapped]
        public ICollection<TagConcept> Tags => ImageTags.Select(it => it.TagConcept).ToList();
    }
}

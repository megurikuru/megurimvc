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
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Caption { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = false;
        public byte[] Content { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        // DocとImageは中間テーブルを介した多対多の関係
        public ICollection<DocImage> DocImages { get; set; } = new List<DocImage>();

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
        public ICollection<Tag> Tags => ImageTags.Select(it => it.Tag).ToList();
    }
}

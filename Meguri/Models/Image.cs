using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Meguri.Models {
    [Table("Images")]
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

        // MessageとImageは中間テーブルを介した多対多の関係
        public ICollection<MessageImage> MessageImages { get; set; } = new List<MessageImage>();

        // 直接のTag一覧が必要な場合は、ImageTagsから取り出す
        [NotMapped]
        public ICollection<Tag> Tags => ImageTags.Select(it => it.Tag).ToList();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Meguri.Models {

    [Table("Docs")]
    [Index(nameof(ParentId))]
    [Index(nameof(FandomId))]
    [Index(nameof(Created))]
    [Index(nameof(Updated))]
    public class Doc {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public int? ParentId { get; set; }
        public int FandomId { get; set; }
        public bool Sexual  { get; set; } = false;
        public bool Violence  { get; set; } = false;
        public bool IsPublic { get; set; } = false;
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        // FandomとDocは一対多の関係
        public Fandom Fandom { get; set; }

        // DocとTagは中間テーブルを介した多対多の関係
        public ICollection<DocTag> DocTags { get; set; } = new List<DocTag>();

        // DocとImageは中間テーブルを介した多対多の関係
        public ICollection<DocImage> DocImages { get; set; } = new List<DocImage>();

        // 直接のTag一覧が必要な場合は、DocTagsから取り出す
        [NotMapped]
        public ICollection<Tag> Tags => DocTags.Select(dt => dt.Tag).ToList();
    }

}

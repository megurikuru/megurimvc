using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Meguri.Models {

    [Table("Docs")]
    [Index(nameof(UserId))]
    [Index(nameof(FandomId))]
    [Index(nameof(Created))]
    [Index(nameof(Updated))]
    [Index(nameof(LastCommentedAt))]
    [Index(nameof(IsPinned))]
    public class Doc {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public int FandomId { get; set; }
        public bool Sexual  { get; set; } = false;
        public bool Violence  { get; set; } = false;
        public bool IsPublic { get; set; } = false;

        /// スレッドの固定表示フラグ
        public bool IsPinned { get; set; } = false;

        /// スレッドの書き込みロック（締切）フラグ
        public bool IsLocked { get; set; } = false;

        /// コメント件数のキャッシュ
        public int CommentCount { get; set; } = 0;

        /// 閲覧数カウンター
        public int ViewCount { get; set; } = 0;

        /// 最終コメント投稿日時（スレッドフロート・ageソート用）
        public DateTime? LastCommentedAt { get; set; }

        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        // FandomとDocは一対多の関係
        public Fandom Fandom { get; set; }

        // DocとTagは中間テーブルを介した多対多の関係
        public ICollection<DocTag> DocTags { get; set; } = new List<DocTag>();

        // DocとImageは中間テーブルを介した多対多の関係
        public ICollection<DocImage> DocImages { get; set; } = new List<DocImage>();

        // Docに付けられたコメント一覧
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        // Docに付けられたリアクション一覧
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

        // 直接のTag一覧が必要な場合は、DocTagsから取り出す
        [NotMapped]
        public ICollection<Tag> Tags => DocTags.Select(dt => dt.Tag).ToList();
    }

}

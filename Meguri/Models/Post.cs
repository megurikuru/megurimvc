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
    [Index(nameof(IsPublic))]
    [Index(nameof(FandomId), nameof(LastCommentedAt))]
    [Index(nameof(FandomId), nameof(Created))]
    public class Post {
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
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
        public Fandom? Fandom { get; set; }

        // PostとTagは中間テーブルを介した多対多の関係
        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();

        // PostとImageは中間テーブルを介した多対多の関係
        public ICollection<PostImage> PostImages { get; set; } = new List<PostImage>();

        // Postに付けられたコメント一覧
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        // Postに付けられたリアクション一覧
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

        // 直接のTag一覧が必要な場合は、PostTagsから取り出す
        [NotMapped]
        public ICollection<TagConcept> Tags => PostTags.Select(pt => pt.TagConcept).ToList();
    }

}

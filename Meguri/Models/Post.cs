using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("Posts")]                                                                    // テーブル名
    [Index(nameof(UserId))]
    [Index(nameof(FandomId))]
    [Index(nameof(CreatedAt))]
    [Index(nameof(UpdatedAt))]
    [Index(nameof(LastCommentedAt))]
    [Index(nameof(IsPinned))]
    [Index(nameof(IsPublic))]
    [Index(nameof(FandomId), nameof(LastCommentedAt))]
    [Index(nameof(FandomId), nameof(CreatedAt))]
    public class Post {
        // プロパティ
        public long Id { get; set; }                                                    // 投稿ID
        public string UserId { get; set; } = string.Empty;                              // 投稿者ID
        public int FandomId { get; set; }                                               // 界隈ID
        public string Name { get; set; } = string.Empty;                                // タイトル
        public string Text { get; set; } = string.Empty;                                // 本文
        public bool IsPublic { get; set; } = false;                                     // 公開フラグ
        public bool IsSexual { get; set; } = false;                                     // 性的表現あり
        public bool IsViolence { get; set; } = false;                                   // 暴力表現あり
        public bool IsPinned { get; set; } = false;                                     // 固定表示
        public bool IsLocked { get; set; } = false;                                     // コメント禁止
        public int CommentCount { get; set; } = 0;                                      // コメント数
        public int ViewCount { get; set; } = 0;                                         // 閲覧数
        public DateTime CreatedAt { get; set; }                                         // 作成日時
        public DateTime UpdatedAt { get; set; }                                         // 更新日時
        public DateTime? LastCommentedAt { get; set; }                                  // 最終コメント日時

        // ナビゲーションプロパティ
        public ApplicationUser? User { get; set; }                                      // 投稿者
        public Fandom? Fandom { get; set; }                                             // 界隈

        // 関連データのナビゲーションプロパティ
        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();       // 投稿とタグの関連
        public ICollection<PostImage> PostImages { get; set; } = new List<PostImage>(); // 添付画像
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();       // コメント一覧
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();    // リアクション一覧

        // 計算プロパティ
        [NotMapped]                                                                     // DB非マッピング
        public IEnumerable<TagConcept> Tags =>                                          // タグ一覧(PostTagsから取得)
            PostTags.Where(pt => pt.TagConcept != null).Select(pt => pt.TagConcept!);
    }
}

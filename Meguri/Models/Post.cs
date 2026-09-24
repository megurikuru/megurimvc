using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("Posts")]
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
        // ==========================================
        // 主キー / 外部キー (Keys)
        // ==========================================
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int FandomId { get; set; }

        // ==========================================
        // 基本プロパティ (Basic Attributes)
        // ==========================================
        public string Name { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

        // ==========================================
        // フラグ・ステータス (Flags & Status)
        // ==========================================
        public bool IsPublic { get; set; } = false;
        public bool IsSexual { get; set; } = false;
        public bool IsViolence { get; set; } = false;

        // スレッドの固定表示フラグ
        public bool IsPinned { get; set; } = false;

        // スレッドの書き込みロック（締切）フラグ
        public bool IsLocked { get; set; } = false;

        // ==========================================
        // カウンター・キャッシュ (Counters)
        // ==========================================        
        // コメント件数のキャッシュ
        public int CommentCount { get; set; } = 0;

        // 閲覧数カウンター
        public int ViewCount { get; set; } = 0;

        // ==========================================
        // 日時・タイムスタンプ (Timestamps)
        // ==========================================
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 最終コメント投稿日時（スレッドフロート・ageソート用）
        public DateTime? LastCommentedAt { get; set; }

        // ==========================================
        // ナビゲーションプロパティ - 単数 (Reference Navigations)
        // ==========================================
        public ApplicationUser? User { get; set; }

        // FandomとPostは一対多の関係
        public Fandom? Fandom { get; set; }

        // ==========================================
        // ナビゲーションプロパティ - コレクション (Collection Navigations)
        // ==========================================

        // PostとTagは中間テーブルを介した多対多の関係
        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();

        // PostとImageは中間テーブルを介した多対多の関係
        public ICollection<PostImage> PostImages { get; set; } = new List<PostImage>();

        // Postに付けられたコメント一覧
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        // Postに付けられたリアクション一覧
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

        // ==========================================
        // 非マッピング / 計算プロパティ (Unmapped / Computed Properties)
        // ==========================================
        // 直接のTag一覧が必要な場合にPostTagsから取得

        [NotMapped]
        public IEnumerable<TagConcept> Tags => 
            PostTags.Where(pt => pt.TagConcept != null).Select(pt => pt.TagConcept!);
    }
}
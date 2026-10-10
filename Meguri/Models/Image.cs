using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {
    [Table("Images")]
    [Index(nameof(CreatedAt))]                                                                   // 作成日時での並べ替え・検索用
    [Index(nameof(IsPublic))]                                                                    // 公開状態での絞り込み用
    [Index(nameof(IsPublic), nameof(CreatedAt))]                                                 // 公開画像の新着順取得用
    [Index(nameof(UserId))]                                                                      // ユーザーごとの画像取得用
    public class Image {                                                                         // 画像エンティティ(メタデータと関連データを保持)
        // プロパティ
        public long Id { get; set; }                                                             // 画像ID(主キー)
        public string? UserId { get; set; }                                                      // 投稿者のユーザーID
        public string Name { get; set; } = string.Empty;                                         // 画像名
        public string StorageKey { get; set; } = string.Empty;                                   // ストレージ上の保存キー
        public string ContentType { get; set; } = "image/webp";                                  // MIMEタイプ
        public long FileSize { get; set; } = 0;                                                  // ファイルサイズ(バイト)
        public int? Width { get; set; }                                                          // 幅(px)。不明ならnull
        public int? Height { get; set; }                                                         // 高さ(px)。不明ならnull
        public string Description { get; set; } = string.Empty;                                  // 説明文
        public string Caption { get; set; } = string.Empty;                                      // キャプション
        public bool IsPublic { get; set; } = false;                                              // 公開フラグ
        public bool IsSexual { get; set; } = false;                                              // 性的表現を含むか
        public bool IsViolence { get; set; } = false;                                            // 暴力表現を含むか
        public DateTime CreatedAt { get; set; }                                                  // 作成日時
        public DateTime UpdatedAt { get; set; }                                                  // 更新日時

        // ナビゲーションプロパティ
        public ApplicationUser? User { get; set; }                                               // 投稿者(ナビゲーションプロパティ)

        // 関連データのナビゲーションプロパティ
        public ICollection<PostImage> PostImages { get; set; } = new List<PostImage>();          // PostとImageは中間テーブルを介した多対多の関係
        public ICollection<ImageTag> ImageTags { get; set; } = new List<ImageTag>();             // ImageとTagは中間テーブルを介した多対多の関係
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();                // Imageに付けられたコメント一覧
        public ICollection<CommentImage> CommentImages { get; set; } = new List<CommentImage>(); // CommentとImageは中間テーブルを介した多対多の関係
        public ICollection<MessageImage> MessageImages { get; set; } = new List<MessageImage>(); // MessageとImageは中間テーブルを介した多対多の関係
        public ICollection<UserImage> UserImages { get; set; } = new List<UserImage>();          // ユーザーのアイコン候補として登録されている中間一覧
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();             // Imageに付けられたリアクション一覧

        // 計算プロパティ
        [NotMapped]
        public ICollection<TagConcept> Tags => ImageTags.Select(it => it.TagConcept).ToList();   // 直接のTag一覧が必要な場合は、ImageTagsから取り出す(DB保存なし)
    }
}


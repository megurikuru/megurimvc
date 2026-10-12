using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("Messages")]
    [Index(nameof(ConversationId))]                                                              // 会話IDによる検索の高速化
    [Index(nameof(SenderId))]                                                                    // 送信者IDによる検索の高速化
    [Index(nameof(CreatedAt))]                                                                   // 作成日時によるソート・検索の高速化
    [Index(nameof(ConversationId), nameof(CreatedAt))]                                           // 特定の会話内での時系列順メッセージ取得の高速化
    public class Message {
        // プロパティ
        public long Id { get; set; }                                                             // メッセージID（主キー）
        public long ConversationId { get; set; }                                                 // 所属する会話のID（外部キー）
        public string SenderId { get; set; } = null!;                                            // メッセージ送信者のユーザーID（外部キー）
        public string Text { get; set; } = string.Empty;                                         // メッセージの本文テキスト
        public DateTime CreatedAt { get; set; }                                                  // メッセージの作成日時
        public DateTime UpdatedAt { get; set; }                                                  // メッセージの最終更新日時

        // リファレンス型（単数形）のナビゲーションプロパティ
        public Conversation Conversation { get; set; } = null!;                                  // 所属する会話のナビゲーションプロパティ
        public ApplicationUser Sender { get; set; } = null!;                                     // メッセージ送信者のユーザー情報のナビゲーションプロパティ

        // コレクション型（複数形）のナビゲーションプロパティ
        public ICollection<MessageImage> MessageImages { get; set; } = new List<MessageImage>(); // 順序付き添付画像の関連データ一覧（中間テーブルなど）
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();             // Messageに付けられたリアクション一覧

        // 計算プロパティ: 画像エンティティのリストを表示順に並び替えて取得する
        [NotMapped]
        public ICollection<Image> Images => MessageImages
            .OrderBy(mi => mi.DisplayOrder)
            .Select(mi => mi.Image)
            .ToList();                                                                           // データベースにマッピングされない、表示順に並び替えられた画像エンティティのリスト
    }

}

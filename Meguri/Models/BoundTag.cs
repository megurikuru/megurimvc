using System;
using System.Collections.Generic;

namespace Meguri.Models {
public class BoundTag
{
    public long BoundId { get; set; }

    /// メインタグID
    public long MainTagId { get; set; }
    public Tag MainTag { get; set; } = null!;

    /// 対象・コンテキストタグID（NULL許容：単体タグの場合）
    public long? ContextTagId { get; set; }
    public Tag? ContextTag { get; set; }

    /// 使用投稿数（パフォーマンスキャッシュ用）
    public int PostCount { get; set; } = 0;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // --- ナビゲーションプロパティ ---
    public ICollection<DocTag> DocTags { get; set; } = new List<DocTag>();
}
}

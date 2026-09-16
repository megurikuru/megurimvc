#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("Tags")]
    [Index(nameof(Name))]
    [Index(nameof(NormalizedName), IsUnique = true)]
    public class Tag {
        public long TagId { get; set; }

        /// 表示用名称（例: "PCVバルブ交換", "インプレッサ"）
        public required string Name { get; set; }

        /// 照合・検索用小文字化・正規化名称
        public required string NormalizedName { get; set; }

        /// 複数のコンテキストが存在するかどうかの衝突フラグ
        public bool IsAmbiguous { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // --- ナビゲーションプロパティ ---

        /// 自身がメインタグとして結合されているBoundTag一覧
        public ICollection<BoundTag> MainBoundTags { get; set; } = new List<BoundTag>();

        /// 自身がコンテキスト（対象）として結合されているBoundTag一覧
        public ICollection<BoundTag> ContextBoundTags { get; set; } = new List<BoundTag>();
    }
}

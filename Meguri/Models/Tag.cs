#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

// 概念に紐づく各言語のタグ文字列を表すクラス
namespace Meguri.Models {

    [Table("Tags")]
    [Index(nameof(TagText))]
    public class Tag {
        public long Id { get; set; }
        public long TagConceptId { get; set; }
        public string TagText { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = "ja";
        public bool IsCanonical { get; set; }
        public string NormalizedText { get; set; } = string.Empty;

        // ナビゲーションプロパティ
        public virtual TagConcept TagConcept { get; set; } = null!;
    }
}

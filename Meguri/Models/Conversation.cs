using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("Conversations")]
    [Index(nameof(Created))]
    [Index(nameof(Updated))]
    public class Conversation {
        public long Id { get; set; }

        /// グループ名（1対1 DMの場合はnull）
        public string? Title { get; set; }

        /// 1対1 DMかグループチャットかの識別フラグ
        public bool IsGroup { get; set; } = false;

        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        // --- ナビゲーションプロパティ ---
        public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

}

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("ConversationMembers")]
    [Index(nameof(ConversationId), nameof(UserId), IsUnique = true)]
    [Index(nameof(UserId))]
    public class ConversationMember {
        public long Id { get; set; }

        public long ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        /// 参加日時
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// 既読位置管理（未読バッジの計算用）
        public DateTime? LastReadAt { get; set; }
    }

}

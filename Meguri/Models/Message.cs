using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("Messages")]
    [Index(nameof(ConversationId))]
    [Index(nameof(SenderId))]
    [Index(nameof(Created))]
    [Index(nameof(ConversationId), nameof(Created))]
    public class Message {
        public int Id { get; set; }

        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public string SenderId { get; set; } = null!;
        public ApplicationUser Sender { get; set; } = null!;

        public string Text { get; set; } = string.Empty;

        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        // --- 順序付き添付画像 ---
        public ICollection<MessageImage> MessageImages { get; set; } = new List<MessageImage>();

        // Messageに付けられたリアクション一覧
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

        [NotMapped]
        public ICollection<Image> Images => MessageImages
            .OrderBy(mi => mi.DisplayOrder)
            .Select(mi => mi.Image)
            .ToList();
    }

}

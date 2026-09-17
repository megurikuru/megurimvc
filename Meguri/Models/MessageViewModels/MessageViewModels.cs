using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Meguri.Models;

namespace Meguri.Models.MessageViewModels {
    public class ConversationListViewModel {
        public List<ConversationItemViewModel> Conversations { get; set; } = new List<ConversationItemViewModel>();
        public List<ApplicationUser> AvailableUsers { get; set; } = new List<ApplicationUser>();
    }

    public class ConversationItemViewModel {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsGroup { get; set; }
        public string LastMessageText { get; set; } = string.Empty;
        public System.DateTime LastMessageTime { get; set; }
        public List<string> MemberNames { get; set; } = new List<string>();
    }

    public class CreateConversationViewModel {
        public bool IsGroup { get; set; } = false;

        [Display(Name = "Message_GroupName")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "メンバーを1名以上選択してください。")]
        [Display(Name = "Message_SelectRecipient")]
        public List<string> SelectedUserIds { get; set; } = new List<string>();

        [Required(ErrorMessage = "メッセージを入力してください。")]
        [Display(Name = "Message_InputPlaceholder")]
        public string InitialMessage { get; set; } = string.Empty;

        public List<IFormFile>? ImageFiles { get; set; }
    }

    public class ChatRoomViewModel {
        public Conversation Conversation { get; set; } = null!;
        public string CurrentUserId { get; set; } = string.Empty;
        public string DisplayTitle { get; set; } = string.Empty;
    }
}

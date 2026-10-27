using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Meguri.Models;

namespace Meguri.ViewModels.Message {
    public class ConversationListViewModel {
        public List<ConversationItemViewModel> Conversations { get; set; } = new List<ConversationItemViewModel>();
        public List<ApplicationUser> AvailableUsers { get; set; } = new List<ApplicationUser>();

        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int Skip { get; set; }
        public System.DateTime? FilterDate { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
        public int PreviousSkip { get; set; }
        public int NextSkip { get; set; }
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

        [Required(ErrorMessage = "Message_Error_SelectRecipientRequired")]
        [Display(Name = "Message_SelectRecipient")]
        public List<string> SelectedUserIds { get; set; } = new List<string>();

        [Display(Name = "Message_SubjectLabel")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [Display(Name = "Message_Title")]
        public string InitialMessage { get; set; } = string.Empty;
        public List<IFormFile>? ImageFiles { get; set; }

        public List<ApplicationUser> AvailableUsers { get; set; } = new List<ApplicationUser>();
    }

    public class EditMessageViewModel {
        public long Id { get; set; }
        public long ConversationId { get; set; }

        [Display(Name = "Message_SubjectLabel")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [Display(Name = "Message_Title")]
        public string Body { get; set; } = string.Empty;
    }

    public class ChatRoomViewModel {
        public Conversation Conversation { get; set; } = null!;
        public string CurrentUserId { get; set; } = string.Empty;
        public string DisplayTitle { get; set; } = string.Empty;

        public List<Meguri.Models.Message> Messages { get; set; } = new List<Meguri.Models.Message>();
        public int PageSize { get; set; } = 80;
        public int TotalCount { get; set; }
        public int Skip { get; set; }
        public System.DateTime? FilterDate { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
        public int PreviousSkip { get; set; }
        public int NextSkip { get; set; }
    }
}

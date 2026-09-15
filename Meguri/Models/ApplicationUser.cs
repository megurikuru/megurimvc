using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Meguri.Models {
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser {
        public ICollection<Fandom> Fandoms { get; set; }
        public ICollection<FandomUser> FandomUsers { get; set; } = new List<FandomUser>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    }
}

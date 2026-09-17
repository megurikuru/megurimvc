using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Meguri.Models {
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser {
        /// 自己紹介文（Bio / Profile description）
        public string Bio { get; set; }= string.Empty;

        /// 現在選択中のアバター（アイコン）画像ID
        public long? ActiveAvatarImageId { get; set; }
        public Image? ActiveAvatarImage { get; set; }

        /// ユーザーが登録・所持している画像一覧
        public ICollection<UserImage> UserImages { get; set; } = new List<UserImage>();

        /// 登録済みアバター画像一覧へのアクセス用便利プロパティ
        [NotMapped]
        public ICollection<Image> AvatarImages => UserImages.Select(ui => ui.Image).ToList();

        public ICollection<Post> Docs { get; set; } = new List<Post>();
        public ICollection<Fandom> Fandoms { get; set; } = new List<Fandom>();
        public ICollection<FandomUser> FandomUsers { get; set; } = new List<FandomUser>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
        public ICollection<TagConcept> TagConcepts { get; set; } = new List<TagConcept>();
    }
}

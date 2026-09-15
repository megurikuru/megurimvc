using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Meguri.Models {

    [Table("UserImages")]
    [Index(nameof(UserId), nameof(ImageId), IsUnique = true)]
    [Index(nameof(UserId))]
    public class UserImage {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public int ImageId { get; set; }
        public Image Image { get; set; } = null!;

        /// ユーザーが画像を登録した日時
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }

}

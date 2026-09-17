using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Meguri.Models.PostViewModels {
    public class PostCreateViewModel {
        [Required(ErrorMessage = "タイトルを入力してください。")]
        [Display(Name = "Post_PostName")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "本文を入力してください。")]
        [Display(Name = "Post_PostText")]
        public string Text { get; set; } = string.Empty;

        [Required(ErrorMessage = "界隈を選択してください。")]
        [Display(Name = "Post_SelectFandom")]
        public int FandomId { get; set; }

        [Display(Name = "Post_NSFW_Sexual")]
        public bool Sexual { get; set; } = false;

        [Display(Name = "Post_NSFW_Violence")]
        public bool Violence { get; set; } = false;

        [Display(Name = "Post_Public")]
        public bool IsPublic { get; set; } = true;

        [Display(Name = "Post_AttachImages")]
        public List<IFormFile> ImageFiles { get; set; } = new List<IFormFile>();

        [Display(Name = "Post_Tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class PostEditViewModel {
        public long Id { get; set; }

        [Required(ErrorMessage = "タイトルを入力してください。")]
        [Display(Name = "Post_PostName")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "本文を入力してください。")]
        [Display(Name = "Post_PostText")]
        public string Text { get; set; } = string.Empty;

        [Required(ErrorMessage = "界隈を選択してください。")]
        [Display(Name = "Post_SelectFandom")]
        public int FandomId { get; set; }

        [Display(Name = "Post_NSFW_Sexual")]
        public bool Sexual { get; set; } = false;

        [Display(Name = "Post_NSFW_Violence")]
        public bool Violence { get; set; } = false;

        [Display(Name = "Post_Public")]
        public bool IsPublic { get; set; } = true;

        [Display(Name = "Post_Pinned")]
        public bool IsPinned { get; set; } = false;

        [Display(Name = "Post_Locked")]
        public bool IsLocked { get; set; } = false;

        [Display(Name = "Post_Tags")]
        public List<string> Tags { get; set; } = new List<string>();

        // 既存の添付画像と表示順序
        public List<PostImageItemViewModel> ExistingImages { get; set; } = new List<PostImageItemViewModel>();

        // 新規追加する画像
        [Display(Name = "Post_AttachImages")]
        public List<IFormFile> NewImageFiles { get; set; } = new List<IFormFile>();
    }

    public class PostImageItemViewModel {
        public long ImageId { get; set; }
        public string ImageName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool Remove { get; set; } = false;
    }
}

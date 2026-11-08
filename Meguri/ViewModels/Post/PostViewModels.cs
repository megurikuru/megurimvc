using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Meguri.Models;
using Meguri.ViewModels.Shared;

namespace Meguri.ViewModels.Post {
    public class PostIndexViewModel {
        public List<Meguri.Models.Post> Posts { get; set; } = new List<Meguri.Models.Post>();
        public List<Meguri.Models.Fandom> Fandoms { get; set; } = new List<Meguri.Models.Fandom>();
        public Meguri.Models.Fandom? CurrentFandom { get; set; }
        public string? CurrentTag { get; set; }
        public string? CurrentSearch { get; set; }
        public bool CanPostToCurrentFandom { get; set; }
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
    }

    public class PostDetailsViewModel {
        public Meguri.Models.Post Post { get; set; } = null!;
        public string DisplayTitle { get; set; } = string.Empty;
        public bool IsAuthor { get; set; }
        public List<Meguri.Models.Fandom> Ancestors { get; set; } = new List<Meguri.Models.Fandom>();
    }

    public class PostCreateViewModel {
        [Display(Name = "Post_PostName")]
        [StringLength(200, ErrorMessage = "Validation_StringLengthMax")]
        public string? Name { get; set; }

        [Display(Name = "Post_PostText")]
        public string? Text { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [Display(Name = "Post_SelectFandom")]
        public int FandomId { get; set; }

        [Display(Name = "Post_NSFW_Sexual")]
        public bool IsSexual { get; set; } = false;

        [Display(Name = "Post_NSFW_Violence")]
        public bool IsViolence { get; set; } = false;

        [Display(Name = "Post_Public")]
        public bool IsPublic { get; set; } = false;

        [Display(Name = "Post_AttachImages")]
        public List<IFormFile>? ImageFiles { get; set; } = new List<IFormFile>();

        [Display(Name = "Post_Tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class PostEditViewModel {
        public long Id { get; set; }

        [Display(Name = "Post_PostName")]
        [StringLength(200, ErrorMessage = "Validation_StringLengthMax")]
        public string? Name { get; set; }

        [Display(Name = "Post_PostText")]
        public string? Text { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [Display(Name = "Post_SelectFandom")]
        public int FandomId { get; set; }

        [Display(Name = "Post_NSFW_Sexual")]
        public bool IsSexual { get; set; } = false;

        [Display(Name = "Post_NSFW_Violence")]
        public bool IsViolence { get; set; } = false;

        [Display(Name = "Post_Public")]
        public bool IsPublic { get; set; } = false;

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
        public List<IFormFile>? NewImageFiles { get; set; } = new List<IFormFile>();
    }

    public class PostImageItemViewModel {
        public long ImageId { get; set; }
        public string ImageName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool Remove { get; set; } = false;
    }
}

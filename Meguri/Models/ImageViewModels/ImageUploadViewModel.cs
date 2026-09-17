using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Meguri.Models.ImageViewModels {
    public class ImageUploadViewModel {
        [Display(Name = "Post_SelectFandom")]
        public int? FandomId { get; set; }

        [Display(Name = "Image_Caption")]
        [StringLength(200)]
        public string? Caption { get; set; }

        [Display(Name = "Image_Description")]
        public string? Description { get; set; }

        [Display(Name = "Post_Public")]
        public bool IsPublic { get; set; } = true;

        [Required(ErrorMessage = "画像ファイルを選択してください。")]
        [Display(Name = "Image_SelectFiles")]
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();

        [Display(Name = "Post_Tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class ImageEditViewModel {
        public long Id { get; set; }

        [Display(Name = "Image_Caption")]
        [StringLength(200)]
        public string? Caption { get; set; }

        [Display(Name = "Image_Description")]
        public string? Description { get; set; }

        [Display(Name = "Post_Public")]
        public bool IsPublic { get; set; } = true;

        [Display(Name = "Post_Tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }
}

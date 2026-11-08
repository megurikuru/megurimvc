using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Meguri.Models;
using Meguri.ViewModels.Shared;

namespace Meguri.ViewModels.Fandom {
    public class FandomDetailsViewModel {
        public Meguri.Models.Fandom Fandom { get; set; } = null!;
        public bool IsJoined { get; set; }
        public List<Meguri.Models.Post> FandomPosts { get; set; } = new List<Meguri.Models.Post>();
        public List<Meguri.Models.Fandom> Ancestors { get; set; } = new List<Meguri.Models.Fandom>();
        public int FandomPostsDescendantCount { get; set; }
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
    }

    public class FandomCreateViewModel {
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? ParentFandomId { get; set; }

        public List<Meguri.Models.Fandom> ParentFandoms { get; set; } = new List<Meguri.Models.Fandom>();
    }
}

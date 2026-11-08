using System.Collections.Generic;
using Meguri.Models;
using Meguri.ViewModels.Shared;

namespace Meguri.ViewModels.Profile {
    public class ProfileDetailsViewModel {
        public ApplicationUser Profile { get; set; } = null!;
        public bool IsOwner { get; set; }
        public List<string> ExternalUrls { get; set; } = new List<string>();
        public List<Meguri.Models.Post> Posts { get; set; } = new List<Meguri.Models.Post>();
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
    }
}

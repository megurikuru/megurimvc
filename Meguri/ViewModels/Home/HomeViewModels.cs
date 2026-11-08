using System.Collections.Generic;
using Meguri.Models;

namespace Meguri.ViewModels.Home {
    public class HomeIndexViewModel {
        public string? Search { get; set; }
        public List<Meguri.Models.Post> Posts { get; set; } = new List<Meguri.Models.Post>();
        public List<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public List<Meguri.Models.Fandom> TopFandoms { get; set; } = new List<Meguri.Models.Fandom>();
        public List<Meguri.Models.Fandom> Fandoms { get; set; } = new List<Meguri.Models.Fandom>();

        public int PostsPageSize { get; set; } = 10;
        public int PostsTotalCount { get; set; }
        public int PostsSkip { get; set; }
        public bool PostsHasPrevious { get; set; }
        public bool PostsHasNext { get; set; }
        public int PostsPreviousSkip { get; set; }
        public int PostsNextSkip { get; set; }
    }
}

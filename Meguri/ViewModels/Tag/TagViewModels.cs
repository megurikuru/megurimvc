using System.Collections.Generic;
using Meguri.Models;
using Meguri.ViewModels.Shared;

namespace Meguri.ViewModels.Tag {
    public class TagIndexViewModel {
        public List<TagConcept> Concepts { get; set; } = new List<TagConcept>();
        public string? SearchQuery { get; set; }
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
    }

    public class TagDetailsViewModel {
        public TagConcept Concept { get; set; } = null!;
        public string PrimaryTag { get; set; } = string.Empty;
        public bool CanEdit { get; set; }

        public List<PostTag> PostTags { get; set; } = new List<PostTag>();
        public int PostTagsPageSize { get; set; } = 10;
        public int PostTagsTotalCount { get; set; }
        public int PostTagsSkip { get; set; }
        public bool PostTagsHasPrevious { get; set; }
        public bool PostTagsHasNext { get; set; }
        public int PostTagsPreviousSkip { get; set; }
        public int PostTagsNextSkip { get; set; }

        public List<ImageTag> ImageTags { get; set; } = new List<ImageTag>();
        public int ImageTagsPageSize { get; set; } = 10;
        public int ImageTagsTotalCount { get; set; }
        public int ImageTagsSkip { get; set; }
        public bool ImageTagsHasPrevious { get; set; }
        public bool ImageTagsHasNext { get; set; }
        public int ImageTagsPreviousSkip { get; set; }
        public int ImageTagsNextSkip { get; set; }
    }
}

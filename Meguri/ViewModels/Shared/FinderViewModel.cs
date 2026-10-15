using System.Collections.Generic;
using Meguri.Models;

namespace Meguri.ViewModels.Shared {
    public class FinderViewModel {
        public int? ParentFandomId { get; set; }
        public IEnumerable<Fandom> ChildFandoms;
        public IEnumerable<Meguri.Models.Post> Posts { get; set; } = [];
    }
}

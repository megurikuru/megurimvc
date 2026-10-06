using System.Collections.Generic;
using Meguri.Models;

namespace Meguri.ViewModels {
    public class FinderViewModel {
        public int? ParentFandomId { get; set; }
        public IEnumerable<Fandom> ChildFandoms;
        public IEnumerable<Post> Posts { get; set; } = [];
    }
}

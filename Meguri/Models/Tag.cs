#nullable enable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Meguri.Models {

    [Table("Tags")]
    public class Tag {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int? ParentTagId { get; set; }
        public Tag? ParentTag { get; set; }
        public ICollection<Tag> Children { get; set; } = new List<Tag>();

        // DocとTagは中間テーブルを介した多対多の関係
        public ICollection<DocTag> DocTags { get; set; } = new List<DocTag>();

        [NotMapped]
        public ICollection<Doc> Docs => DocTags.Select(dt => dt.Doc).ToList();

    }
} 

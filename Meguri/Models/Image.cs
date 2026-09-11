using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Meguri.Models {
    [Table("Images")]
    public class Image { 
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Caption { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = false;
        public byte[] Content { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        // DocとImageは中間テーブルを介した多対多の関係
        public ICollection<DocImage> DocImages { get; set; } = new List<DocImage>();
    }
}

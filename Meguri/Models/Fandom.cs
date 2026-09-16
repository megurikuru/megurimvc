using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Meguri.Models {

    [Table("Fandoms")]
    [Index(nameof(ParentFandomId))]
    [Index(nameof(Name))]
    public class Fandom {
        public int Id { get; set; }
        public string Name{ get; set; }

        /// 親FandomのID（最上位階層の場合はnull）
        public int? ParentFandomId { get; set; }

        /// 親Fandom
        [ForeignKey(nameof(ParentFandomId))]
        public Fandom? ParentFandom { get; set; }

        /// 子Fandom一覧
        [InverseProperty(nameof(ParentFandom))]
        public ICollection<Fandom> ChildFandoms { get; set; } = new List<Fandom>();

        public ICollection<Post> Docs { get; set; } = new List<Post>();
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<FandomUser> FandomUsers { get; set; } = new List<FandomUser>();
    }

}

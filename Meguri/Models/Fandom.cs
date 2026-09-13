using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Meguri.Models {

    [Table("Fandoms")]
    public class Fandom {
        public int Id { get; set; }
        public string Name{ get; set; }

        public ICollection<Doc> Docs { get; set; }
    }

}

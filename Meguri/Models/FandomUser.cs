using System.ComponentModel.DataAnnotations.Schema;

namespace Meguri.Models {
    [Table("FandomUsers")]
    public class FandomUser {
        public int FandomId { get; set; }
        public string UserId { get; set; }

        public Fandom Fandom { get; set; }
        public ApplicationUser User { get; set; }
    }
}

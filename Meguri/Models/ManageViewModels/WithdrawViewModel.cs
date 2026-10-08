using System.ComponentModel.DataAnnotations;

namespace Meguri.Models.ManageViewModels {
    public class WithdrawViewModel {
        public bool HasPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Account_Field_Password")]
        public string Password { get; set; }
    }
}

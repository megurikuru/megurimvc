using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Meguri.Models.AccountViewModels {
    public class LoginViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [Display(Name = "Account_Field_UserName")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [DataType(DataType.Password)]
        [Display(Name = "Account_Field_Password")]
        public string Password { get; set; }

        [Display(Name = "Account_Field_RememberMe")]
        public bool RememberMe { get; set; }
    }
}

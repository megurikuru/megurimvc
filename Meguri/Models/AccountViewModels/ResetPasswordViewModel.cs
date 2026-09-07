using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Meguri.Models.AccountViewModels {
    public class ResetPasswordViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [EmailAddress(ErrorMessage = "Validation_InvalidEmail")]
        [Display(Name = "Account_Field_Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [StringLength(100, ErrorMessage = "Validation_StringLength", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Account_Field_Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Account_Field_ConfirmPassword")]
        [Compare("Password", ErrorMessage = "Validation_Compare")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }
}

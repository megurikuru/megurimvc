using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Meguri.Validation;

namespace Meguri.ViewModels.Account {
    public class ResetPasswordViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [EmailAddress(ErrorMessage = "Validation_InvalidEmail")]
        [Display(Name = "Account_Field_Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [PasswordRequirements]
        [DataType(DataType.Password)]
        [Display(Name = "Account_Field_Password")]
        public string Password { get; set; }

        public string Code { get; set; }
    }
}

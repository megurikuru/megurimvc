using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Meguri.Models.AccountViewModels {
    public class ExternalLoginViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [EmailAddress(ErrorMessage = "Validation_InvalidEmail")]
        [Display(Name = "Account_Field_Email")]
        public string Email { get; set; }
    }
}

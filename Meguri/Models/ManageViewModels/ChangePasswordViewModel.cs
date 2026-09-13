using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Meguri.Models.ManageViewModels {
    public class ChangePasswordViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [DataType(DataType.Password)]
        [Display(Name = "Account_Field_Password")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [StringLength(100, ErrorMessage = "Validation_StringLength", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Manage_ChangePassword_NewPassword")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Manage_ChangePassword_ConfirmPassword")]
        [Compare("NewPassword", ErrorMessage = "Validation_Compare")]
        public string ConfirmPassword { get; set; }

        public string StatusMessage { get; set; }
    }
}

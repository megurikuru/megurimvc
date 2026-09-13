using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Meguri.Models.ManageViewModels {
    public class SetPasswordViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [StringLength(100, ErrorMessage = "Validation_StringLength", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Manage_SetPassword_NewPassword")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Manage_SetPassword_ConfirmPassword")]
        [Compare("NewPassword", ErrorMessage = "Validation_Compare")]
        public string ConfirmPassword { get; set; }

        public string StatusMessage { get; set; }
    }
}

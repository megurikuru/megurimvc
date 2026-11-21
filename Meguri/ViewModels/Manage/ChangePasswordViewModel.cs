using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Meguri.Constants;
using Meguri.Validation;

namespace Meguri.ViewModels.Manage {
    public class ChangePasswordViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [StringLength(IdConst.MaxLength, ErrorMessage = "Validation_StringLengthMax")]
        [DataType(DataType.Password)]
        [Display(Name = "Account_Field_Password")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [PasswordRequirements]
        [DataType(DataType.Password)]
        [Display(Name = "Manage_ChangePassword_NewPassword")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Manage_ChangePassword_ConfirmPassword")]
        [Compare("NewPassword", ErrorMessage = "Validation_Compare")]
        public string ConfirmPassword { get; set; }

        public bool CanRemovePasswordAnd2fa { get; set; }

        public string StatusMessage { get; set; }
    }
}

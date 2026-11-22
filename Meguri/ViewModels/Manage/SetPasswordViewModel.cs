using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Meguri.Validation;

namespace Meguri.ViewModels.Manage {
    public class SetPasswordViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [PasswordRequirements]
        [DataType(DataType.Password)]
        [Display(Name = "Manage_SetPassword_NewPassword")]
        public string NewPassword { get; set; }

        public bool HasPasskeys { get; set; }

        public string StatusMessage { get; set; }
    }
}

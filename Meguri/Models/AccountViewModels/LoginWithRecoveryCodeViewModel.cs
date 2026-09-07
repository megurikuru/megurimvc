using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Meguri.Models.AccountViewModels {
    public class LoginWithRecoveryCodeViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [DataType(DataType.Text)]
        [Display(Name = "Account_Field_RecoveryCode")]
        public string RecoveryCode { get; set; }
    }
}

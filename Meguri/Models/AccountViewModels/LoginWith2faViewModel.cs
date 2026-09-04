using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Meguri.Models.AccountViewModels {
    public class LoginWith2faViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [StringLength(7, ErrorMessage = "Validation_StringLength", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Account_Field_AuthenticatorCode")]
        public string TwoFactorCode { get; set; }

        [Display(Name = "Account_Field_RememberMachine")]
        public bool RememberMachine { get; set; }

        public bool RememberMe { get; set; }
    }
}

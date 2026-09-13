using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Meguri.Models.ManageViewModels {
    public class EnableAuthenticatorViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [StringLength(7, ErrorMessage = "Validation_StringLength", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Manage_EnableAuthenticator_VerificationCode")]
        public string Code { get; set; }

        [BindNever]
        public string SharedKey { get; set; }

        [BindNever]
        public string AuthenticatorUri { get; set; }
    }
}

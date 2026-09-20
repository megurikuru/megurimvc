using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Meguri.Models.ManageViewModels {
    public class IndexViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [StringLength(256, ErrorMessage = "Validation_StringLength")]
        [Display(Name = "Manage_Profile_Username")]
        public string Username { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [EmailAddress(ErrorMessage = "Validation_InvalidEmail")]
        [Display(Name = "Manage_Profile_Email")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Manage_Profile_PhoneNumber")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Manage_Profile_DateOfBirth")]
        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        [Display(Name = "Manage_Profile_Bio")]
        [StringLength(1000, ErrorMessage = "Validation_StringLength")]
        public string Bio { get; set; } = string.Empty;

        public string StatusMessage { get; set; }
    }
}

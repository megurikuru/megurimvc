using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace Meguri.Models.AccountViewModels {
    public class RegisterViewModel : IValidatableObject {
        [Required(ErrorMessage = "Validation_Required")]
        [StringLength(256, ErrorMessage = "Validation_StringLength")]
        [Display(Name = "Account_Field_UserName")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [EmailAddress(ErrorMessage = "Validation_InvalidEmail")]
        [Display(Name = "Account_Field_Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [StringLength(100, ErrorMessage = "Validation_StringLength", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Account_Field_Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Account_Field_ConfirmPassword")]
        [Compare("Password", ErrorMessage = "Validation_Compare")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [Display(Name = "Account_Field_DateOfBirth_Year")]
        public int? BirthYear { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [Display(Name = "Account_Field_DateOfBirth_Month")]
        public int? BirthMonth { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [Display(Name = "Account_Field_DateOfBirth_Day")]
        public int? BirthDay { get; set; }

        public DateOnly DateOfBirth { get; private set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (BirthYear.HasValue && BirthMonth.HasValue && BirthDay.HasValue) {
                var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) as IStringLocalizer<SharedResource>;
                string invalidDateMessage = localizer?["Validation_InvalidDate"] ?? "Validation_InvalidDate";

                bool isValidDate = true;
                try {
                    DateOfBirth = new DateOnly(BirthYear.Value, BirthMonth.Value, BirthDay.Value);
                } catch (ArgumentOutOfRangeException) {
                    isValidDate = false;
                }

                if (!isValidDate || DateOfBirth > DateOnly.FromDateTime(DateTime.Today)) {
                    yield return new ValidationResult(invalidDateMessage, new[] { nameof(BirthYear), nameof(BirthMonth), nameof(BirthDay) });
                }
            }
        }
    }
}

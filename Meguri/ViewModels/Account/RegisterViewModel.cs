using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace Meguri.ViewModels.Account {
    public class RegisterBaseViewModel : IValidatableObject {
        [Required(ErrorMessage = "Validation_Required")]
        [StringLength(256, ErrorMessage = "Validation_StringLengthMax")]
        [Display(Name = "Account_Field_UserName")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [EmailAddress(ErrorMessage = "Validation_InvalidEmail")]
        [Display(Name = "Account_Field_Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [Display(Name = "Account_Field_DateOfBirth_Year")]
        public int? BirthYear { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [Display(Name = "Account_Field_DateOfBirth_Month")]
        public int? BirthMonth { get; set; }

        [Required(ErrorMessage = "Validation_Required")]
        [Display(Name = "Account_Field_DateOfBirth_Day")]
        public int? BirthDay { get; set; }

        [StringLength(100, ErrorMessage = "Validation_StringLengthMax")]
        [Display(Name = "Manage_Passkeys_Name")]
        public string? PasskeyName { get; set; }

        [Display(Name = "Account_Register_AgreeToTerms")]
        public bool AgreeToTerms { get; set; }

        public DateOnly DateOfBirth { get; protected set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) as IStringLocalizer<SharedResource>;

            if (BirthYear.HasValue && BirthMonth.HasValue && BirthDay.HasValue) {
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

            if (!AgreeToTerms) {
                string agreeToTermsMessage = localizer?["Validation_AgreeToTerms"] ?? "Validation_AgreeToTerms";
                yield return new ValidationResult(agreeToTermsMessage, new[] { nameof(AgreeToTerms) });
            }
        }
    }

    public class RegisterViewModel : RegisterBaseViewModel {
        [Required(ErrorMessage = "Validation_Required")]
        [StringLength(100, ErrorMessage = "Validation_StringLength", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Account_Field_Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Account_Field_ConfirmPassword")]
        [Compare("Password", ErrorMessage = "Validation_Compare")]
        public string ConfirmPassword { get; set; }
    }

    public class PasskeyRegisterViewModel : RegisterBaseViewModel {
        public string? CredentialJson { get; set; }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Meguri.Services {
    public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public LocalizedIdentityErrorDescriber(IStringLocalizer<SharedResource> localizer) {
            _localizer = localizer;
        }

        public override IdentityError DefaultError() => new() {
            Code = nameof(DefaultError),
            Description = _localizer["Identity_DefaultError"]
        };

        public override IdentityError ConcurrencyFailure() => new() {
            Code = nameof(ConcurrencyFailure),
            Description = _localizer["Identity_ConcurrencyFailure"]
        };

        public override IdentityError PasswordMismatch() => new() {
            Code = nameof(PasswordMismatch),
            Description = _localizer["Identity_PasswordMismatch"]
        };

        public override IdentityError InvalidToken() => new() {
            Code = nameof(InvalidToken),
            Description = _localizer["Identity_InvalidToken"]
        };

        public override IdentityError RecoveryCodeRedemptionFailed() => new() {
            Code = nameof(RecoveryCodeRedemptionFailed),
            Description = _localizer["Identity_RecoveryCodeRedemptionFailed"]
        };

        public override IdentityError LoginAlreadyAssociated() => new() {
            Code = nameof(LoginAlreadyAssociated),
            Description = _localizer["Identity_LoginAlreadyAssociated"]
        };

        public override IdentityError InvalidUserName(string? userName) => new() {
            Code = nameof(InvalidUserName),
            Description = string.Format(_localizer["Identity_InvalidUserName"].Value, userName)
        };

        public override IdentityError InvalidEmail(string? email) => new() {
            Code = nameof(InvalidEmail),
            Description = string.Format(_localizer["Identity_InvalidEmail"].Value, email)
        };

        public override IdentityError DuplicateUserName(string userName) => new() {
            Code = nameof(DuplicateUserName),
            Description = string.Format(_localizer["Identity_DuplicateUserName"].Value, userName)
        };

        public override IdentityError DuplicateEmail(string email) => new() {
            Code = nameof(DuplicateEmail),
            Description = string.Format(_localizer["Identity_DuplicateEmail"].Value, email)
        };

        public override IdentityError InvalidRoleName(string? role) => new() {
            Code = nameof(InvalidRoleName),
            Description = string.Format(_localizer["Identity_InvalidRoleName"].Value, role)
        };

        public override IdentityError DuplicateRoleName(string role) => new() {
            Code = nameof(DuplicateRoleName),
            Description = string.Format(_localizer["Identity_DuplicateRoleName"].Value, role)
        };

        public override IdentityError UserAlreadyHasPassword() => new() {
            Code = nameof(UserAlreadyHasPassword),
            Description = _localizer["Identity_UserAlreadyHasPassword"]
        };

        public override IdentityError UserLockoutNotEnabled() => new() {
            Code = nameof(UserLockoutNotEnabled),
            Description = _localizer["Identity_UserLockoutNotEnabled"]
        };

        public override IdentityError UserAlreadyInRole(string role) => new() {
            Code = nameof(UserAlreadyInRole),
            Description = string.Format(_localizer["Identity_UserAlreadyInRole"].Value, role)
        };

        public override IdentityError UserNotInRole(string role) => new() {
            Code = nameof(UserNotInRole),
            Description = string.Format(_localizer["Identity_UserNotInRole"].Value, role)
        };

        public override IdentityError PasswordTooShort(int length) => new() {
            Code = nameof(PasswordTooShort),
            Description = string.Format(_localizer["Identity_PasswordTooShort"].Value, length)
        };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() {
            Code = nameof(PasswordRequiresUniqueChars),
            Description = string.Format(_localizer["Identity_PasswordRequiresUniqueChars"].Value, uniqueChars)
        };

        public override IdentityError PasswordRequiresNonAlphanumeric() => new() {
            Code = nameof(PasswordRequiresNonAlphanumeric),
            Description = _localizer["Identity_PasswordRequiresNonAlphanumeric"]
        };

        public override IdentityError PasswordRequiresDigit() => new() {
            Code = nameof(PasswordRequiresDigit),
            Description = _localizer["Identity_PasswordRequiresDigit"]
        };

        public override IdentityError PasswordRequiresLower() => new() {
            Code = nameof(PasswordRequiresLower),
            Description = _localizer["Identity_PasswordRequiresLower"]
        };

        public override IdentityError PasswordRequiresUpper() => new() {
            Code = nameof(PasswordRequiresUpper),
            Description = _localizer["Identity_PasswordRequiresUpper"]
        };
    }
}

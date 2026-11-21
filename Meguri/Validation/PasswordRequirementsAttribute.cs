using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Meguri.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Meguri.Validation {
    /// <summary>
    /// IdentityOptions.Password の設定値（必須桁数・文字種要件）および最大文字数に連動して
    /// サーバー側・クライアント側の双方でパスワード検証を行うカスタム属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PasswordRequirementsAttribute : ValidationAttribute, IClientModelValidator {
        public int MaxLength { get; }

        public PasswordRequirementsAttribute(int maxLength = IdConst.MaxLength) {
            if (maxLength <= 0) {
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            }
            MaxLength = maxLength;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is not string password || string.IsNullOrEmpty(password)) {
                return ValidationResult.Success;
            }

            var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) as IStringLocalizer<SharedResource>;
            var identityOptions = (validationContext.GetService(typeof(IOptions<IdentityOptions>)) as IOptions<IdentityOptions>)?.Value;
            var passwordOptions = identityOptions?.Password ?? CreateDefaultPasswordOptions();
            var memberNames = !string.IsNullOrEmpty(validationContext.MemberName)
                ? new[] { validationContext.MemberName }
                : null;

            if (password.Length > MaxLength) {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    localizer?["Validation_StringLengthMax"].Value ?? "The {0} must be at max {1} characters long.",
                    validationContext.DisplayName,
                    MaxLength);
                return new ValidationResult(message, memberNames);
            }

            if (password.Length < passwordOptions.RequiredLength) {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    localizer?["Validation_StringLength"].Value ?? "The {0} must be at least {2} and at max {1} characters long.",
                    validationContext.DisplayName,
                    MaxLength,
                    passwordOptions.RequiredLength);
                return new ValidationResult(message, memberNames);
            }

            if (passwordOptions.RequireNonAlphanumeric && password.All(IsLetterOrDigit)) {
                var message = localizer?["Identity_PasswordRequiresNonAlphanumeric"].Value
                    ?? "Passwords must have at least one non alphanumeric character.";
                return new ValidationResult(message, memberNames);
            }

            if (passwordOptions.RequireDigit && !password.Any(IsDigit)) {
                var message = localizer?["Identity_PasswordRequiresDigit"].Value
                    ?? "Passwords must have at least one digit ('0'-'9').";
                return new ValidationResult(message, memberNames);
            }

            if (passwordOptions.RequireLowercase && !password.Any(IsLower)) {
                var message = localizer?["Identity_PasswordRequiresLower"].Value
                    ?? "Passwords must have at least one lowercase ('a'-'z').";
                return new ValidationResult(message, memberNames);
            }

            if (passwordOptions.RequireUppercase && !password.Any(IsUpper)) {
                var message = localizer?["Identity_PasswordRequiresUpper"].Value
                    ?? "Passwords must have at least one uppercase ('A'-'Z').";
                return new ValidationResult(message, memberNames);
            }

            if (passwordOptions.RequiredUniqueChars >= 1 && password.Distinct().Count() < passwordOptions.RequiredUniqueChars) {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    localizer?["Identity_PasswordRequiresUniqueChars"].Value ?? "Passwords must use at least {0} different characters.",
                    passwordOptions.RequiredUniqueChars);
                return new ValidationResult(message, memberNames);
            }

            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context) {
            ArgumentNullException.ThrowIfNull(context);

            var services = context.ActionContext.HttpContext.RequestServices;
            var passwordOptions = services.GetService<IOptions<IdentityOptions>>()?.Value.Password ?? CreateDefaultPasswordOptions();
            var localizer = services.GetService<IStringLocalizer<SharedResource>>();
            var displayName = context.ModelMetadata.GetDisplayName();
            var requiredLength = Math.Max(1, passwordOptions.RequiredLength);

            var message = string.Format(
                CultureInfo.CurrentCulture,
                localizer?["Validation_StringLength"].Value ?? "The {0} must be at least {2} and at max {1} characters long.",
                displayName,
                MaxLength,
                requiredLength);

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-length", message);
            MergeAttribute(context.Attributes, "data-val-length-max", MaxLength.ToString(CultureInfo.InvariantCulture));
            MergeAttribute(context.Attributes, "data-val-length-min", requiredLength.ToString(CultureInfo.InvariantCulture));
        }

        private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value) {
            if (!attributes.ContainsKey(key)) {
                attributes.Add(key, value);
            }
        }

        private static PasswordOptions CreateDefaultPasswordOptions() => new() {
            RequiredLength = IdConst.MinLength,
            RequiredUniqueChars = IdConst.RequiredUniqueChars,
            RequireDigit = false,
            RequireLowercase = false,
            RequireUppercase = false,
            RequireNonAlphanumeric = false
        };

        private static bool IsDigit(char c) => c >= '0' && c <= '9';
        private static bool IsLower(char c) => c >= 'a' && c <= 'z';
        private static bool IsUpper(char c) => c >= 'A' && c <= 'Z';
        private static bool IsLetterOrDigit(char c) => IsUpper(c) || IsLower(c) || IsDigit(c);
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Meguri.Validation {
    /// <summary>
    /// appsettings.json の Identity:Password:MaxLength に連動して
    /// サーバー側・クライアント側の双方でパスワードの最大文字数検証を行うカスタム属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PasswordMaxLengthAttribute : ValidationAttribute, IClientModelValidator {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (value is not string password || string.IsNullOrEmpty(password)) {
                return ValidationResult.Success;
            }

            var configuration = validationContext.GetService(typeof(IConfiguration)) as IConfiguration;
            var maxLength = PasswordRequirementsAttribute.GetMaxLength(configuration);

            if (password.Length > maxLength) {
                var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) as IStringLocalizer<SharedResource>;
                var memberNames = !string.IsNullOrEmpty(validationContext.MemberName)
                    ? new[] { validationContext.MemberName }
                    : null;
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    localizer?["Validation_StringLengthMax"].Value ?? "The {0} must be at max {1} characters long.",
                    validationContext.DisplayName,
                    maxLength);
                return new ValidationResult(message, memberNames);
            }

            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context) {
            ArgumentNullException.ThrowIfNull(context);

            var services = context.ActionContext.HttpContext.RequestServices;
            var configuration = services.GetService<IConfiguration>();
            var maxLength = PasswordRequirementsAttribute.GetMaxLength(configuration);
            var localizer = services.GetService<IStringLocalizer<SharedResource>>();
            var displayName = context.ModelMetadata.GetDisplayName();

            var message = string.Format(
                CultureInfo.CurrentCulture,
                localizer?["Validation_StringLengthMax"].Value ?? "The {0} must be at max {1} characters long.",
                displayName,
                maxLength);

            var maxLengthStr = maxLength.ToString(CultureInfo.InvariantCulture);
            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-length", message);
            MergeAttribute(context.Attributes, "data-val-length-max", maxLengthStr);
            MergeAttribute(context.Attributes, "maxlength", maxLengthStr);
        }

        private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value) {
            if (!attributes.ContainsKey(key)) {
                attributes.Add(key, value);
            }
        }
    }
}

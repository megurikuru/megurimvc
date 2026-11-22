using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meguri.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Meguri.Validation {

    // ユーザー名・メールアドレスとの一致／包含およびよくあるパスワードを拒否するカスタムパスワード検証器
    public sealed class CommonPasswordValidator(IStringLocalizer<SharedResource> localizer)
        : IPasswordValidator<ApplicationUser> {

        private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase) {
            "123456789012",
            "1234567890123",
            "12345678901234",
            "123456789012345",
            "1234567890123456",
            "012345678901",
            "012345678912",
            "987654321098",
            "111111111111",
            "000000000000",
            "123412341234",
            "123123123123",
            "123456123456",
            "1q2w3e4r5t6y",
            "1qaz2wsx3edc",
            "1qaz2wsx3edc4rfv",
            "qazwsxedcrfv",
            "qwertyuiopas",
            "qwertyuiopasdf",
            "qwertyuiopasdfgh",
            "qwerty123456",
            "asdfghjklzxc",
            "zxcvbnmasdfg",
            "abcdefghijkl",
            "abcdefghijklm",
            "abcdefghijklmn",
            "abcdefghijklmno",
            "abcdefghijklmnopqrstuvwxyz",
            "password1234",
            "password12345",
            "password123456",
            "password1234!",
            "passwordpassword",
            "passw0rd1234",
            "p@ssw0rd1234",
            "iloveyou1234",
            "welcome12345",
            "admin1234567",
            "administrator",
            "letmein12345",
            "monkey123456",
            "dragon123456",
            "baseball1234",
            "football1234",
            "sunshine1234",
            "princess1234",
            "master123456",
            "shadow123456",
            "superman1234",
            "trustno12345",
            "meguri123456",
            "meguripassword",
            "megurikuru12"
        };

        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password) {
            if (string.IsNullOrEmpty(password)) {
                return Task.FromResult(IdentityResult.Success);
            }

            var errors = new List<IdentityError>();

            // 1. ユーザー名との一致／包含チェック
            if (!string.IsNullOrEmpty(user.UserName) &&
                (string.Equals(password, user.UserName, StringComparison.OrdinalIgnoreCase) ||
                 (user.UserName.Length >= 3 && password.Contains(user.UserName, StringComparison.OrdinalIgnoreCase)))) {
                errors.Add(new IdentityError {
                    Code = "PasswordContainsUserName",
                    Description = localizer["Identity_PasswordContainsUserName"]
                });
            }

            // 2. メールアドレス（またはローカル部）との一致／包含チェック
            if (!string.IsNullOrEmpty(user.Email)) {
                var atIndex = user.Email.IndexOf('@');
                var emailLocalPart = atIndex > 0 ? user.Email[..atIndex] : string.Empty;
                var containsEmail = password.Contains(user.Email, StringComparison.OrdinalIgnoreCase)
                    || (emailLocalPart.Length >= 3 && password.Contains(emailLocalPart, StringComparison.OrdinalIgnoreCase));

                if (containsEmail) {
                    errors.Add(new IdentityError {
                        Code = "PasswordContainsEmail",
                        Description = localizer["Identity_PasswordContainsEmail"]
                    });
                }
            }

            // 3. よくあるパスワードチェック
            if (CommonPasswords.Contains(password)) {
                errors.Add(new IdentityError {
                    Code = "PasswordTooCommon",
                    Description = localizer["Identity_PasswordTooCommon"]
                });
            }

            return Task.FromResult(errors.Count == 0
                ? IdentityResult.Success
                : IdentityResult.Failed(errors.ToArray()));
        }
    }
}

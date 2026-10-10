using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Meguri.Data;
using Meguri.Models;
using Meguri.Models.ManageViewModels;
using Meguri.Services;

namespace Meguri.Controllers {
    [Authorize]
    [Route("[controller]/[action]")]
    public class ManageController : Controller {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender<ApplicationUser> _emailSender;
        private readonly ILogger _logger;
        private readonly UrlEncoder _urlEncoder;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ApplicationDbContext _context;
        private readonly IR2StorageService _r2StorageService;
        private readonly IImageProcessingService _imageProcessingService;

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        private const string RecoveryCodesKey = nameof(RecoveryCodesKey);

        public ManageController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          IEmailSender<ApplicationUser> emailSender,
          ILogger<ManageController> logger,
          UrlEncoder urlEncoder,
          IStringLocalizer<SharedResource> localizer,
          ApplicationDbContext context,
          IR2StorageService r2StorageService,
          IImageProcessingService imageProcessingService) {
            _imageProcessingService = imageProcessingService;
            _context = context;
            _r2StorageService = r2StorageService;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _urlEncoder = urlEncoder;
            _localizer = localizer;
        }

        [TempData]
        public string StatusMessage { get; set; }

        // 存在しないユーザーの認証Cookieが残っている場合は、サインアウトしてログイン画面へ戻す
        private async Task<IActionResult> SignOutAndRedirectToLoginAsync() {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(AccountController.Login), "Account");
        }

        [HttpGet]
        public async Task<IActionResult> Index() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var urls = user.ExternalUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(5).Cast<string?>().ToList();
            while (urls.Count < 5) urls.Add(null);

            var model = new IndexViewModel {
                Username = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Bio = user.Bio,
                IsEmailConfirmed = user.EmailConfirmed,
                IsProfilePublic = user.IsProfilePublic,
                AvatarImageId = user.ActiveAvatarImageId,
                HeaderImageId = user.HeaderImageId,
                ExternalUrls = urls,
                StatusMessage = StatusMessage
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IndexViewModel model) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            model.AvatarImageId = user.ActiveAvatarImageId;
            model.HeaderImageId = user.HeaderImageId;

            var urls = (model.ExternalUrls ?? new List<string?>())
                .Select(u => u?.Trim())
                .Where(u => !string.IsNullOrEmpty(u))
                .Cast<string>()
                .ToList();
            if (urls.Count > 5) {
                ModelState.AddModelError(nameof(model.ExternalUrls), "URLは最大5個までです。");
            }
            foreach (var u in urls) {
                if (u.Length > 500 || !Uri.TryCreate(u, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) {
                    ModelState.AddModelError(nameof(model.ExternalUrls), $"URLの形式が正しくありません: {u}");
                }
            }
            if (!ModelState.IsValid) {
                model.ExternalUrls ??= new List<string?>();
                while (model.ExternalUrls.Count < 5) model.ExternalUrls.Add(null);
                return View(model);
            }

            bool isChanged = false;
            var username = user.UserName;
            if (model.Username != username) {
                isChanged = true;
                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Username);
                if (!setUserNameResult.Succeeded) {
                    throw new ApplicationException($"Unexpected error occurred setting username for user with ID '{user.Id}'.");
                }
            }

            var email = user.Email;
            if (model.Email != email) {
                isChanged = true;
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded) {
                    throw new ApplicationException($"Unexpected error occurred setting email for user with ID '{user.Id}'.");
                }
            }

            var phoneNumber = user.PhoneNumber;
            if (model.PhoneNumber != phoneNumber) {
                isChanged = true;
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
                if (!setPhoneResult.Succeeded) {
                    throw new ApplicationException($"Unexpected error occurred setting phone number for user with ID '{user.Id}'.");
                }
            }

            bool isUserModified = false;
            if ((model.Bio ?? string.Empty) != user.Bio) {
                user.Bio = model.Bio ?? string.Empty;
                isUserModified = true;
                isChanged = true;
            }

            if (user.IsProfilePublic != model.IsProfilePublic) {
                user.IsProfilePublic = model.IsProfilePublic;
                isUserModified = true;
                isChanged = true;
            }

            var joinedUrls = string.Join('\n', urls);
            if (user.ExternalUrls != joinedUrls) {
                user.ExternalUrls = joinedUrls;
                isUserModified = true;
                isChanged = true;
            }

            try {
                if (model.RemoveAvatar && user.ActiveAvatarImageId != null) { user.ActiveAvatarImageId = null; isUserModified = isChanged = true; }
                if (model.RemoveHeader && user.HeaderImageId != null) { user.HeaderImageId = null; isUserModified = isChanged = true; }
                if (model.AvatarFile != null && model.AvatarFile.Length > 0) {
                    user.ActiveAvatarImageId = (await SaveProfileImageAsync(user, model.AvatarFile)).Id;
                    isUserModified = isChanged = true;
                }
                if (model.HeaderFile != null && model.HeaderFile.Length > 0) {
                    user.HeaderImageId = (await SaveProfileImageAsync(user, model.HeaderFile)).Id;
                    isUserModified = isChanged = true;
                }
            } catch (Exception ex) {
                ModelState.AddModelError(string.Empty, ex.Message);
                while (model.ExternalUrls.Count < 5) model.ExternalUrls.Add(null);
                return View(model);
            }

            var imageIds = new[] { user.ActiveAvatarImageId, user.HeaderImageId }.Where(i => i.HasValue).Select(i => i!.Value).ToList();
            var profileImages = await _context.Images.Where(i => imageIds.Contains(i.Id)).ToListAsync();
            foreach (var img in profileImages) {
                img.IsPublic = user.IsProfilePublic;
            }

            if (isUserModified) {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded) {
                    throw new ApplicationException($"Unexpected error occurred updating profile for user with ID '{user.Id}'.");
                }
            }

            StatusMessage = isChanged ? _localizer["Manage_Profile_Updated"] : _localizer["Manage_Profile_NoChanges"];
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendVerificationEmail(IndexViewModel model) {
            if (!ModelState.IsValid) {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
            var email = user.Email;
            await _emailSender.SendConfirmationLinkAsync(user, email, callbackUrl);

            StatusMessage = "Verification email sent. Please check your email.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword) {
                return RedirectToAction(nameof(SetPassword));
            }

            var model = new ChangePasswordViewModel { StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model) {
            if (!ModelState.IsValid) {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded) {
                AddErrors(changePasswordResult);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Your password has been changed.";

            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet]
        public async Task<IActionResult> SetPassword() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);

            if (hasPassword) {
                return RedirectToAction(nameof(ChangePassword));
            }

            var model = new SetPasswordViewModel { StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model) {
            if (!ModelState.IsValid) {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!addPasswordResult.Succeeded) {
                AddErrors(addPasswordResult);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            StatusMessage = "Your password has been set.";

            return RedirectToAction(nameof(SetPassword));
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLogins() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var model = new ExternalLoginsViewModel { CurrentLogins = await _userManager.GetLoginsAsync(user) };
            model.OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => model.CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            model.ShowRemoveButton = await _userManager.HasPasswordAsync(user) || model.CurrentLogins.Count > 1;
            model.StatusMessage = StatusMessage;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkLogin(string provider) {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action(nameof(LinkLoginCallback));
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        public async Task<IActionResult> LinkLoginCallback() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null) {
                throw new ApplicationException($"Unexpected error occurred loading external login info for user with ID '{user.Id}'.");
            }

            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded) {
                throw new ApplicationException($"Unexpected error occurred adding external login for user with ID '{user.Id}'.");
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            StatusMessage = "The external login was added.";
            return RedirectToAction(nameof(ExternalLogins));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel model) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var result = await _userManager.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey);
            if (!result.Succeeded) {
                throw new ApplicationException($"Unexpected error occurred removing external login for user with ID '{user.Id}'.");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            StatusMessage = "The external login was removed.";
            return RedirectToAction(nameof(ExternalLogins));
        }

        [HttpGet]
        public async Task<IActionResult> TwoFactorAuthentication() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var model = new TwoFactorAuthenticationViewModel {
                HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null,
                Is2faEnabled = user.TwoFactorEnabled,
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user),
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Disable2faWarning() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            if (!user.TwoFactorEnabled) {
                throw new ApplicationException($"Unexpected error occured disabling 2FA for user with ID '{user.Id}'.");
            }

            return View(nameof(Disable2fa));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable2fa() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2faResult.Succeeded) {
                throw new ApplicationException($"Unexpected error occured disabling 2FA for user with ID '{user.Id}'.");
            }

            _logger.LogInformation("User with ID {UserId} has disabled 2fa.", user.Id);
            return RedirectToAction(nameof(TwoFactorAuthentication));
        }

        [HttpGet]
        public async Task<IActionResult> EnableAuthenticator() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var model = new EnableAuthenticatorViewModel();
            await LoadSharedKeyAndQrCodeUriAsync(user, model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            if (!ModelState.IsValid) {
                await LoadSharedKeyAndQrCodeUriAsync(user, model);
                return View(model);
            }

            // Strip spaces and hypens
            var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid) {
                ModelState.AddModelError("Code", "Verification code is invalid.");
                await LoadSharedKeyAndQrCodeUriAsync(user, model);
                return View(model);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            _logger.LogInformation("User with ID {UserId} has enabled 2FA with an authenticator app.", user.Id);
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            TempData[RecoveryCodesKey] = recoveryCodes.ToArray();

            return RedirectToAction(nameof(ShowRecoveryCodes));
        }

        [HttpGet]
        public IActionResult ShowRecoveryCodes() {
            var recoveryCodes = (string[])TempData[RecoveryCodesKey];
            if (recoveryCodes == null) {
                return RedirectToAction(nameof(TwoFactorAuthentication));
            }

            var model = new ShowRecoveryCodesViewModel { RecoveryCodes = recoveryCodes };
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetAuthenticatorWarning() {
            return View(nameof(ResetAuthenticator));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAuthenticator() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            _logger.LogInformation("User with id '{UserId}' has reset their authentication app key.", user.Id);

            return RedirectToAction(nameof(EnableAuthenticator));
        }

        [HttpGet]
        public async Task<IActionResult> GenerateRecoveryCodesWarning() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            if (!user.TwoFactorEnabled) {
                throw new ApplicationException($"Cannot generate recovery codes for user with ID '{user.Id}' because they do not have 2FA enabled.");
            }

            return View(nameof(GenerateRecoveryCodes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRecoveryCodes() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            if (!user.TwoFactorEnabled) {
                throw new ApplicationException($"Cannot generate recovery codes for user with ID '{user.Id}' as they do not have 2FA enabled.");
            }

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            _logger.LogInformation("User with ID {UserId} has generated new 2FA recovery codes.", user.Id);

            var model = new ShowRecoveryCodesViewModel { RecoveryCodes = recoveryCodes.ToArray() };

            return View(nameof(ShowRecoveryCodes), model);
        }

        [HttpGet]
        public async Task<IActionResult> Options() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.Name ?? "ja";
            if (!currentCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase)) {
                currentCulture = "ja";
            } else {
                currentCulture = "en";
            }

            var colorMode = Request.Cookies["meguri_theme"] ?? "auto";

            var model = new OptionsViewModel {
                Culture = currentCulture,
                ColorMode = colorMode,
                StatusMessage = StatusMessage
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Options(OptionsViewModel model) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            if (!string.IsNullOrEmpty(model.Culture)) {
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(model.Culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax }
                );
            }

            if (!string.IsNullOrEmpty(model.ColorMode)) {
                Response.Cookies.Append(
                    "meguri_theme",
                    model.ColorMode,
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax }
                );
            }

            StatusMessage = _localizer["Manage_Options_Updated"];
            return RedirectToAction(nameof(Options));
        }


        private async Task<Image> SaveProfileImageAsync(ApplicationUser user, IFormFile file) {
            var processed = await _imageProcessingService.ProcessAndOptimizeImageAsync(file);
            var storageKey = $"{user.Id}/{Guid.NewGuid():N}.webp";
            using var uploadStream = new System.IO.MemoryStream(processed.Data);
            await _r2StorageService.UploadFileAsync(uploadStream, storageKey, processed.ContentType);

            var image = new Image {
                UserId = user.Id,
                Name = System.IO.Path.GetFileNameWithoutExtension(file.FileName) + ".webp",
                StorageKey = storageKey,
                ContentType = processed.ContentType,
                FileSize = processed.FileSize,
                Width = processed.Width,
                Height = processed.Height,
                Description = string.Empty,
                Caption = string.Empty,
                IsPublic = user.IsProfilePublic,
                IsSexual = false,
                IsViolence = false
            };
            image.UserImages.Add(new UserImage { UserId = user.Id, Image = image });
            _context.Images.Add(image);
            await _context.SaveChangesAsync();
            return image;
        }

        [HttpGet]
        public async Task<IActionResult> Withdraw() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            return View(new WithdrawViewModel {
                HasPassword = await _userManager.HasPasswordAsync(user)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(WithdrawViewModel model) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return await SignOutAndRedirectToLoginAsync();
            }

            model.HasPassword = await _userManager.HasPasswordAsync(user);
            if (model.HasPassword && (string.IsNullOrEmpty(model.Password) || !await _userManager.CheckPasswordAsync(user, model.Password))) {
                ModelState.AddModelError(nameof(model.Password), _localizer["Manage_Withdraw_InvalidPassword"]);
            }
            if (!ModelState.IsValid) {
                return View(model);
            }

            // 投稿（投稿画像の関連はカスケード削除）
            var posts = await _context.Posts
                .Where(p => p.UserId == user.Id)
                .Include(p => p.PostImages)
                .ThenInclude(pi => pi.Image)
                .ToListAsync();

            // ユーザープロフィール画像
            var userImages = await _context.UserImages
                .Where(ui => ui.UserId == user.Id)
                .Include(ui => ui.Image)
                .ToListAsync();

            // 投稿・プロフィール・ヘッダー・メッセージ添付など、ユーザーがアップロードした全画像
            var images = await _context.Images
                .Where(i => i.UserId == user.Id)
                .ToListAsync();

            var storageKeys = images
                .Where(i => !string.IsNullOrEmpty(i.StorageKey))
                .Select(i => i.StorageKey)
                .ToList();

            user.ActiveAvatarImageId = null;
            user.HeaderImageId = null;
            user.IsProfilePublic = false;
            user.ExternalUrls = string.Empty;
            _context.Posts.RemoveRange(posts);            _context.Images.RemoveRange(images);

            // 論理削除（ユーザー名は残し、メールアドレスは再利用可能にする）
            user.IsWithdrawn = true;
            user.Email = null;
            user.NormalizedEmail = null;
            user.EmailConfirmed = false;
            user.PhoneNumber = null;
            user.PhoneNumberConfirmed = false;
            user.Bio = string.Empty;
            user.PasswordHash = null;
            user.TwoFactorEnabled = false;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) {
                AddErrors(result);
                return View(model);
            }

            foreach (var login in await _userManager.GetLoginsAsync(user)) {
                await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
            }
            var claims = await _userManager.GetClaimsAsync(user);
            if (claims.Count > 0) {
                await _userManager.RemoveClaimsAsync(user, claims);
            }
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count > 0) {
                await _userManager.RemoveFromRolesAsync(user, roles);
            }
            await _userManager.RemoveAuthenticationTokenAsync(user, "[AspNetUserStore]", "AuthenticatorKey");
            await _userManager.RemoveAuthenticationTokenAsync(user, "[AspNetUserStore]", "RecoveryCodes");

            foreach (var key in storageKeys) {
                try {
                    await _r2StorageService.DeleteFileAsync(key);
                } catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to delete storage file '{Key}' while withdrawing user '{UserId}'.", key, user.Id);
                }
            }

            try {
                await _r2StorageService.DeleteByPrefixAsync($"{user.Id}/");
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to delete storage folder for user '{UserId}'.", user.Id);
            }

            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User with ID '{UserId}' withdrew.", user.Id);
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        #region Helpers

        private void AddErrors(IdentityResult result) {
            foreach (var error in result.Errors) {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private string FormatKey(string unformattedKey) {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length) {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length) {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey) {
            return string.Format(
                AuthenticatorUriFormat,
                _urlEncoder.Encode("Meguri"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user, EnableAuthenticatorViewModel model) {
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey)) {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            model.SharedKey = FormatKey(unformattedKey);
            model.AuthenticatorUri = GenerateQrCodeUri(user.Email, unformattedKey);
        }

        #endregion
    }
}

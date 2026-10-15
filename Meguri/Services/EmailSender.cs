using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading.Tasks;
using Meguri.Models;

namespace Meguri.Services;

// .NET 標準の IEmailSender<TUser> インターフェースを実装
public class EmailSender : IEmailSender<ApplicationUser> {

    private readonly ILogger _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public EmailSender(
        IOptions<SMTPServerConf> optionsAccessor,
        ILogger<EmailSender> logger,
        IStringLocalizer<SharedResource> localizer
    ) {
        Options = optionsAccessor.Value;
        _logger = logger;
        _localizer = localizer;
    }

    public SMTPServerConf Options { get; }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) {
        var textMessage = string.Format(_localizer["Email_ConfirmAccount_Body"].Value, confirmationLink);
        await Execute(_localizer["Email_ConfirmAccount_Subject"].Value, textMessage, email);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) {
        var textMessage = string.Format(_localizer["Email_ResetPassword_Body"].Value, resetLink);
        await Execute(_localizer["Email_ResetPassword_Subject"].Value, textMessage, email);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) {
        var textMessage = string.Format(_localizer["Email_ResetPasswordCode_Body"].Value, resetCode);
        await Execute(_localizer["Email_ResetPasswordCode_Subject"].Value, textMessage, email);
    }

    public async Task Execute(string subject, string message, string toEmail) {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(
            Options.FromName, Options.Account)
        );
        mimeMessage.To.Add(new MailboxAddress(toEmail, toEmail));
        mimeMessage.Subject = subject;

        // プレーンテキスト形式で本文を設定
        mimeMessage.Body = new TextPart("plain") {
            Text = message
        };

        try {
            using (var client = new SmtpClient()) {
                client.LocalDomain = Options.LocalDomain;
                await client.ConnectAsync(
                    Options.HostName, Options.Port, SecureSocketOptions.SslOnConnect
                );

                // 認証方式を PLAIN または LOGIN にする。
                client.AuthenticationMechanisms.Clear();
                client.AuthenticationMechanisms.Add("PLAIN");
                client.AuthenticationMechanisms.Add("LOGIN");

                await client.AuthenticateAsync(
                    Options.Account, Options.Password
                );
                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);
            }
        } catch (Exception ex) {
            _logger.LogError(
                "Failure Email to {ToEmail}. {ExMessage}", toEmail, ex.Message
            );
            return;
        }
        _logger.LogInformation(
            "Email to {ToEmail} queued successfully!", toEmail
        );
    }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
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

    public EmailSender(
        IOptions<SMTPServerConf> optionsAccessor, ILogger<EmailSender> logger
    ) {
        Options = optionsAccessor.Value;
        _logger = logger;
    }

    public SMTPServerConf Options { get; }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) {
        var textMessage = $"以下のリンクをクリックして、アカウントを確定してください。\n\n{confirmationLink}";
        await Execute("アカウントの確認", textMessage, email);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) {
        var textMessage = $"以下のリンクをクリックして、パスワードをリセットしてください。\n\n{resetLink}";
        await Execute("パスワードのリセット", textMessage, email);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) {
        var textMessage = $"リセットコードは次の通りです:\n\n{resetCode}";
        await Execute("パスワードリセットコード", textMessage, email);
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

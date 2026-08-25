using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Servit.Api.Services;

public class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var host = configuration["Smtp:Host"]
            ?? throw new InvalidOperationException("Smtp:Host is not configured. Run 'dotnet user-secrets set Smtp:Host <value>'.");
        var port = int.Parse(configuration["Smtp:Port"]
            ?? throw new InvalidOperationException("Smtp:Port is not configured. Run 'dotnet user-secrets set Smtp:Port <value>'."));
        var user = configuration["Smtp:User"]
            ?? throw new InvalidOperationException("Smtp:User is not configured. Run 'dotnet user-secrets set Smtp:User <value>'.");
        var password = configuration["Smtp:Password"]
            ?? throw new InvalidOperationException("Smtp:Password is not configured. Run 'dotnet user-secrets set Smtp:Password <value>'.");
        var fromEmail = configuration["Smtp:FromEmail"] ?? user;
        var fromName = configuration["Smtp:FromName"] ?? "Servit";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient { CheckCertificateRevocation = false };
        await client.ConnectAsync(host, port, SecureSocketOptions.Auto);
        await client.AuthenticateAsync(user, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

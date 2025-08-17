using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using TCBackend.Model;
using TCBackend.Data;

namespace TCBackend.Services
{
    public class EmailService /*: IEmailService*/
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;
        private readonly TCDbContext _context;
        public EmailService(IOptions<EmailSettings> emailSettings, TCDbContext context)
        {
            _smtpHost = emailSettings.Value.SmtpHost;
            _smtpPort = emailSettings.Value.SmtpPort;
            _smtpUser = emailSettings.Value.SmtpUser;
            _smtpPassword = emailSettings.Value.SmtpPassword;
            _context = context;
        }
        
        public async Task SendEmailAsync(string toEmail, string ccEmail, string subject, string body)
        {
            using (var mail = new MailMessage())
            {
                mail.From = new MailAddress("Copal@colorplast.in");
                // mail.To.Add(toEmail);
                if (!string.IsNullOrEmpty(toEmail))
                {
                    mail.To.Add(toEmail);
                }
                

                if (!string.IsNullOrEmpty(ccEmail))
                {
                    mail.CC.Add(ccEmail);
                }

                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    smtp.Host = _smtpHost;
                    smtp.Port = _smtpPort;
                    smtp.EnableSsl = true;
                    smtp.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);
                    await smtp.SendMailAsync(mail);
                }
            }
        }

        

    }
}

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TCBackend.Model
{
    [Keyless]
    public class EmailSettings
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPassword { get; set; }
    }
    public class EmailAddressDetails
    {
        [Key]
        public int? MailProcessID { get; set; } // Primary key
        public List<string> ToMail { get; set; }
        public List<string> CcMail { get; set; }
        public List<string> GroupMail { get; set; }
        public List<string> HODMail { get; set; }
        public List<string> RMMail { get; set; }
    }
    public class EmailAddressResponse
    {
        public string? ToMail { get; set; }
        public string? CcMail { get; set; }
        public string? GroupMail { get; set; }
        public string? HODMail { get; set; }
        public string? RMMail { get; set; }
    }
}

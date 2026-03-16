using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TCBackend.Dtos.Home
{
    [Keyless]
    public class GenericDropdownDto
    {
        public string? Value { get; set; }
        public string? Text { get; set; }
        public string? ExtraData1 { get; set; }
    }

    public class StatusMasterDto
    {
        [Key]
        public int StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? StatusSH { get; set; }
    }

}

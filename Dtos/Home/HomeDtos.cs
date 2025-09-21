using Microsoft.EntityFrameworkCore;

namespace TCBackend.Dtos.Home
{
    [Keyless]
    public class GenericDropdownDto
    {
        public string? Value { get; set; }
        public string? Text { get; set; }
        public string? ExtraData1 { get; set; }
    }
}   

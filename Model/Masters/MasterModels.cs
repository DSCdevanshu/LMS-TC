using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TCBackend.Model.Masters
{
    [Table("LocationMaster")]
    public class LocationMaster
    {
        [Key]
        public int LocationId { get; set; }
        public string? LocationName { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Coordinates { get; set; }
    }

    [Table("CompanyMaster")]
    public class CompanyMaster
    {
        [Key]
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyCode { get; set; }
    }
}

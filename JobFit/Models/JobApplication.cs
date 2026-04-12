using System.ComponentModel.DataAnnotations;

namespace JobFit.Models
{
    // One row of the Applications table.
    public class JobApplication
    {
        public static readonly string[] Statuses = { "Applied", "Interview", "Offer", "Rejected" };

        public int AppID { get; set; }

        [Required, StringLength(100)]
        public string Company { get; set; } = "";

        [Required, StringLength(100)]
        public string Role { get; set; } = "";

        [StringLength(100)]
        public string? Location { get; set; }

        [Required, DataType(DataType.Date), Display(Name = "Applied on")]
        public DateTime AppliedDate { get; set; } = DateTime.Today;

        [Required]
        public string Status { get; set; } = "Applied";

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}

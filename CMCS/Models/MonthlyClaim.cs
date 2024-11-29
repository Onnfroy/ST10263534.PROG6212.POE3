using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS.Models
{
    public class MonthlyClaim
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string LecturerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string LecturerEmail { get; set; } = string.Empty;

        [Phone]
        public string LecturerPhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Department { get; set; } = string.Empty; // Dropdown values

        [Required]
        public double HoursWorked { get; set; }

        [Required]
        public double HourlyRate { get; set; }

        public string? Notes { get; set; } // Optional

        public string Status { get; set; } = "Pending";

        [ForeignKey("UploadedDocument")]
        public int? DocumentId { get; set; }

        public UploadedDocument? Document { get; set; }
    }
}
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
        public string LecturerName { get; set; } = string.Empty; // Ensure non-null initialization

        [Required]
        public double HoursWorked { get; set; }

        [Required]
        public double HourlyRate { get; set; }

        public string? Notes { get; set; } // Optional field

        public string Status { get; set; } = "Pending";

        [ForeignKey("UploadedDocument")]
        public int? DocumentId { get; set; }

        public UploadedDocument? Document { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS.Models
{
    public class MonthlyClaim
    {
        [Key]
        public int Id { get; set; } // Primary key

        [Required]
        [MaxLength(100)]
        public string LecturerName { get; set; } = string.Empty; // Name of the lecturer

        [Required]
        [EmailAddress]
        public string LecturerEmail { get; set; } = string.Empty; // Email of the lecturer

        [Phone]
        public string LecturerPhoneNumber { get; set; } = string.Empty; // Phone number of the lecturer

        [Required]
        public string Department { get; set; } = string.Empty; // Department of the lecturer, dropdown values

        [Required]
        public double HoursWorked { get; set; } // Number of hours worked

        [Required]
        public double HourlyRate { get; set; } // Hourly rate of the lecturer

        public string? Notes { get; set; } // Optional notes

        public string Status { get; set; } = "Pending"; // Status of the claim, default is "Pending"

        [ForeignKey("UploadedDocument")]
        public int? DocumentId { get; set; } // Foreign key to the uploaded document

        public UploadedDocument? Document { get; set; } // Navigation property to the uploaded document
    }
}
using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    // Define the UploadedDocument class
    public class UploadedDocument
    {
        // Primary key for the UploadedDocument table
        [Key]
        public int Id { get; set; }

        // Name of the uploaded file, required field
        [Required]
        public string FileName { get; set; } = string.Empty;

        // Path where the uploaded file is stored, required field
        [Required]
        public string FilePath { get; set; } = string.Empty;
    }
}
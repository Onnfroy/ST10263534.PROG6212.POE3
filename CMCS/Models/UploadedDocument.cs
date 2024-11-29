using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class UploadedDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;
    }
}
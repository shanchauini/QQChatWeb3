using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QQChatWeb3.Models
{
    public class ChatFile
    {
        [Key]
        public int FileId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadDate { get; set; } = DateTime.Now;
    }
} 
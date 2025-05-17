using System.ComponentModel.DataAnnotations;

namespace QQChatWeb3.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PSW { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = false;
        public bool IsBanned { get; set; } = false;

        [StringLength(10)]
        public string? Gender { get; set; }

        [EmailAddress]
        [StringLength(50)]
        public string? Email { get; set; }
    }
} 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QQChatWeb3.Models
{
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        public int SendId { get; set; }
        public int ReceiveId { get; set; }

        [Required]
        [StringLength(20)]
        public string ReceiveType { get; set; } = "普通用户";

        public string? Content { get; set; }

        public DateTime SendTime { get; set; } = DateTime.Now;

        public int? FileId { get; set; }

        [StringLength(10)]
        public string? MessageType { get; set; }

        [ForeignKey("SendId")]
        public User? Sender { get; set; }

        [ForeignKey("FileId")]
        public ChatFile? File { get; set; }
    }
} 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QQChatWeb3.Models
{
    public class FriendRequest
    {
        [Key]
        public int RequestId { get; set; }

        public int SendId { get; set; }
        public int ReceiveId { get; set; }

        [StringLength(20)]
        public string RequestStatus { get; set; } = "申请中";

        public DateTime RequestDate { get; set; } = DateTime.Now;

        [ForeignKey("SendId")]
        public User? Sender { get; set; }

        [ForeignKey("ReceiveId")]
        public User? Receiver { get; set; }
    }
} 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QQChatWeb3.Models
{
    public class Friendship
    {
        [Key]
        public int FriendShipId { get; set; }

        public int UserId1 { get; set; }
        public int UserId2 { get; set; }

        public DateTime CreateDate { get; set; } = DateTime.Now;

        [ForeignKey("UserId1")]
        public User? User1 { get; set; }

        [ForeignKey("UserId2")]
        public User? User2 { get; set; }
    }
} 
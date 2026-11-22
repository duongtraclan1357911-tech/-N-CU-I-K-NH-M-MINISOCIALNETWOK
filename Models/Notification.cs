using MiniSocialMedia.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MiniSocialMedia.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int SenderId { get; set; }

        public int? PostId { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        // Người nhận thông báo
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // Người gửi thông báo
        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; }

        // Bài viết liên quan
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; }
    }
}
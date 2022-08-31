using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramExchangeDB.Models
{
    internal class VideoProduct
    {
        public VideoProduct(string uniqId, long userChatId)
        {
            UniqId = uniqId;
            UserChatId = userChatId;
        }

        [Key]
        public string UniqId { get; set; } = null!;
        public long StorageMessageId { get; set; }
        public long StorageId { get; set; }
        public long UserChatId { get; set; }
    }
}

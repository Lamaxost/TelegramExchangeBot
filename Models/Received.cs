using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramExchangeDB.Models
{
    internal class Received
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id;

        public long ChatId;
        public string VideoId = null!;
        public Received()
        {
            
        }
        public Received(long chatId, string videoId)
        {
            ChatId = chatId;
            VideoId = videoId;
        }
    }
}

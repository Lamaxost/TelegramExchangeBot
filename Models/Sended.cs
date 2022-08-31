using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramExchangeDB.Models
{
    internal class Sended
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id;

        public long ChatId;
        public string VideoId;
        public Sended()
        {
        }
        public Sended(long chatId, string videoId)
        {
            ChatId = chatId;
            VideoId = videoId;
        }
    }
}

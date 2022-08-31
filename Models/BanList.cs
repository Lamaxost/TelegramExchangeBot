using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramExchangeDB.Models
{
    internal class Ban
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public Ban(long chatId)
        {
            ChatId = chatId;
        }
    }
}

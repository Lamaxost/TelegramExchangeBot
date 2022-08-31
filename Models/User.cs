using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramExchangeDB.Models
{
    public enum UserStates
    {
        Idle,
        Exchanging,
        Clouding,
        Messaging,
    }
    internal class User
    {
        public User(long chatId)
        {
            ChatId = chatId;
            State = UserStates.Idle;
            ExchgangingCount = 0;
            AverageServerDublicatesCount = 0;
        }

        [Key]
        public long ChatId { get; set; }
        public int ExchgangingCount { get; set; }
        public int AverageServerDublicatesCount { get; set; }
        public int ReceivedCount { get; set; }
        public List<VideoProduct> VideoProducts { get; set; } =  new();
        public List<Received> VideoProductsReceived { get; set; } = new();
        public List<Sended> VideoProductsSended { get; set; } = new();
        public UserStates State { get; set; }

    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TelegramExchangeDB;


using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace TelegramEchangeNew
{
    public class ProductionBot:IDisposable
    {
        private IConfigurationRoot config;
        private TelegramBotClient botClient;

        private ContextFactory ctxFactory;
        private ApplicationContext db;

        private List<long> adminsChatIds;
        private List<long> buffersChatIds;

        private IConfigurationSection texts;
        private bool disposedValue;

        private Queue<Tuple<TelegramExchangeDB.Models.VideoProduct, int>> queueToBuffers;
        private List<Tuple<TelegramExchangeDB.Models.VideoProduct, long>> usersWaitForVideoAnswer;

        private List<TelegramExchangeDB.Models.User> ExchanginUsers;

        ApplicationContext buffersDb;
        private Timer buffersTimer;
        ApplicationContext answersDb;
        private Timer answerTimer;

        public async Task Start()
        {

            using var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };
            buffersTimer = new System.Threading.Timer(
                async e => await HandleQueueToBuffers(),
                null,
                0,
                3200);
            answerTimer = new System.Threading.Timer(
                async e => await AnswerVideosFromBuffers(),
                null,
                0,
                500);
            botClient.StartReceiving(
                updateHandler: UpdateHandler,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
                );
            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            // Send cancellation request to stop bot
            cts.Cancel();
        }

        public ProductionBot()
        {

            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            builder.AddJsonFile("textConfig.json");
            config = builder.Build();

            adminsChatIds = config.GetSection("AdminChatIds").Get<List<long>>();
            buffersChatIds =config.GetSection("StorageChatIds").Get<List<long>>();

            ctxFactory = new ContextFactory();
            db = ctxFactory.CreateDbContext(new string[0]);
            buffersDb = ctxFactory.CreateDbContext(new string[0]);
            answersDb = ctxFactory.CreateDbContext(new string[0]);

            ExchanginUsers = new List<TelegramExchangeDB.Models.User>();

            texts = config.GetSection("BotTexts");

            var botToken = config["BotToken"];
            botClient = new TelegramBotClient(botToken);

            queueToBuffers = new Queue<Tuple<TelegramExchangeDB.Models.VideoProduct, int>>();
            usersWaitForVideoAnswer = new List<Tuple<TelegramExchangeDB.Models.VideoProduct, long>>();
            
        }

        private async Task AnswerVideosFromBuffers()
        {

            var answers = new List<Tuple<TelegramExchangeDB.Models.VideoProduct, long>>();
            lock (usersWaitForVideoAnswer)
            {
                if (usersWaitForVideoAnswer.Count == 0)
                {
                    return;
                }
                for(int i = 0; i < usersWaitForVideoAnswer.Count; i++)
                {
                    if (answers.Select(a => a.Item2).Contains(usersWaitForVideoAnswer[i].Item2))
                    {
                        continue;
                    }
                    answers.Add(usersWaitForVideoAnswer[i]);
                    usersWaitForVideoAnswer.RemoveAt(i);
                }
               
            }
            answersDb.Dispose();
            answersDb = ctxFactory.CreateDbContext(new string[0]);
            var banList = answersDb.BanList.Select(b => b.ChatId);
            
            foreach(var answer in answers){
                var chatId = answer.Item2;
                if (banList.Contains(chatId))
                {
                    break;
                }
                var video = answer.Item1;
                try
                {

                    
                    var exchangedForwardedVideo = await botClient.CopyMessageAsync(chatId, video.StorageId, (int)video.StorageMessageId, caption: "");
                    Console.WriteLine(video.StorageMessageId + " from storage to user " + chatId);
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    continue;
                }
            }
            
        }

        private async Task HandleQueueToBuffers()
        {
            var tuples = new List<Tuple<TelegramExchangeDB.Models.VideoProduct, int>>();
            lock (queueToBuffers)
            {
                if (queueToBuffers.Count == 0)
                {
                    return;
                }
                Console.WriteLine(queueToBuffers.Count);
                foreach (var item in buffersChatIds)
                {
                    if (queueToBuffers.Count == 0)
                    {
                        break;
                    }

                    tuples.Add(queueToBuffers.Dequeue());
                }
            }
            buffersDb.Dispose();
            buffersDb = ctxFactory.CreateDbContext(new string[0]);
            var banList = buffersDb.BanList.Select(b => b.ChatId);
            for (int i=0; i < tuples.Count; i++)
            {
                var videoProduct = tuples[i].Item1;
                var messageId = tuples[i].Item2;
                if (banList.Contains(videoProduct.UserChatId))
                {
                    break;
                }
                var storageMessage = await botClient.CopyMessageAsync(buffersChatIds[i], videoProduct.UserChatId, messageId, caption: videoProduct.UserChatId.ToString());

                Console.WriteLine(videoProduct.UserChatId + " from to storage " + storageMessage.Id);
                videoProduct.StorageMessageId = storageMessage.Id;
                videoProduct.StorageId = buffersChatIds[i];
               
                var userModel = buffersDb.Users.Find(videoProduct.UserChatId);
                userModel.VideoProducts.Add(videoProduct);
                var sended = new TelegramExchangeDB.Models.Sended(videoProduct.UserChatId, videoProduct.UniqId);
                userModel.VideoProductsSended.Add(sended);
                
            }
            buffersDb.SaveChanges();
            return;

        }
        private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;

            // Only process text messages


            var chatId = message.Chat.Id;
            var userModel = db.Users.Find(chatId);



            if (userModel == null)
            {
                userModel = new TelegramExchangeDB.Models.User(chatId);
                db.Users.Add(userModel);
            }
            db.SaveChanges();
            var banList = db.BanList.Select(ban => ban.ChatId);
            if (banList.Contains(chatId))
            {
                var msg = await botClient.SendTextMessageAsync(chatId, texts["YouAreBanned"]);
                return;
            }
            try
            {
                if (message.Text is { })
                {
                    await TextCommandsHandler(update, cancellationToken);
                }
                else if (message.Video is { })
                {
                    await VideosHandler(update, cancellationToken);
                }
            }
            catch (ApiRequestException apiRequestException)
            {
                if (apiRequestException.ErrorCode == 403)
                {
                    Console.WriteLine($"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}");
                }
                else
                {
                    throw apiRequestException;
                }
            }
        }

        private async Task TextCommandsHandler(Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;
            if (message.Text is not { } messageText)
                return;
            long chatId = message.Chat.Id;

            var userModel = db.Users.Find(chatId);
            try
            {
                if (messageText == "/test")
                {
                    Console.WriteLine(chatId);
                }
                else if (messageText == "/start" || messageText == "/help")
                {
                    Console.WriteLine(chatId + " started");
                    await StartOrCreateCommand(chatId, cancellationToken);

                    db.SaveChanges();
                }
                else if (messageText == "/exchange")
                {
                    await ExchangeCommand(chatId, cancellationToken);
                }
                else if (messageText == "/endexchange")
                {
                    await EndExchangeCommand(chatId, cancellationToken);
                }
                else if (messageText.Contains("/ban") && adminsChatIds.Contains(chatId))
                {

                    long bannedId = long.Parse(messageText.Split(' ')[1]);
                    BanUser(bannedId);
                    await botClient.SendTextMessageAsync(chatId, bannedId + " Baned");
                }
                else if (messageText.Contains("/unban") && adminsChatIds.Contains(chatId))
                {
                    long unbannedId = long.Parse(messageText.Split(' ')[1]);
                    UnBanUser(unbannedId);
                    await botClient.SendTextMessageAsync(chatId, unbannedId + " Unbaned");
                }
                else if (messageText.Contains("/give") && adminsChatIds.Contains(chatId))
                {
                    long userId = long.Parse(messageText.Split(' ')[1]);
                    int count = int.Parse(messageText.Split(' ')[2]);
                    Give(userId, count);
                    await botClient.SendTextMessageAsync(chatId, userId + " Baned");
                }
                else if (messageText.Contains("/delete") && adminsChatIds.Contains(chatId))
                {
                    long bannedId = long.Parse(messageText.Split(' ')[1]);
                    await DeleteUserVideos(bannedId);
                    await botClient.SendTextMessageAsync(chatId, bannedId + " Videos Deleted");
                }
                else if (messageText == "/cloud")
                {
                    await CloudCommand(chatId, cancellationToken);
                }
                else if (messageText.StartsWith("/message") && adminsChatIds.Contains(chatId))
                {
                    userIdMessageTo = long.Parse(messageText.Split(' ')[1]);
                    await MessageToUser(chatId, cancellationToken);
                }
                else
                {
                    if (userModel.State == TelegramExchangeDB.Models.UserStates.Clouding)
                    {
                        foreach (var adminChatId in adminsChatIds)
                        {
                            try
                            {
                                await botClient.SendTextMessageAsync(adminChatId, messageText + "\n\n" + chatId);
                                userModel.State = TelegramExchangeDB.Models.UserStates.Idle;
                                await botClient.SendTextMessageAsync(chatId, "Message Sended");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                            }
                        }

                    }
                    else if (userModel.State == TelegramExchangeDB.Models.UserStates.Messaging)
                    {
                        if (userIdMessageTo == -1)
                        {
                            var banlist = db.BanList.Select(b => b.ChatId).ToList();
                            var users = db.Users.Where(u => !banlist.Contains(u.ChatId)).Where(u => !buffersChatIds.Contains(u.ChatId)).Where(u => !adminsChatIds.Contains(u.ChatId));
                            Console.WriteLine(users.Count());
                            foreach (var user in users)
                            {
                                try
                                {

                                    await botClient.SendTextMessageAsync(user.ChatId, messageText + "\n\n" + texts["MessageFromAdmin"], parseMode: ParseMode.Html);
                                    userModel.State = TelegramExchangeDB.Models.UserStates.Idle;
                                    await botClient.SendTextMessageAsync(chatId, "Message Sended");
                                    Thread.Sleep(600);
                                }
                                catch (Exception e)
                                {
                                    if (e is ApiRequestException)
                                    {
                                        Console.WriteLine(user.ChatId + " - blocked");
                                    }
                                    else
                                    {
                                        Console.WriteLine(e.ToString());
                                    }

                                }
                            }

                        }
                        else
                        {
                            try
                            {
                                await botClient.SendTextMessageAsync(userIdMessageTo, messageText + "\n\n" + texts["MessageFromAdmin"], parseMode: ParseMode.Html);
                                userModel.State = TelegramExchangeDB.Models.UserStates.Idle;
                                await botClient.SendTextMessageAsync(chatId, "Message Sended");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                            }
                        }

                    }
                    else
                    {
                        string sendMessageText = texts["UndefinedCommand"];
                        Message sentMessage = await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: sendMessageText,
                        cancellationToken: cancellationToken
                        );
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private async Task CloudCommand(long chatId, CancellationToken cancellationToken)
        {
            var userModel = db.Users.Find(chatId);
            userModel.State = TelegramExchangeDB.Models.UserStates.Clouding;
            db.SaveChanges();
            
            await botClient.SendTextMessageAsync(chatId: chatId, text: texts["EnterYourCloudSuggestion"], cancellationToken:cancellationToken);
        }
        long userIdMessageTo;
        private async Task MessageToUser(long chatId, CancellationToken cancellationToken)
        {
            var userModel = db.Users.Find(chatId);
            userModel.State = TelegramExchangeDB.Models.UserStates.Messaging;
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Enter your message for user", cancellationToken: cancellationToken);
        }

        private async Task VideosHandler(Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
            {
                return;
            }
            if (message.Video is not { } video)
            {
                return;
            }

            long chatId = message.Chat.Id;
            var userModel= ExchanginUsers.Find(u => u.ChatId == chatId);
            if (userModel== null)
            {
                userModel = db.Users.Include(u => u.VideoProducts).Include(u => u.VideoProductsReceived).Include(u => u.VideoProductsSended).FirstOrDefault(u => u.ChatId == chatId);
            }
            
            if (userModel.State != TelegramExchangeDB.Models.UserStates.Exchanging)
            {
                var sendMessageText = texts["NotStartedExchange"];
                Message NoMoreVideoMessage = await botClient.SendTextMessageAsync(chatId, sendMessageText);
                return;
            }
            if (userModel.VideoProductsSended.Any(v => v.VideoId == video.FileUniqueId) ||
                userModel.VideoProducts.Any(v => v.UniqId == video.FileUniqueId))
            {
                var messageText = texts["VideoRepeat"];
                Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: userModel.ChatId,
                text: messageText,
                cancellationToken: cancellationToken,
                replyToMessageId: message.MessageId
                );
                return;
            }
            if (userModel.VideoProductsReceived.Any(r => r.VideoId == video.FileUniqueId))
            {
                var messageText = texts["WeSendYouThisVideo"];
                Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: userModel.ChatId,
                text: messageText,
                cancellationToken: cancellationToken,
                replyToMessageId: message.MessageId
                );
                return;
            }
            userModel.ExchgangingCount += 1;
            var videoSended = new TelegramExchangeDB.Models.Sended(chatId, video.FileUniqueId);
            
            userModel.VideoProductsSended.Add(videoSended);
            var videoProduct = db.VideoProducts.FirstOrDefault(v => v.UniqId == video.FileUniqueId);
            if (videoProduct is not null)
            {
                userModel.AverageServerDublicatesCount += 1;
                db.SaveChanges();
                return;
            }

            videoProduct = new TelegramExchangeDB.Models.VideoProduct(video.FileUniqueId, userModel.ChatId);


            lock (queueToBuffers)
            {
                queueToBuffers.Enqueue(new Tuple<TelegramExchangeDB.Models.VideoProduct, int>(videoProduct, message.MessageId));
            }
            if (db.VideoProducts.Count() < userModel.VideoProducts.Count() + userModel.ExchgangingCount)
            {
                var sendMessageText = texts["NoMoreVideo"];

                Message NoMoreVideoMessage = await botClient.SendTextMessageAsync(chatId, sendMessageText);
            }
        }

        private async Task DeleteUserVideos(long bannedId)
        {
            var videos = db.VideoProducts.Where(v => v.UserChatId == bannedId);
            Console.WriteLine(videos.Count());
            foreach (var video in videos)
            {
                try
                {
                    await botClient.DeleteMessageAsync(video.StorageId, (int)video.StorageMessageId);
                    Console.WriteLine(video.StorageMessageId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            db.VideoProducts.RemoveRange(videos);
            db.SaveChanges();
        }

        private void BanUser(long chatId)
        {
            var ban = new TelegramExchangeDB.Models.Ban(chatId);
            db.BanList.Add(ban);
            db.SaveChanges();
        }
        private void UnBanUser(long chatId)
        {
            var ban = db.BanList.Find(chatId);
            if (ban != null)
            {
                db.BanList.Remove(ban);
                db.SaveChanges();
            }
        }
        private void Give(long chatId, int count)
        {
            var user = db.Users.Find(chatId);
            if(user != null)
            {
                user.ExchgangingCount = count;
                db.SaveChanges();
            }
        }
        private async Task ExchangeCommand(long chatId, CancellationToken cancellationToken)
        {
            Message sentMessage1 = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: texts["PreparingForExhange"],
                cancellationToken: cancellationToken);
            var userModel = db.Users.Include(u => u.VideoProducts).Include(u => u.VideoProductsReceived).Include(u => u.VideoProductsSended).FirstOrDefault(u => u.ChatId == chatId);
            userModel.State = TelegramExchangeDB.Models.UserStates.Exchanging;
            var userChache = ExchanginUsers.Find(u => u.ChatId==chatId);
            if (userChache == null)
            {
                ExchanginUsers.Add(userModel);
            }
            var sendMessageText = texts["ExchangeCommand"];
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: userModel.ChatId,
                text: sendMessageText,
                cancellationToken: cancellationToken);
        }

        private async Task EndExchangeCommand(long chatId, CancellationToken cancellationToken)
        {
            var userModel = db.Users.Include(u => u.VideoProducts).FirstOrDefault(u => u.ChatId == chatId);
            Console.WriteLine(userModel.ExchgangingCount);
            if (userModel.State != TelegramExchangeDB.Models.UserStates.Exchanging)
            {
                var sendMessageText = texts["NotStartedExchange"];

                Message NoMoreVideoMessage = await botClient.SendTextMessageAsync(userModel.ChatId, sendMessageText);
                return;
            }
            if (userModel.ExchgangingCount == 0)
            {
                var sendMessageText = texts["NoOneVideoSended"];
                Message NoMoreVideoMessage = await botClient.SendTextMessageAsync(userModel.ChatId, sendMessageText);
                return;
            }
            var userModelVideosUniqIds = userModel.VideoProducts.Select(v => v.UniqId).ToList();
            var banList = db.BanList.Select(ban => ban.ChatId);
            var answerVideos = db.VideoProducts
                .Where(v => !banList.Contains(v.UserChatId))
                .Where(v => v.UserChatId != userModel.ChatId)
                .Where(v => !userModelVideosUniqIds.Contains(v.UniqId))
                .Skip(userModel.ReceivedCount)
                .Take(userModel.ExchgangingCount)

                ;
            if (answerVideos.Count() <= 0)
            {
                var sendMessageText = texts["CantSendFiles"];

                Message noMoreVideoMessage = await botClient.SendTextMessageAsync(userModel.ChatId, sendMessageText);
                return;
            }

            var savedCount = userModel.ExchgangingCount;
            foreach (var video in answerVideos)
            {
                try
                {

                    if (banList.Contains(userModel.ChatId))
                    {
                        break;
                    }

                    lock (usersWaitForVideoAnswer)
                    {
                        usersWaitForVideoAnswer.Add(new Tuple<TelegramExchangeDB.Models.VideoProduct, long>(video,userModel.ChatId));
                    }
                    var recived = new TelegramExchangeDB.Models.Received(chatId, video.UniqId);
                    userModel.VideoProductsReceived.Add(recived);
                    userModel.ExchgangingCount -= 1;
                    userModel.ReceivedCount += 1;
                    if (userModel.ExchgangingCount <= 0)
                    {
                        ExchanginUsers.Remove(userModel);
                    }
                    answersDb.SaveChanges();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + "\n" +
                        e.StackTrace);
                }

            }
            if (userModel.ExchgangingCount > 0)
            {
                var sendMessageText = texts["WeSentButNotHaveMore"];
                Message SendMessage = await botClient.SendTextMessageAsync(userModel.ChatId, sendMessageText);
                return;
            }
            userModel.State = TelegramExchangeDB.Models.UserStates.Idle;
            userModel.ExchgangingCount = 0;
            db.SaveChanges();
        }

        private async Task StartOrCreateCommand(long chatId, CancellationToken cancellationToken)
        {
            var userModel = db.Users.Find(chatId);
            userModel.State = TelegramExchangeDB.Models.UserStates.Idle;
            
            var messageText = texts["StartAndHelp"];
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: userModel.ChatId,
                text: messageText,
                cancellationToken: cancellationToken);
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",

                _ => exception.ToString()
            };
            Console.WriteLine(exception.StackTrace);
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    db.Dispose();
                    answersDb.Dispose();
                    buffersDb.Dispose();
                    answerTimer.Dispose();
                    buffersTimer.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ProductionBot()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

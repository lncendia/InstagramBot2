using System;
using System.Threading;
using System.Threading.Tasks;
using InstagramApiSharp;
using InstagramApiSharp.API;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Timer = System.Timers.Timer;

namespace Insta
{
    public class Work
    {
        public int Id { get; }
        private IInstaApi Api { get; }
        public string Hashtag { get; set; }
        private int Duration { get; set; }
        private Timer Timer { get; set; }
        private User Owner { get; set; }
        public readonly CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
        public Work(int id, IInstaApi api, User user)
        {
            Id = id;
            Api = api;
            Owner = user;
        }

        public void SetHashtag(string hashtag)
        {
            Hashtag = hashtag;
        }

        public void SetDuration(int duration)
        {
            Duration = duration;
        }

        public string GetUsername()
        {
            return Api.GetLoggedUser().UserName;
        }
        public void StartAtTime(TimeSpan time)
        {
            Timer = new Timer(time.TotalMilliseconds) {Enabled = true};
            Timer.Elapsed += Timer_Elapsed;
        }

        public void TimerDispose()
        {
            try
            {
                Timer.Dispose();
                CancelTokenSource.Cancel();
                SendMessageStop(true);
            }
            catch
            {
                SendMessageStop(false,message:"Timer stop failed");
            }
        }
        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Timer.Dispose();
            await Task.Run(Start);
        }
        public bool IsStarted { get; set; }
        public async void Start()
        {
            try
            {
                IsStarted = true;
                SendMessageStart();
                var posts = await Api.HashtagProcessor.GetRecentHashtagMediaListAsync(Hashtag,
                    PaginationParameters.MaxPagesToLoad(17));
                if (!posts.Succeeded)
                {
                    SendMessageStop(false,message:"request failed");
                    return;
                }
                int j=0;
                foreach (var post in posts.Value.Medias)
                {
                    if(post.HasLiked) continue;
                    if (CancelTokenSource.IsCancellationRequested)
                    {
                        SendMessageStop(true);
                        return;
                    }
                    if (j > 10)
                    {
                        SendMessageStop(false, true,message:"limit");
                        return;
                    }

                    var like = await Api.MediaProcessor.LikeMediaAsync(post.InstaIdentifier);
                    bool success = like.Value;
                    if (!success)
                    {
                        j++;
                        await Task.Delay(Duration);
                        continue;
                    }
                    j = 0;
                    Console.WriteLine($"{GetUsername()}: #{Hashtag}, {true}");
                    await Task.Delay(Duration);
                }

                SendMessageStop(true);
            }
            catch(Exception ex)
            {
                SendMessageStop(false, message:ex.Message);
            }
        }

        private async void SendMessageStart()
        {
            try
            {
                TelegramBotClient tgbot = new TelegramBotClient("1682222171:AAGw4CBCJ875NRn1rFnh0sBncYkev5KIa4o");
                await tgbot.SendTextMessageAsync(Owner.Id,
                    $"🆕 Отработка начата. Аккаунт {GetUsername()}. Хештег #{Hashtag}.", replyMarkup:new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🛑 Отмена", $"cancel_{Id}")));
            }
            catch
            {
                // ignored
            }
        }

        private async void SendMessageStop(bool finished, bool limin = false,string message = "")
        {
            try
            {
                if(message!="")Console.WriteLine($"У {GetUsername()} ошибка. {message}");
                Owner.Works.Remove(this);
                TelegramBotClient tgbot = new TelegramBotClient("1682222171:AAGw4CBCJ875NRn1rFnh0sBncYkev5KIa4o");
                if (finished)
                {
                    await tgbot.SendTextMessageAsync(Owner.Id, 
                        $"🏁 Отработка завершена успешно. Аккаунт {GetUsername()}. Хештег #{Hashtag}.");
                }
                else
                {
                    if(limin)
                        await tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {GetUsername()}. Хештег #{Hashtag}. Скорее всего вы достигли лимита лайков на сегодня.");
                    else
                        await tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {GetUsername()}. Хештег #{Hashtag}.");
                }
            }
            catch
            {
                // ignored
            }
        }

    }
}

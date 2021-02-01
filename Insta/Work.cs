using System;
using System.Threading;
using System.Threading.Tasks;
using InstagramApiSharp;
using InstagramApiSharp.API;
using System.Timers;
using Telegram.Bot;
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
        private static readonly TelegramBotClient Tgbot =
            new TelegramBotClient("1682222171:AAGw4CBCJ875NRn1rFnh0sBncYkev5KIa4o");

        private int _countLike = 0, _countSave = 0;
        public enum Mode
        {
            like,
            save,
            likeAndSave
        }

        private Mode mode;
        public readonly CancellationTokenSource CancelTokenSource = new CancellationTokenSource();
        public Work(int id, IInstaApi api, User user)
        {
            Id = id;
            Api = api;
            Owner = user;
            Owner.Works.Add(this);
        }

        public void SetMode(Mode mode)
        {
            this.mode = mode;
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
                    PaginationParameters.MaxPagesToLoad(30));
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
                        Console.WriteLine("Post has liked");
                        SendMessageStop(true);
                        return;
                    }
                    if (j > 10)
                    {
                        SendMessageStop(false, true,message:"limit");
                        return;
                    }
                    bool success=false;
                    switch (mode)
                    {
                        case Mode.like:
                            success = Api.MediaProcessor.LikeMediaAsync(post.InstaIdentifier).Result.Value;
                            break;
                        case Mode.save:
                            success=Api.MediaProcessor.SaveMediaAsync(post.InstaIdentifier).Result.Value;
                            break;
                        case Mode.likeAndSave:
                            success = Api.MediaProcessor.LikeMediaAsync(post.InstaIdentifier).Result.Value && Api.MediaProcessor.SaveMediaAsync(post.InstaIdentifier).Result.Value;
                            break;
                    }
                    if (!success)
                    {
                        j++;
                        await Task.Delay(Duration);
                        continue;
                    }
                    _countLike++;
                    _countSave++;
                    j = 0;
                        //Console.WriteLine($"{GetUsername()}: #{Hashtag}");
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
                await Tgbot.SendTextMessageAsync(Owner.Id,
                    $"Отработка запущена. Аккаунт {GetUsername()}. Хештег #{Hashtag}.",
                    replyMarkup: Keyboards.Cancel(Id));
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
                string result=String.Empty;
                switch (mode)
                {
                    case Mode.like:
                        result = $"\nЛайков поставлено: {_countLike}";
                        break;
                    case Mode.save:
                        result = $"\nСохранено постов: {_countSave}";
                        break;
                    case Mode.likeAndSave:
                        result = $"\nЛайков поставлено: {_countLike}\nСохранено постов: {_countSave}";
                        break;
                }
                Owner.Works.Remove(this);
                if (finished)
                {
                    await Tgbot.SendTextMessageAsync(Owner.Id, 
                        $"🏁 Отработка завершена успешно. Аккаунт {GetUsername()}. Хештег #{Hashtag}.{result}");
                }
                else
                {
                    if(limin)
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {GetUsername()}. Хештег #{Hashtag}. Скорее всего вы достигли лимита лайков / сохранений.{result}");
                    else
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {GetUsername()}. Хештег #{Hashtag}.{result}");
                }
            }
            catch
            {
                // ignored
            }
        }

    }
}

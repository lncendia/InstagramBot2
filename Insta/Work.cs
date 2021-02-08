using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InstagramApiSharp;
using InstagramApiSharp.API;
using System.Timers;
using InstagramApiSharp.Classes;
using Telegram.Bot;
using Timer = System.Timers.Timer;

namespace Insta
{
    public class Work
    {
        public int Id { get; }
        private IInstaApi Api { get; }
        public string Hashtag { get; private set; }
        private int LowerDelay { get; set; }
        private int UpperDelay { get; set; }
        private Timer Timer { get; set; }
        private User Owner { get; }

        private static readonly TelegramBotClient Tgbot = 
            new(Program.Token);
            private static readonly Random Rnd = new();

            private int _countLike, _countSave, _countFollow;
        public enum Mode
        {
            like,
            save,
            follow,
            likeAndSave }

        private Mode mode;
        public readonly CancellationTokenSource CancelTokenSource = new();
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

        public void SetDuration(int ld,int ud)
        {
            LowerDelay = ld;
            UpperDelay = ud;
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
        public bool IsStarted { get; private set; }
        public async void Start()
        {
            try
            {
                IsStarted = true;
                SendMessageStart();
                var posts = await Api.HashtagProcessor.GetRecentHashtagMediaListAsync(Hashtag,
                    PaginationParameters.MaxPagesToLoad(33));
                if (posts.Info.ResponseType == ResponseType.LoginRequired)
                {
                    SendMessageStop(false,message:"logOut", needLeave:true);
                    return;
                }
                if (!posts.Succeeded)
                {
                    SendMessageStop(false,message:"request failed");
                    return;
                }
                
                foreach (var post in posts.Value.Medias)
                {
                    if (CancelTokenSource.IsCancellationRequested)
                    {
                        SendMessageStop(true);
                        return;
                    }
                    bool success=true, logOut = false;
                    switch (mode)
                    {
                        case Mode.like:
                            if(post.HasLiked) continue;
                            var like = await Api.MediaProcessor.LikeMediaAsync(post.InstaIdentifier);
                            switch (like.Info.ResponseType)
                            {
                                case ResponseType.Spam:
                                    success = false;
                                    break;
                                case ResponseType.OK:
                                    _countLike++;
                                    break;
                                case ResponseType.LoginRequired:
                                    logOut = true;
                                    break;
                            }
                            break;
                        case Mode.save:
                            var save = await Api.MediaProcessor.SaveMediaAsync(post.InstaIdentifier);
                            switch (save.Info.ResponseType)
                            {
                                case ResponseType.Spam:
                                    success = false;
                                    break;
                                case ResponseType.RequestsLimit:
                                    success = false;
                                    break;
                                case ResponseType.OK:
                                    _countSave++;
                                    break;
                                case ResponseType.LoginRequired:
                                    logOut = true;
                                    break;
                            }
                            break;
                        case Mode.follow:
                            var follow = await Api.UserProcessor.FollowUserAsync(post.User.Pk);
                            switch (follow.Info.ResponseType)
                            {
                                case ResponseType.RequestsLimit:
                                    success = false;
                                    break;
                                case ResponseType.OK:
                                    _countFollow++;
                                    break;
                                case ResponseType.LoginRequired:
                                    logOut = true;
                                    break;
                            }
                            break;
                        case Mode.likeAndSave:
                            like = await Api.MediaProcessor.LikeMediaAsync(post.InstaIdentifier);
                            save = await Api.MediaProcessor.SaveMediaAsync(post.InstaIdentifier);
                            switch (like.Info.ResponseType)
                            {
                                case ResponseType.Spam:
                                    success = false;
                                    break;
                                case ResponseType.OK:
                                    if (!post.HasLiked)_countLike++;
                                    if (save.Info.ResponseType == ResponseType.OK) _countSave++;
                                    break;
                                case ResponseType.LoginRequired:
                                    logOut = true;
                                    break;
                            }
                            break;
                    }

                    if (logOut)
                    {
                        SendMessageStop(false, false,message:"logOut", true);
                        return; 
                    }
                    if (!success)
                    {
                        SendMessageStop(false, true,message:"limit");
                        return;
                    }
                    
                    await Task.Delay(Rnd.Next(LowerDelay, UpperDelay) * 1000);
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
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Отработка запущена у {Owner.Id}.\nАккаунт: {GetUsername()}\nХештег: #{Hashtag}\n");
                await Tgbot.SendTextMessageAsync(Owner.Id,
                    $"Отработка запущена. Аккаунт {GetUsername()}. Хештег #{Hashtag}.",
                    replyMarkup: Keyboards.Cancel(Id));
            }
            catch
            {
                // ignored
            }
        }

        private async void SendMessageStop(bool finished, bool limit = false,string message = "", bool needLeave = false)
        {
            try
            {
                Owner.Works.Remove(this);
                string result=String.Empty;
                switch (mode)
                {
                    case Mode.like:
                        result = $"\nЛайков поставлено: {_countLike}";
                        break;
                    case Mode.save:
                        result = $"\nСохранено постов: {_countSave}";
                        break;
                    case Mode.follow:
                        result = $"\nПодписок сделано: {_countFollow}";
                        break;
                    case Mode.likeAndSave:
                        result = $"\nЛайков поставлено: {_countLike}\nСохранено постов: {_countSave}";
                        break;
                }
                if (finished)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Отработка завершена у {Owner.Id}.\nИнстаграм: {GetUsername()}\nХештег: #{Hashtag}{result}\n");
                    await Tgbot.SendTextMessageAsync(Owner.Id, 
                        $"🏁 Отработка завершена успешно. Аккаунт {GetUsername()}. Хештег #{Hashtag}.{result}");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Отработка завершена у {Owner.Id} c ошибкой: {message}.\nИнстаграм: {GetUsername()}\nХештег: #{Hashtag}{result}\n");
                    if(limit)
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {GetUsername()}. Хештег #{Hashtag}. Вы достигли ограничения.{result}");
                    else if(needLeave)
                    {
                        
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {GetUsername()}. Хештег #{Hashtag}. Был осуществлен выход, пожалуйста, войдите заново.{result}");
                        Instagram inst = Owner.Instagrams.FirstOrDefault(_ => _.Username == GetUsername());
                        if(inst==null) return;
                        await using DB db = new DB();
                        db.UpdateRange(Owner, inst);
                        Owner.Instagrams.Remove(inst);
                        db.Remove(inst);
                        await db.SaveChangesAsync();
                    }
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

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Insta.Bot;
using Insta.Entities;
using InstagramApiSharp;
using InstagramApiSharp.Classes;
using Telegram.Bot;
using Timer = System.Timers.Timer;

namespace Insta.Working
{
    public class Work
    {
        public int Id { get; }
        public Instagram Instagram { get; }
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
            likeAndSave
        }

        private enum Stop
        {
            ok,
            limit,
            logOut,
            proxyError,
            anotherError
        }

        private Mode mode;
        public readonly CancellationTokenSource CancelTokenSource = new();

        public Work(int id, Instagram inst, User user)
        {
            Id = id;
            Instagram = inst;
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

        public void SetDuration(int ld, int ud)
        {
            LowerDelay = ld;
            UpperDelay = ud;
        }

        public async Task StartAtTime(DateTime time)
        {
            try
            {
                Timer = new Timer(time.Subtract(DateTime.Now).TotalMilliseconds) {Enabled = true};
                Timer.Elapsed += Timer_Elapsed;
                await using Db db = new Db();
                _works = new WorkTask
                {
                    Hashtag = Hashtag, Instagram = Instagram, LowerDelay = LowerDelay, UpperDelay = UpperDelay,
                    StartTime = time
                };
                db.Update(Instagram);
                db.Add(_works);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await SendMessageStop(Stop.anotherError, message:ex.Message);
            }
            
        }
        public async Task StartAtTime(DateTime time, WorkTask task)
        {
            try
            {
                Timer = new Timer(time.Subtract(DateTime.Now).TotalMilliseconds) {Enabled = true};
                Timer.Elapsed += Timer_Elapsed;
                _works = task;
            }
            catch (Exception ex)
            {
                await SendMessageStop(Stop.anotherError, message: ex.Message);
            }
        }

        private WorkTask _works;
        public async Task TimerDispose()
        {
            try
            {
                Timer.Dispose();
                CancelTokenSource.Cancel();
                await SendMessageStop(Stop.ok);
            }
            catch
            {
                await SendMessageStop(Stop.anotherError, message: "Timer stop failed");
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
                if (Instagram.Api == null)
                {
                    await SendMessageStop(Stop.logOut, message: "logOut");
                    return;
                }
                SendMessageStart();
                var posts = await Instagram.Api.HashtagProcessor.GetRecentHashtagMediaListAsync(Hashtag,
                    PaginationParameters.MaxPagesToLoad(34));
                if (posts.Info.ResponseType == ResponseType.LoginRequired)
                {
                    await SendMessageStop(Stop.logOut, message: "logOut");
                    return;
                }

                if (!posts.Succeeded)
                {
                    if (posts.Info.Exception is HttpRequestException || posts.Info.Exception is NullReferenceException)
                    {
                        if (Operation.CheckProxy(Instagram.Proxy))
                        {
                            await SendMessageStop(Stop.logOut, message: "logOut");
                            return;
                        }

                        await SendMessageStop(Stop.proxyError, "Ошибка прокси");
                    }
                    else
                    {
                        await SendMessageStop(Stop.anotherError, message: "Ошибка при загрузке постов");
                    }

                    return;
                }

                foreach (var post in posts.Value.Medias)
                {
                    if (CancelTokenSource.IsCancellationRequested)
                    {
                        await SendMessageStop(Stop.ok);
                        return;
                    }
                    switch (mode)
                    {
                        case Mode.like:
                            if (post.HasLiked) continue;
                            var like = await Instagram.Api.MediaProcessor.LikeMediaAsync(post.InstaIdentifier);
                            if(!await CheckResult(like.Info)) return;
                            break;
                        case Mode.save:
                            var save = await Instagram.Api.MediaProcessor.SaveMediaAsync(post.InstaIdentifier);
                            if(!await CheckResult(save.Info)) return;
                            break;
                        case Mode.follow:
                            var follow = await Instagram.Api.UserProcessor.FollowUserAsync(post.User.Pk);
                            if(!await CheckResult(follow.Info)) return;
                            break;
                        case Mode.likeAndSave:
                            like = await Instagram.Api.MediaProcessor.LikeMediaAsync(post.InstaIdentifier);
                            save = await Instagram.Api.MediaProcessor.SaveMediaAsync(post.InstaIdentifier);
                            if(!await CheckResult(like.Info)) return;
                            if(!await CheckResult(save.Info)) return;
                            break;
                    }
                    await Task.Delay(Rnd.Next(LowerDelay, UpperDelay) * 1000);
                }
                await SendMessageStop(Stop.ok);
            }
            catch (Exception ex)
            {
                await SendMessageStop(Stop.anotherError, message: ex.Message);
            }
        }

        private async Task<bool> CheckResult(ResultInfo result)
        {
            try
            {
                if (result == null)
                {
                    return false;
                }

                switch (result.ResponseType)
                {
                    case ResponseType.Spam:
                        await SendMessageStop(Stop.limit, message: "limit");
                        return false;
                    case ResponseType.RequestsLimit:
                        await SendMessageStop(Stop.limit, message: "limit");
                        return false;
                    case ResponseType.OK:
                        switch (mode)
                        {
                            case Mode.like:
                                _countLike++;
                                break;
                            case Mode.save:
                                _countSave++;
                                break;
                            case Mode.follow:
                                _countFollow++;
                                break;
                            case Mode.likeAndSave:
                                _countSave++;
                                _countLike++;
                                break;
                        }

                        return true;
                    case ResponseType.LoginRequired:
                        await SendMessageStop(Stop.logOut, message: "logOut");
                        return false;
                    case ResponseType.UnExpectedResponse:
                        if (Operation.CheckProxy(Instagram.Proxy))
                            return true;
                        else
                        {
                            await SendMessageStop(Stop.proxyError, "Ошибка прокси");
                            return false;
                        }
                    case ResponseType.NetworkProblem:
                        if (result.Exception is HttpRequestException)
                        {
                            Operation.CheckProxy(Instagram.Proxy);
                            await SendMessageStop(Stop.proxyError, "Ошибка прокси");
                        }
                        else
                        {
                            await SendMessageStop(Stop.anotherError, result.ResponseType.ToString());
                        }

                        return false;
                    default:
                        await SendMessageStop(Stop.anotherError, result.ResponseType.ToString());
                        return false;
                }
            }
            catch(Exception ex)
            {
                await SendMessageStop(Stop.anotherError, message: ex.Message);
                return false;
            }
        }

        private async void SendMessageStart()
        {
            try
            {
                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] Отработка запущена у {Owner.Id}.\nАккаунт: {Instagram.Username}\nХештег: #{Hashtag}\n");
                await Tgbot.SendTextMessageAsync(Owner.Id,
                    $"Отработка запущена. Аккаунт {Instagram.Username}. Хештег #{Hashtag}.",
                    replyMarkup: Keyboards.Cancel(Id));
            }
            catch
            {
                // ignored
            }
        }

        private async Task SendMessageStop(Stop stop, string message = "")
        {
            try
            {
                Owner.Works.Remove(this);
                string result = String.Empty;
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

                var log = stop == Stop.ok
                    ? $"[{DateTime.Now:HH:mm:ss}] Отработка завершена у {Owner.Id}.\nИнстаграм: {Instagram.Username}\nХештег: #{Hashtag}{result}\n"
                    : $"[{DateTime.Now:HH:mm:ss}] Отработка завершена у {Owner.Id} c ошибкой: {message}\n[{Instagram.Proxy.Id}] {Instagram.Proxy.Host}:{Instagram.Proxy.Port}.\nИнстаграм: {Instagram.Username}\nХештег: #{Hashtag}{result}\n";
                Console.WriteLine(log);
                switch (stop)
                {
                    case Stop.ok:
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена успешно. Аккаунт {Instagram.Username}. Хештег #{Hashtag}.{result}");
                        break;
                    case Stop.limit:
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {Instagram.Username}. Хештег #{Hashtag}. Вы достигли ограничения.{result}");
                        break;
                    case Stop.logOut:
                    {
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {Instagram.Username}. Хештег #{Hashtag}. Был осуществлен выход, пожалуйста, войдите заново.{result}");
                        Instagram inst = Owner.Instagrams.FirstOrDefault(_ => _.Username == Instagram.Username);
                        if (inst == null) return;
                        await using Db db = new Db();
                        db.UpdateRange(Owner, inst);
                        Owner.Instagrams.Remove(inst);
                        db.Remove(inst);
                        await db.SaveChangesAsync();
                        break;
                    }
                    case Stop.proxyError:
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {Instagram.Username}. Хештег #{Hashtag}. Попробуйте сменить прокси.{result}",
                            replyMarkup: Keyboards.ChangeProxy(Instagram));
                        break;
                    case Stop.anotherError:
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой ({message}). Аккаунт {Instagram.Username}. Хештег #{Hashtag}.{result}");
                        break;
                }

                if (_works != null)
                {
                    await using Db context = new Db();
                    context.Update(_works);
                    context.Remove(_works);
                    await context.SaveChangesAsync();
                }
            }
            catch
            {
                // ignored
            }
        }

    }
}

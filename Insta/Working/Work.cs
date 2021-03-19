using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Insta.Bot;
using Insta.Enums;
using Insta.Model;
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
        private int Offset { get; set; }
        private Timer Timer { get; set; }
        private User Owner { get; }
        private WorkTask _works;
        public bool IsStarted { get; private set; }

        private static readonly TelegramBotClient Tgbot = BotSettings.Get();

        private static readonly Random Rnd = new();

        private int _countLike, _countSave, _countFollow;

        private Mode _mode;
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
            _mode = mode;
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

        public void SetOffset(int offset)
        {
            Offset = offset;
        }
        private int _iterator;
        private int _countPosts;
        public string GetInformation()
        {
            if(!IsStarted) return String.Empty;
            return _countPosts == 0 ? "Получение публикаций..." : $"Постов обработано: {_iterator}/{_countPosts}.";
        }
        public async Task StartAtTimeAsync(DateTime time)
        {
            try
            {
                Timer = new Timer(time.Subtract(DateTime.Now).TotalMilliseconds) {Enabled = true};
                Timer.Elapsed += Timer_ElapsedAsync;
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
                await SendMessageStopAsync(Stop.anotherError, ex.Message);
            }

        }

        public async Task StartAtTimeAsync(DateTime time, WorkTask task)
        {
            try
            {
                Timer = new Timer(time.Subtract(DateTime.Now).TotalMilliseconds) {Enabled = true};
                Timer.Elapsed += Timer_ElapsedAsync;
                _works = task;
            }
            catch (Exception ex)
            {
                await SendMessageStopAsync(Stop.anotherError, ex.Message);
            }
        }

        public async Task TimerDisposeAsync()
        {
            try
            {
                Timer.Dispose();
                CancelTokenSource.Cancel();
                await SendMessageStopAsync(Stop.ok);
            }
            catch
            {
                await SendMessageStopAsync(Stop.anotherError, message: "Timer stop failed");
            }
        }

        private async void Timer_ElapsedAsync(object sender, ElapsedEventArgs e)
        {
            Timer.Dispose();
            await StartAsync();
        }

        private int _countNetworkProblems;
        public async Task StartAsync()
        {
            try
            {
                IsStarted = true;

                if (Instagram.Api == null)
                {
                    await SendMessageStopAsync(Stop.logOut, message: "logOut");
                    return;
                }

                await SendMessageStartAsync();
                var posts = await Instagram.Api.HashtagProcessor.GetRecentHashtagMediaListAsync(Hashtag,
                    PaginationParameters.MaxPagesToLoad(34));
                if (posts.Info.ResponseType == ResponseType.LoginRequired)
                {
                    await SendMessageStopAsync(Stop.logOut, message: "logOut");
                    return;
                }

                if (!posts.Succeeded)
                {
                    if (posts.Info.Exception is HttpRequestException || posts.Info.Exception is NullReferenceException)
                    {
                        if (Operation.CheckProxy(Instagram.Proxy))
                        {
                            await SendMessageStopAsync(Stop.logOut, message: "logOut");
                            return;
                        }

                        await SendMessageStopAsync(Stop.proxyError, "Ошибка прокси");
                    }
                    else
                    {
                        await SendMessageStopAsync(Stop.anotherError, message: "Ошибка при загрузке постов");
                    }

                    return;
                }

                _countPosts = posts.Value.Medias.Count;
                for (_iterator = Offset; _iterator < _countPosts; _iterator++)
                {
                    var post = posts.Value.Medias[_iterator];
                    if (CancelTokenSource.IsCancellationRequested)
                    {
                        await SendMessageStopAsync(Stop.ok);
                        return;
                    }

                    switch (_mode)
                    {
                        case Mode.like:
                            if (post.HasLiked) continue;
                            var like = await Instagram.Api.MediaProcessor.LikeMediaAsync(post.InstaIdentifier);
                            if (!await CheckResultAsync(like.Info)) return;
                            _countLike++;
                            break;
                        case Mode.save:
                            var save = await Instagram.Api.MediaProcessor.SaveMediaAsync(post.InstaIdentifier);
                            if (!await CheckResultAsync(save.Info)) return;
                            _countSave++;
                            break;
                        case Mode.follow:
                            var follow = await Instagram.Api.UserProcessor.FollowUserAsync(post.User.Pk);
                            if (!await CheckResultAsync(follow.Info)) return;
                            _countFollow++;
                            break;
                        case Mode.likeAndSave:
                            like = await Instagram.Api.MediaProcessor.LikeMediaAsync(post.InstaIdentifier);
                            save = await Instagram.Api.MediaProcessor.SaveMediaAsync(post.InstaIdentifier);
                            if (!await CheckResultAsync(like.Info)) return;
                            if (!await CheckResultAsync(save.Info)) return;
                            _countSave++;
                            _countLike++;
                            break;
                    }

                    await Task.Delay(Rnd.Next(LowerDelay, UpperDelay) * 1000);
                }

                await SendMessageStopAsync(Stop.ok);
            }
            catch (Exception ex)
            {
                await SendMessageStopAsync(Stop.anotherError, ex.Message);
            }
        }

        private async Task<bool> CheckResultAsync(ResultInfo result)
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
                        await SendMessageStopAsync(Stop.limit, message: "limit");
                        return false;
                    case ResponseType.RequestsLimit:
                        await SendMessageStopAsync(Stop.limit, message: "limit");
                        return false;
                    case ResponseType.OK:
                        return true;
                    case ResponseType.LoginRequired:
                        await SendMessageStopAsync(Stop.logOut, message: "logOut");
                        return false;
                    case ResponseType.UnExpectedResponse:
                        if (Operation.CheckProxy(Instagram.Proxy))
                            return true;
                        else
                        {
                            await SendMessageStopAsync(Stop.proxyError, "Ошибка прокси");
                            return false;
                        }
                    case ResponseType.NetworkProblem:
                        _countNetworkProblems++;
                        if (_countNetworkProblems < 10) return true;
                        if (result.Exception is HttpRequestException)
                        {
                            Operation.CheckProxy(Instagram.Proxy);
                            await SendMessageStopAsync(Stop.proxyError, "Ошибка прокси");
                        }
                        else
                        {
                            await SendMessageStopAsync(Stop.anotherError, result.ResponseType.ToString());
                        }
                        return false;
                    default:
                        await SendMessageStopAsync(Stop.anotherError, result.ResponseType.ToString());
                        return false;
                }
            }
            catch (Exception ex)
            {
                await SendMessageStopAsync(Stop.anotherError, message: ex.Message);
                return false;
            }
        }

        private async Task SendMessageStartAsync()
        {
            try
            {
                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] Отработка запущена у {Owner.Id} ({LowerDelay}-{UpperDelay}).\nАккаунт: {Instagram.Username}\nХештег: #{Hashtag}\n");
                await Tgbot.SendTextMessageAsync(Owner.Id,
                    $"Отработка запущена. Аккаунт {Instagram.Username}. Хештег #{Hashtag}.",
                    replyMarkup: Keyboards.Cancel(Id));
            }
            catch
            {
                // ignored
            }
        }

        private async Task SendMessageStopAsync(Stop stop, string message = "")
        {
            try
            {
                Owner.Works.Remove(this);
                string result = String.Empty;
                switch (_mode)
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
                    ? $"[{DateTime.Now:HH:mm:ss}] Отработка завершена у {Owner.Id} ({LowerDelay}-{UpperDelay}).\nИнстаграм: {Instagram.Username}\nХештег: #{Hashtag}{result}\n"
                    : $"[{DateTime.Now:HH:mm:ss}] Отработка завершена у {Owner.Id} ({LowerDelay}-{UpperDelay}) c ошибкой: {message}\n[{Instagram.Proxy.Id}] {Instagram.Proxy.Host}:{Instagram.Proxy.Port}.\nИнстаграм: {Instagram.Username}\nХештег: #{Hashtag}{result}\n";
                Console.WriteLine(log);
                switch (stop)
                {
                    case Stop.ok:
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена успешно. Аккаунт {Instagram.Username}. Хештег #{Hashtag}.{result}");
                        break;
                    case Stop.limit:
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {Instagram.Username}. Хештег #{Hashtag}. Вы достигли ограничения.{result}.\nПерезаход в аккаунт завершит все запущенные на нем отработки.", replyMarkup:Keyboards.Exit(Instagram.Id,false));
                        break;
                    case Stop.logOut:
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {Instagram.Username}. Хештег #{Hashtag}. Был осуществлен выход, пожалуйста, войдите заново.{result}");
                        await Operation.LogOutAsync(Owner, Instagram);
                        return;
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
                    try
                    {
                        await using Db context = new Db();
                        context.Update(_works);
                        context.Remove(_works);
                        await context.SaveChangesAsync();
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    await Tgbot.SendTextMessageAsync(346978522, $"[{DateTime.Now}]: Ошибка у {Owner.Id} {e.Message}");
                    await Tgbot.SendTextMessageAsync(346978522, $"StackTrace: {e.StackTrace}");
                    var trace = new StackTrace(e, true);

                    foreach (var frame in trace.GetFrames())
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine($"Файл: {frame.GetFileName()}");
                        sb.AppendLine($"Строка: {frame.GetFileLineNumber()}");
                        sb.AppendLine($"Столбец: {frame.GetFileColumnNumber()}");
                        sb.AppendLine($"Метод: {frame.GetMethod()}");
                        await Tgbot.SendTextMessageAsync(346978522, sb.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке исключения!!! {e.Message}\n{ex.Message}");
                }
            }
        }
    }
}

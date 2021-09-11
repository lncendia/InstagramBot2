using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Insta.Bot;
using Insta.Enums;
using Insta.Model;
using InstagramApiSharp;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using Telegram.Bot;
using Result = Insta.Enums.Result;
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
            if (!IsStarted) return String.Empty;
            return _countPosts == 0
                ? "Получение публикаций..."
                : $"Постов обработано: {_iterator - Offset}/{_countPosts - Offset}.";
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
                    StartTime = time, Offset = Offset
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
                await SendMessageStopAsync(Stop.anotherError, "Timer stop failed");
            }
        }

        private async void Timer_ElapsedAsync(object sender, ElapsedEventArgs e)
        {
            Timer.Dispose();
            await StartAsync();
        }

        private async Task<IResult<InstaSectionMedia>> GetPosts()
        {
            try
            {
                if (Instagram.Api == null)
                {
                    await SendMessageStopAsync(Stop.logOut, "logOut");
                    return null;
                }

                await SendMessageStartAsync();

                var posts = await Instagram.Api.HashtagProcessor.GetRecentHashtagMediaListAsync(Hashtag,
                    PaginationParameters.MaxPagesToLoad(34));
                if (CancelTokenSource.IsCancellationRequested)
                {
                    await SendMessageStopAsync(Stop.ok);
                }
                else if (posts.Succeeded) return posts;
                else if (posts.Info.Exception is HttpRequestException or NullReferenceException)
                {
                    if (Operation.CheckProxy(Instagram.Proxy))
                    {
                        await SendMessageStopAsync(Stop.logOut, "logOut");
                        return null;
                    }

                    await Task.Delay(new TimeSpan(0, 2, 0));
                    posts = await Instagram.Api.HashtagProcessor.GetRecentHashtagMediaListAsync(Hashtag,
                        PaginationParameters.MaxPagesToLoad(34));
                    if (posts.Succeeded) return posts;
                    await SendMessageStopAsync(Stop.proxyError, "Ошибка прокси");
                    return null;
                }
                else if (posts.Info.ResponseType == ResponseType.LoginRequired)
                {
                    await SendMessageStopAsync(Stop.logOut, "logOut");
                }
                else
                {
                    await SendMessageStopAsync(Stop.anotherError, "Ошибка при загрузке постов");
                }

                return null;
            }
            catch (Exception ex)
            {
                await SendMessageStopAsync(Stop.anotherError, ex.Message);
                return null;
            }
        }

        private int _countNetworkProblems;
        private int _timeOuts;
        private int _proxyErrors;

        public async Task StartAsync()
        {
            try
            {
                IsStarted = true;
                var posts = await GetPosts();
                if (posts is null) return;
                _countPosts = posts.Value.Medias.Count;
                if (Offset > _countPosts)
                {
                    await SendMessageStopAsync(Stop.wrongOffset, "Неверный номер поста");
                    return;
                }

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
                            if (!post.HasLiked)
                            {
                                switch (await CheckResultAsync(
                                    (await Instagram.Api.MediaProcessor.LikeMediaAsync(post.InstaIdentifier)).Info))
                                {
                                    case Result.ok:
                                        ++_countLike;
                                        break;
                                    case Result.timeOut:
                                        continue;
                                    case Result.stop:
                                        return;
                                }
                            }

                            break;
                        case Mode.save:
                            switch (await CheckResultAsync(
                                (await Instagram.Api.MediaProcessor.SaveMediaAsync(post.InstaIdentifier)).Info))
                            {
                                case Result.ok:
                                    ++_countSave;
                                    break;
                                case Result.timeOut:
                                    continue;
                                case Result.stop:
                                    return;
                            }

                            break;
                        case Mode.follow:
                            switch (await CheckResultAsync(
                                (await Instagram.Api.UserProcessor.FollowUserAsync(post.User.Pk)).Info))
                            {
                                case Result.ok:
                                    ++_countFollow;
                                    break;
                                case Result.timeOut:
                                    continue;
                                case Result.stop:
                                    return;
                            }

                            break;
                        case Mode.likeAndSave:
                            if (!post.HasLiked)
                            {
                                switch (await CheckResultAsync(
                                    (await Instagram.Api.MediaProcessor.LikeMediaAsync(post.InstaIdentifier)).Info))
                                {
                                    case Result.ok:
                                        ++_countLike;
                                        post.HasLiked = true;
                                        break;
                                    case Result.timeOut:
                                        continue;
                                    case Result.stop:
                                        return;
                                }
                            }

                            switch (await CheckResultAsync(
                                (await Instagram.Api.MediaProcessor.SaveMediaAsync(post.InstaIdentifier)).Info))
                            {
                                case Result.ok:
                                    ++_countSave;
                                    break;
                                case Result.timeOut:
                                    continue;
                                case Result.stop:
                                    return;
                            }

                            break;
                    }

                    await Task.Delay(Rnd.Next(LowerDelay, UpperDelay) * 1000);
                }

                await SendMessageStopAsync(Stop.ok, needBlock: true);
            }
            catch (Exception ex)
            {
                await SendMessageStopAsync(Stop.anotherError, ex.Message);
            }
        }

        private async Task<Result> CheckResultAsync(ResultInfo result)
        {
            {
                if (result == null)
                    return Result.stop;
                switch (result.ResponseType)
                {
                    case ResponseType.LoginRequired:
                        await SendMessageStopAsync(Stop.logOut, "logOut");
                        return Result.stop;
                    case ResponseType.RequestsLimit:
                        if (_timeOuts < 3)
                        {
                            --_iterator;
                            ++_timeOuts;
                            await Task.Delay(new TimeSpan(0, 5, 0));
                            return Result.timeOut;
                        }

                        await SendMessageStopAsync(Stop.limit, "limit");
                        return Result.stop;
                    case ResponseType.OK:
                        return Result.ok;
                    case ResponseType.UnExpectedResponse:
                        if (Operation.CheckProxy(Instagram.Proxy))
                            return Result.timeOut;
                        if (_proxyErrors < 1)
                        {
                            --_iterator;
                            ++_proxyErrors;
                            await Task.Delay(new TimeSpan(0, 2, 0));
                            return Result.timeOut;
                        }

                        await SendMessageStopAsync(Stop.proxyError, "Ошибка прокси");
                        return Result.stop;
                    case ResponseType.Spam:
                        if (_timeOuts < 3)
                        {
                            --_iterator;
                            ++_timeOuts;
                            await Task.Delay(new TimeSpan(0, 5, 0));
                            return Result.timeOut;
                        }

                        await SendMessageStopAsync(Stop.limit, "limit");
                        return Result.stop;
                    case ResponseType.NetworkProblem:
                        --_iterator;
                        ++_countNetworkProblems;
                        if (_countNetworkProblems < 10)
                            return Result.timeOut;
                        if (result.Exception is HttpRequestException)
                        {
                            if (_proxyErrors < 1)
                            {
                                ++_proxyErrors;
                                await Task.Delay(new TimeSpan(0, 2, 0));
                                return Result.timeOut;
                            }

                            Operation.CheckProxy(Instagram.Proxy);
                            await SendMessageStopAsync(Stop.proxyError, "Ошибка прокси");
                        }
                        else
                            await SendMessageStopAsync(Stop.anotherError, result.ResponseType.ToString());

                        return Result.stop;
                    default:
                        await SendMessageStopAsync(Stop.anotherError, result.ResponseType.ToString());
                        return Result.stop;
                }
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

        private async Task SendMessageStopAsync(Stop stop, string message = "", bool needBlock = false)
        {
            try
            {
                Owner.Works.Remove(this);
                if (needBlock) Instagram.Block = DateTime.Now.AddHours(BotSettings.Cfg.BlockHours);
                else if (stop != Stop.ok) Instagram.Block = DateTime.MinValue;
                var result = _mode switch
                {
                    Mode.like => $"\nЛайков поставлено: {_countLike}",
                    Mode.save => $"\nСохранено постов: {_countSave}",
                    Mode.follow => $"\nПодписок сделано: {_countFollow}",
                    Mode.likeAndSave => $"\nЛайков поставлено: {_countLike}\nСохранено постов: {_countSave}",
                    _ => String.Empty
                };
                if (IsStarted && _countPosts != 0 && stop != Stop.wrongOffset)
                {
                    result += $"\nВсего постов: {_countPosts - Offset}";
                }

                var log = stop == Stop.ok
                    ? $"[{DateTime.Now:HH:mm:ss}] Отработка завершена у {Owner.Id} ({LowerDelay}-{UpperDelay}).\n[{Instagram.Proxy.Id}] {Instagram.Proxy.Host}:{Instagram.Proxy.Port}.\nИнстаграм: {Instagram.Username}\nХештег: #{Hashtag}{result}\n"
                    : $"[{DateTime.Now:HH:mm:ss}] Отработка завершена у {Owner.Id} ({LowerDelay}-{UpperDelay}) c ошибкой: {message}\n[{Instagram.Proxy.Id}] {Instagram.Proxy.Host}:{Instagram.Proxy.Port}.\nИнстаграм: {Instagram.Username}\nХештег: #{Hashtag}{result}\n";
                Console.WriteLine(log);
                switch (stop)
                {
                    case Stop.ok:
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена успешно. Аккаунт {Instagram.Username}. Хештег #{Hashtag}.{result}");
                        break;
                    case Stop.limit:
                        // try
                        // {
                        //     using var httpclient = new HttpClient(Instagram.Api.HttpRequestProcessor.HttpHandler);
                        //     var response = await httpclient.GetAsync("https://yandex.ru/internet");
                        //     if (response.StatusCode == HttpStatusCode.OK)
                        //     {
                        //         HtmlDocument html = new HtmlDocument();
                        //         html.LoadHtml(await response.Content.ReadAsStringAsync());
                        //         var table = html.DocumentNode.SelectSingleNode(
                        //             "//ul[contains(@class, 'general-info layout__general-info')]");
                        //         var ipv4 = table.ChildNodes[0].LastChild.InnerText;
                        //         var ipv6 = table.ChildNodes[1].LastChild.InnerText;
                        //         Console.WriteLine($"IPv4: {ipv4}\nIPv6: {ipv6}");
                        //     }
                        // }
                        // catch
                        // {
                        //     //ignored
                        // }

                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой. Аккаунт {Instagram.Username}. Хештег #{Hashtag}. Вы достигли ограничения.{result}.\nПерезаход в аккаунт завершит все запущенные на нем отработки.",
                            replyMarkup: Keyboards.Exit(Instagram.Id, false));
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
                    case Stop.wrongOffset:
                        await Tgbot.SendTextMessageAsync(Owner.Id,
                            $"🏁 Отработка завершена с ошибкой ({message}). Аккаунт {Instagram.Username}. Хештег #{Hashtag}.{result}");
                        break;
                }

                await using Db context = new Db();
                context.Update(Instagram);
                if (_works != null)
                {
                    context.Update(_works);
                    context.Remove(_works);
                }

                await context.SaveChangesAsync();
            }
            catch
            {
                //ignored
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Insta.Bot;
using Insta.Model;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace Insta.Working
{
    internal static class Operation
    {
        private static List<Proxy> _proxies;
        private static int _number;
        private static readonly TelegramBotClient Tgbot = BotSettings.Get();
        private static readonly object Locker = new();


        private static IWebProxy GetProxy(Instagram instagram)
        {
            lock (Locker)
            {
                try
                {
                    var proxy = _proxies[_number];
                    _number = (++_number) % _proxies.Count;
                    var webProxy = new WebProxy(proxy.Host, proxy.Port)
                    {
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(proxy.Login, proxy.Password)
                    };
                    instagram.Proxy = proxy;
                    //Console.WriteLine($"[{proxy.Id}]{proxy.Host}:{proxy.Port}");
                    return webProxy;
                }
                catch
                {
                    instagram.Proxy = new Proxy() {Host = "default", Port = 0};
                    return new WebProxy();
                }
            }
        }

        public static void LoadProxy(List<Proxy> proxies)
        {
            _proxies = proxies;
        }

        public static bool CheckProxy(Proxy proxy)
        {
            try
            {
                if (proxy.Host == "default") return true;
                var webProxy = new WebProxy(proxy.Host, proxy.Port)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(proxy.Login, proxy.Password)
                };
                HttpClientHandler handler = new HttpClientHandler {Proxy = webProxy};
                HttpClient client = new HttpClient(handler);
                var x = client.GetAsync("https://www.instagram.com");
                switch (x.Result.StatusCode)
                {
                    case HttpStatusCode.ProxyAuthenticationRequired:
                    {
                        Console.WriteLine(
                            $"[{DateTime.Now:HH:mm:ss}] (id {proxy.Id}): {proxy.Host}:{proxy.Port} - неверные данные, прокси будет удалена!\n");
                        using Db db = new Db();
                        _proxies.Remove(proxy);
                        _number %= _proxies.Count;
                        db.Update(proxy);
                        db.Remove(proxy);
                        db.SaveChanges();
                        return false;
                    }
                    case HttpStatusCode.OK:
                        return true;
                    default:
                        ProxyError(proxy, x.Result.StatusCode);
                        return false;
                }
            }
            catch
            {
                ProxyError(proxy, HttpStatusCode.NotFound);
                return false;
            }
        }

        private static void ProxyError(Proxy proxy, HttpStatusCode code)
        {
            try
            {
                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] (id {proxy.Id}): {proxy.Host}:{proxy.Port} - Ошибка {code}\n");
                using Db db = new Db();
                db.Update(proxy);
                proxy.CountErrors++;
                db.SaveChanges();
            }
            catch
            {
                // ignored
            }
        }

        public static bool ChangeProxy(Instagram instagram)
        {
            var data = instagram.Api.GetStateDataAsObject();
            instagram.Api = InstaApiBuilder.CreateBuilder()
                .UseHttpClientHandler(new HttpClientHandler {Proxy = GetProxy(instagram)})
                .Build();
            instagram.Api.LoadStateDataFromObject(data);
            return true;
        }

        public static async Task<IResult<InstaLoginResult>> CheckLoginAsync(Instagram instagram,
            bool isAccepted = false)
        {
            try
            {
                var userSession = new UserSessionData
                {
                    UserName = instagram.Username,
                    Password = instagram.Password
                };
                IWebProxy proxy;
                if (isAccepted && instagram.Proxy != null && instagram.Proxy.Host != "default")
                {
                    proxy = new WebProxy(instagram.Proxy.Host, instagram.Proxy.Port)
                    {
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(instagram.Proxy.Login, instagram.Proxy.Password)
                    };
                }
                else proxy = GetProxy(instagram);

                var instaApi = InstaApiBuilder.CreateBuilder()
                    .UseHttpClientHandler(new HttpClientHandler {Proxy = proxy})
                    .SetUser(userSession)
                    .Build();
                var logInResult = await instaApi.LoginAsync();
                if (logInResult.Value == InstaLoginResult.Success && !logInResult.Succeeded) return null;
                instagram.Api = instaApi;
                return logInResult;
            }
            catch
            {
                return null;
            }
        }

        private static async Task LoadFromStateDataAsync(Instagram instagram)
        {
            try
            {
                var instaApi = InstaApiBuilder.CreateBuilder()
                    .UseHttpClientHandler(new HttpClientHandler {Proxy = GetProxy(instagram)})
                    .Build();
                await instaApi.LoadStateDataFromStringAsync(instagram.StateData);
                instagram.Api = instaApi;
            }
            catch
            {
                // ignored
            }
        }

        public static async Task LoadUsersStateDataAsync(List<Instagram> instagrams)
        {
            foreach (var instagram in instagrams)
            {
                await LoadFromStateDataAsync(instagram);
            }
        }

        public static async Task<IResult<InstaLoginTwoFactorResult>> SendCodeTwoFactorAsync(IInstaApi api, string code)
        {
            var response = await api.TwoFactorLoginAsync(code, 0);
            return response.Succeeded ? response : null;
        }

        public static async Task<IResult<InstaLoginResult>> SendCodeChallengeRequiredAsync(IInstaApi api, string code)
        {
            var response = await api.VerifyCodeForChallengeRequireAsync(code);
            return response.Succeeded ? response : null;
        }

        public static async Task<bool> SubmitPhoneChallengeRequiredAsync(IInstaApi api, string phoneNumber)
        {
            if (!phoneNumber.StartsWith("+"))
                phoneNumber = $"+{phoneNumber}";

            var submitPhone = await api.SubmitPhoneNumberForChallengeRequireAsync(phoneNumber);
            return submitPhone.Succeeded;
        }

        public static async Task<bool> LogOutAsync(User user, Instagram inst)
        {
            try
            {
                await using Db db = new Db();
                foreach (var work in user.Works.Where(_ => _.Instagram == inst).ToList())
                {
                    if (work.IsStarted)
                    {
                        work.CancelTokenSource.Cancel();
                    }
                    else
                    {
                        await work.TimerDisposeAsync();
                    }

                    user.Works.Remove(work);
                }

                db.UpdateRange(user, inst);
                var works = db.Works.Include(_ => _.Instagram).Where(_ => _.Instagram == inst);
                db.RemoveRange(works);
                user.Instagrams.Remove(inst);
                db.Remove(inst);
                user.CountLogOut++;
                await db.SaveChangesAsync();
                if (inst.Api != null) await inst.Api.LogoutAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async void CheckSubscribeAsync(List<User> users)
        {
            while (true)
            {
                try
                {
                    int count = 0;
                    await using Db db = new Db();
                    foreach (var user in users)
                    {
                        var accounts = user.Instagrams.ToList().Where(x => !x.IsDeactivated).ToList();
                        var overdue = user.Subscribes.ToList()
                            .Where(subscribe => subscribe.EndSubscribe.CompareTo(DateTime.Now) <= 0).ToList();
                        count += overdue.Count;
                        foreach (var subscribe in overdue)
                        {
                            db.UpdateRange(user, subscribe);
                            db.Remove(subscribe);
                        }

                        var accountsUsername = string.Empty;
                        for (var i = accounts.Count - (user.Subscribes.Count - overdue.Count); i > 0; i--)
                        {
                            var inst = accounts[^i];
                            if (inst == null) continue;
                            db.UpdateRange(inst);
                            inst.IsDeactivated = true;
                            accountsUsername += ", " + inst.Username;
                        }

                        try
                        {
                            if (overdue.Count > 0)
                            {
                                if (accountsUsername != String.Empty)
                                    await Tgbot.SendTextMessageAsync(user.Id,
                                        $"Действие {overdue.Count} подписки(ок) завершилось. Аккаунт(ы) {accountsUsername[2..]} деактивирован(ы).");
                                else
                                {
                                    await Tgbot.SendTextMessageAsync(user.Id,
                                        $"Действие {overdue.Count} подписки(ок) завершилось.");
                                }
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    await db.SaveChangesAsync();
                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss}] Произведена проверка подписок. Подписок удалено: {count}.\n");
                    await Task.Delay(new TimeSpan(1, 0, 0, 0));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Ошибка при проверке подписовк. {ex.Message}\n");
                }
            }
        }

        public static async Task LoadWorksAsync(List<WorkTask> worksList)
        {
            try
            {
                await using Db db = new Db();
                foreach (var work in worksList)
                {
                    try
                    {
                        if (work.StartTime.CompareTo(DateTime.Now) <= 0 || work.Instagram.IsDeactivated)
                        {
                            db.Update(work);
                            db.Remove(work);
                            await db.SaveChangesAsync();
                            continue;
                        }

                        User user = work.Instagram.User;
                        Work workUser = new Work(user.Works.Count, work.Instagram, user);
                        workUser.SetHashtag(work.Hashtag);
                        workUser.SetDuration(work.LowerDelay, work.UpperDelay);
                        workUser.SetMode(work.Mode);
                        workUser.SetHashtagType(work.HashtagType);
                        workUser.SetOffset(work.Offset);
                        await workUser.StartAtTimeAsync(work.StartTime, work);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
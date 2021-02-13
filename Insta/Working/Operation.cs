using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Insta.Entities;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Logger;
using Telegram.Bot;

namespace Insta.Working
{
    internal static class Operation
    {
        private static List<Proxy> _proxies;
        private static int _number;
        private static readonly object Locker = new();
        private static IWebProxy GetProxy(Instagram instagram)
        {
            lock (Locker)
            {
                try
                {
                    var proxy = _proxies[_number];
                    _number=(++_number)%_proxies.Count;
                    var webProxy = new WebProxy(proxy.Host,proxy.Port)
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
                    return new WebProxy();
                }
            }

        }

        public static void LoadProxy(List<Proxy> proxies)
        {
            _proxies = proxies;
        }
        public static bool AddProxy(string credentials)
        {
            try
            {
                var data = credentials.Split(':');
                if (data.Length != 4) return false;
                using Db db = new Db();
                var proxy = new Proxy()
                {
                    Host = data[0],
                    Port = int.Parse(data[1]),
                    Login = data[2],
                    Password = data[3]
                };
                db.Add(proxy);
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool CheckProxy(Proxy proxy)
        {
            try
            {
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
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] (id {proxy.Id}): {proxy.Host}:{proxy.Port} - неверные данные, прокси будет удалена!\n");
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
                        ProxyError(proxy,x.Result.StatusCode);
                        return false;
                } 
            }
            catch
            {
                ProxyError(proxy,HttpStatusCode.NotFound);
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
        public static async Task<IResult<InstaLoginResult>> CheckLoginAsync(Instagram instagram)
        {
            try
            {
                var userSession = new UserSessionData
                {
                    UserName = instagram.Username,
                    Password = instagram.Password
                };
                var instaApi = InstaApiBuilder.CreateBuilder()
                    .UseHttpClientHandler(new HttpClientHandler {Proxy = GetProxy(instagram)})
                    .SetUser(userSession)
                    .Build();
                if (instaApi.IsUserAuthenticated) return null;
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

        private static async Task<bool> LoadFromStateData(Instagram instagram)
        {
            try
            {
                var instaApi = InstaApiBuilder.CreateBuilder()
                    .UseHttpClientHandler(new HttpClientHandler {Proxy = GetProxy(instagram)})
                    .Build();
                await instaApi.LoadStateDataFromStringAsync(instagram.StateData);
                instagram.Api = instaApi;
                return true;
            }
            catch
            {
                return false;
            }

        }

        public static async void LoadUsersStateData(List<Instagram> instagrams)
        {
            foreach (var instagram in instagrams)
            {
                await LoadFromStateData(instagram);
            }
        }
        public static async Task<IResult<InstaLoginTwoFactorResult>> SendCodeTwoFactorAsync(IInstaApi api, string code)
        {
            var response = await api.TwoFactorLoginAsync(code);
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

        public static async Task<bool> LogOut(User user, Instagram inst)
        {
            try
            {
                if (inst.Api != null) await inst.Api.LogoutAsync();
                await using Db db = new Db();
                db.UpdateRange(user, inst);
                db.Remove(inst);
                await db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        private static readonly TelegramBotClient Tgbot =
            new("1682222171:AAGw4CBCJ875NRn1rFnh0sBncYkev5KIa4o");
        public static async void SubscribeToEvent(List<User> users)
        {
            await using Db db = new Db();
            foreach (var user in users)
            {
                var accounts = user.Instagrams.ToList().Where(x=>!x.IsDeactivated).ToList();
                var overdue = user.Subscribes.ToList()
                    .Where(subscribe => subscribe.EndSubscribe.CompareTo(DateTime.Now) <= 0).ToList();
                foreach (var subscribe in overdue)
                {
                    db.UpdateRange(user,subscribe);
                    db.Remove(subscribe);
                }
                var accountsUsername = string.Empty;
                for(var i = accounts.Count-(user.Subscribes.Count-overdue.Count);i>0;i--)
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
                        if(accountsUsername!=String.Empty)
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
            await Task.Delay(new TimeSpan(1, 0, 0, 0));
        }
    }
}

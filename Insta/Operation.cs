using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using Telegram.Bot;

namespace Insta
{
    static class Operation
    {

        public static async Task<IResult<InstaLoginResult>> CheckLoginAsync(Instagram instagram)
        {
            try
            {
                var userSession = new UserSessionData
                {
                    UserName = instagram.Username,
                    Password = instagram.Password
                };

                IInstaApi instaApi = InstaApiBuilder.CreateBuilder()
                    .SetUser(userSession)
                    .Build();
                if (instaApi.IsUserAuthenticated) return null;
                var logInResult = await instaApi.LoginAsync();
                if (logInResult.Value == InstaLoginResult.Success && !logInResult.Succeeded) return null;
                instagram.api = instaApi;
                return logInResult;

            }
            catch
            {
                return null;
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

        public static async Task<bool> LogOut(User user, int id)
        {
            Instagram inst = user.Instagrams.ToList().Find(x => x.Id == id);
            if (inst == null) return false;
            if (inst.api != null) await inst.api.LogoutAsync();
            await using DB db = new DB();
            db.UpdateRange(user, inst);
            db.Remove(inst);
            await db.SaveChangesAsync();
            return true;
        }
        private static readonly TelegramBotClient Tgbot =
            new("1682222171:AAGw4CBCJ875NRn1rFnh0sBncYkev5KIa4o");
        public static async void SubscribeToEvent(List<User> users)
        {
            await using DB db = new DB();
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

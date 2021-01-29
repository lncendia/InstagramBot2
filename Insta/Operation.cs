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
                Console.WriteLine($"Logging in as {userSession.UserName}");
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
            new TelegramBotClient("1682222171:AAGw4CBCJ875NRn1rFnh0sBncYkev5KIa4o");
        public static async void SubscribeToEvent(List<User> users)
        {
            await using DB db = new DB();
            foreach (var user in users)
            {
                foreach (var subscribe in user.Subscribes.ToList().Where(subscribe => subscribe.EndSubscribe.CompareTo(DateTime.Now) <= 0))
                {
                    db.UpdateRange(user,subscribe);
                    db.Remove(subscribe);
                    var inst = user.Instagrams.ToList().LastOrDefault(x => x.IsDeactivated == false);
                    if(inst!=null) inst.IsDeactivated = true;
                    await Tgbot.SendTextMessageAsync(user.Id,
                        $"Действие подписки завершилось. Аккаунт {inst.Username} деактивирован.");
                }
            }
            await db.SaveChangesAsync();
            //await Task.Delay(new TimeSpan(1, 0, 0, 0));
        }
    }
}

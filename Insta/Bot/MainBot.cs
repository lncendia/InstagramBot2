using System;
using System.Net.Http;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Model;
using Insta.Working;
using InstagramApiSharp.Classes;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Insta.Bot;

internal static class MainBot
{
    public static void Start()
    {
        BotSettings.Get().StartReceiving(new UpdateHandler());
    }

    public static async Task<bool> Login(ITelegramBotClient client, User user, IResult<InstaLoginResult> login)
    {
        if (login == null)
        {
            await client.SendTextMessageAsync(user.Id, "Ошибка. Данные введены неверно.");
            user.EnterData = null;
            user.State = State.main;
            return false;
        }

        switch (login.Value)
        {
            case InstaLoginResult.Success:
            {
                if (!login.Succeeded)
                {
                    await client.SendTextMessageAsync(user.Id,
                        "Ошибка. Данные введены неверно.");
                    user.EnterData = null;
                    user.State = State.main;
                    return false;
                }

                user.EnterData.StateData = await user.EnterData.Api.GetStateDataAsStringAsync();
                await client.SendTextMessageAsync(user.Id, "Инстаграм успешно добавлен.");
                user.State = State.main;
                return true;
            }
            case InstaLoginResult.BadPassword:
                await client.SendTextMessageAsync(user.Id, "Неверный пароль. Попробуйте ввести снова.",
                    replyMarkup: Keyboards.Back("password"));
                user.State = State.enterPassword;
                break;
            case InstaLoginResult.InvalidUser:
                user.State = State.main;
                user.EnterData = null;
                await client.SendTextMessageAsync(user.Id, "Пользователь не найден. Попробуйте еще раз.",
                    replyMarkup: Keyboards.EnterData);
                break;
            case InstaLoginResult.LimitError:
                user.State = State.main;
                user.EnterData = null;
                await client.SendTextMessageAsync(user.Id,
                    "Слишком много запросов. Подождите несколько минут и попробуйте снова.",
                    replyMarkup: Keyboards.EnterData);
                break;
            case InstaLoginResult.TwoFactorRequired:
                await client.SendTextMessageAsync(user.Id,
                    "Необходима двухфакторная аутентификация. Введите код из сообщения.",
                    replyMarkup: Keyboards.Main);
                user.State = State.enterTwoFactorCode;
                break;
            case InstaLoginResult.ChallengeRequired:
            {
                var challenge = await user.EnterData.Api.GetChallengeRequireVerifyMethodAsync();
                if (!challenge.Succeeded)
                {
                    await client.SendTextMessageAsync(user.Id,
                        "Ошибка. Попробуйте войти ещё раз.");
                    user.EnterData = null;
                    user.State = State.main;
                    return false;
                }

                if (challenge.Value.SubmitPhoneRequired)
                {
                    user.State = State.challengeRequiredPhoneCall;
                    await client.SendTextMessageAsync(user.Id,
                        "Инстаграм просит подтверждение. Введите подключенный к аккаунту номер.",
                        replyMarkup: Keyboards.Main);
                    break;
                }

                InlineKeyboardMarkup key;
                if (string.IsNullOrEmpty(challenge.Value.StepData.PhoneNumber))
                {
                    key = Keyboards.Email(challenge.Value.StepData.Email);
                }
                else if (string.IsNullOrEmpty(challenge.Value.StepData.Email))
                {
                    key = Keyboards.Phone(challenge.Value.StepData.PhoneNumber);
                }
                else
                {
                    key = Keyboards.PhoneAndEmail(challenge.Value.StepData.Email,
                        challenge.Value.StepData.PhoneNumber);
                }

                user.State = State.challengeRequired;
                await client.SendTextMessageAsync(user.Id,
                    "Инстаграм просит подтверждение. Выбирете, каким образом вы хотите подтвердить код:",
                    replyMarkup: key);
            }
                break;
            case InstaLoginResult.Exception:
                if (login.Info.Exception is HttpRequestException or NullReferenceException)
                {
                    Operation.CheckProxy(user.EnterData.Proxy);
                }

                await client.SendTextMessageAsync(user.Id,
                    "Ошибка при отправке запроса. Попробуйте войти ещё раз.");
                user.EnterData = null;
                user.State = State.main;
                break;

            default:
                await client.SendTextMessageAsync(user.Id,
                    "Ошибка. Попробуйте еще раз!");
                user.EnterData = null;
                user.State = State.main;
                break;
        }

        return false;
    }
}
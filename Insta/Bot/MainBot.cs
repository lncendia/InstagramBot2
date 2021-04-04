using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Model;
using Insta.Working;
using InstagramApiSharp.Classes;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Insta.Bot
{
    internal static class MainBot
    {
        private static readonly TelegramBotClient Tgbot = BotSettings.Get();
        private static readonly List<User> Users = BotSettings.Users;

        public static void Start()
        {
            Tgbot.OnMessage += Tgbot_OnMessage;
            Tgbot.OnCallbackQuery += Tgbot_OnCallbackQuery;
            Tgbot.StartReceiving();
        }

        private static async void Tgbot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            try
            {
                var user = Users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                if (user == null) return;
                var command = BotSettings.CallbackQueryCommands.Find(_ => _.Compare(e.CallbackQuery, user));
                if (command != null) await command.Execute(Tgbot, user, e.CallbackQuery);
            }
            catch
            {
                var user = Users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                Error(user);
            }
        }

        private static async void Tgbot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                var message = e.Message;
                var user = Users.Find(x => x.Id == message.From.Id);
                var command = BotSettings.Commands.FirstOrDefault(_ => _.Compare(message, user));
                if (command != null) await command.Execute(Tgbot, user, message);
            }
            catch
            {
                var user = Users.FirstOrDefault(x => x.Id == e.Message.From.Id);
                Error(user);
            }
        }

        public static async Task<bool> Login(User user, IResult<InstaLoginResult> login)
        {
            try
            {
                if (login == null)
                {
                    await Tgbot.SendTextMessageAsync(user.Id,
                        "Ошибка. Данные введены неверно.");
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
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Ошибка. Данные введены неверно.");
                            user.EnterData = null;
                            user.State = State.main;
                            return false;
                        }

                        user.EnterData.StateData = await user.EnterData.Api.GetStateDataAsStringAsync();
                        await Tgbot.SendTextMessageAsync(user.Id, "Инстаграм успешно добавлен.");
                        user.State = State.main;
                        return true;
                    }
                    case InstaLoginResult.BadPassword:
                        await Tgbot.SendTextMessageAsync(user.Id, "Неверный пароль. Попробуйте ввести снова.",
                            replyMarkup: Keyboards.Back("password"));
                        user.State = State.enterPassword;
                        break;
                    case InstaLoginResult.InvalidUser:
                        user.State = State.main;
                        user.EnterData = null;
                        await Tgbot.SendTextMessageAsync(user.Id, "Пользователь не найден. Попробуйте еще раз.",
                            replyMarkup: Keyboards.EnterData);
                        break;
                    case InstaLoginResult.LimitError:
                        user.State = State.main;
                        user.EnterData = null;
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Слишком много запросов. Подождите несколько минут и попробуйте снова.",
                            replyMarkup: Keyboards.EnterData);
                        break;
                    case InstaLoginResult.TwoFactorRequired:
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Необходима двухфакторная аутентификация. Введите код из сообщения.",
                            replyMarkup: Keyboards.Main);
                        user.State = State.enterTwoFactorCode;
                        break;
                    case InstaLoginResult.ChallengeRequired:
                    {
                        var challenge = await user.EnterData.Api.GetChallengeRequireVerifyMethodAsync();
                        if (!challenge.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Ошибка. Попробуйте войти ещё раз.");
                            user.EnterData = null;
                            user.State = State.main;
                            return false;
                        }

                        if (challenge.Value.SubmitPhoneRequired)
                        {
                            user.State = State.challengeRequiredPhoneCall;
                            await Tgbot.SendTextMessageAsync(user.Id,
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
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Инстаграм просит подтверждение. Выбирете, каким образом вы хотите подтвердить код:",
                            replyMarkup: key);
                    }
                        break;
                    case InstaLoginResult.Exception:
                        if (login.Info.Exception is HttpRequestException ||
                            login.Info.Exception is NullReferenceException)
                        {
                            Operation.CheckProxy(user.EnterData.Proxy);
                        }

                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Ошибка при отправке запроса. Попробуйте войти ещё раз.");
                        user.EnterData = null;
                        user.State = State.main;
                        break;

                    default:
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Ошибка. Попробуйте еще раз!");
                        user.EnterData = null;
                        user.State = State.main;
                        break;
                }

                return false;
            }
            catch
            {
                Error(user);
                return false;
            }
        }

        private static async void Error(User user)
        {
            if (user == null) return;
            user.CurrentWorks.ForEach(x => user.Works.Remove(x));
            user.EnterData = null;
            user.State = State.main;
            try
            {
                await Tgbot.SendTextMessageAsync(user.Id,
                    "Произошла ошибка.", replyMarkup: Keyboards.MainKeyboard);
            }
            catch
            {
                // ignored
            }
        }
    }
}
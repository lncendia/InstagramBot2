using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands
{
    public class GoBackQueryCommand : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            switch (user.State)
            {
                case State.enterPassword:
                    user.State = State.enterLogin;
                    await client.DeleteMessageAsync(query.From.Id,
                        query.Message.MessageId);
                    await client.SendTextMessageAsync(query.From.Id,
                        "Введите логин", replyMarkup: Keyboards.Main);
                    break;
                case State.enterTwoFactorCode:
                    user.State = State.enterPassword;
                    await client.DeleteMessageAsync(query.From.Id,
                        query.Message.MessageId);
                    await client.SendTextMessageAsync(query.From.Id,
                        "Теперь введите пароль", replyMarkup: Keyboards.Back);
                    break;
                case State.setHashtag:
                    user.State = State.setMode;
                    await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                        "Выберите режим.", replyMarkup: Keyboards.SelectMode);
                    break;
                case State.setDuration:
                    user.State = State.setHashtag;
                    await client.DeleteMessageAsync(query.From.Id,
                        query.Message.MessageId);
                    await client.SendTextMessageAsync(query.From.Id,
                        "Введите хештег без #.", replyMarkup: Keyboards.Back);
                    break;
                case State.setDate:
                    await client.SendTextMessageAsync(query.From.Id,
                        "Выбирете, когда хотите начать.", replyMarkup: Keyboards.StartWork);
                    await client.DeleteMessageAsync(query.From.Id,
                        query.Message.MessageId);
                    user.State = State.setTimeWork;
                    break;
                case State.setOffset:
                    user.State = State.setDuration;
                    await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                        "Введите пределы интервала в секундах. (<strong>Пример:</strong> <em>30:120</em>).\nРекомендуемые параметры нижнего предела:\nНоввый аккаунт: <code>120 секунд.</code>\n3 - 6 месяцев: <code>90 секунд.</code>\nБольше года: <code>72 секунды.</code>\n",
                        replyMarkup: Keyboards.Back, parseMode: ParseMode.Html);
                    break;
                case State.enterOffset:
                    await client.DeleteMessageAsync(query.From.Id,
                        query.Message.MessageId);
                    await client.SendTextMessageAsync(query.From.Id,
                        "С какого поста начать отработку?", replyMarkup: Keyboards.SetOffset);
                    user.State = State.setOffset;
                    break;
            }
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "back";
        }
    }
}
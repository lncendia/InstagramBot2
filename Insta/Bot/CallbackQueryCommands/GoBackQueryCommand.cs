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
            switch (query.Data[5..])
            {
                case "password":
                    if (user.State != State.enterPassword)
                    {
                        await client.AnswerCallbackQueryAsync(query.Id, "Вы не вводите пароль сейчас.");
                        break;
                    }

                    user.State = State.enterLogin;
                    await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId, "Введите логин",
                        replyMarkup: Keyboards.Main);
                    break;
                case "selectHashtagMode":
                    if (user.State != State.setMode)
                    {
                        await client.AnswerCallbackQueryAsync(query.Id, "Вы не выбираете режим сейчас.");
                        break;
                    }

                    user.State = State.setHashtagType;
                    await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                        "Выберите тип публикаций.", replyMarkup: Keyboards.SelectHashtagMode);
                    break;
                case "selectMode":
                    if (user.State != State.setHashtag)
                    {
                        await client.AnswerCallbackQueryAsync(query.Id, "Вы не вводите хештег сейчас.");
                        break;
                    }

                    user.State = State.setMode;
                    await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId, "Выберите режим.",
                        replyMarkup: Keyboards.SelectMode);
                    break;
                case "interval":
                    if (user.State != State.setDuration)
                    {
                        await client.AnswerCallbackQueryAsync(query.Id, "Вы не вводите интервал сейчас.");
                        break;
                    }

                    user.State = State.setHashtag;
                    await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId, "Введите хештег без #.",
                        replyMarkup: Keyboards.Back("selectMode"));
                    break;
                case "date":
                    if (user.State != State.setDate)
                    {
                        await client.AnswerCallbackQueryAsync(query.Id, "Вы не указываете дату сейчас.");
                        break;
                    }

                    await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                        "Выбирете, когда хотите начать.",
                        replyMarkup: Keyboards.StartWork);
                    user.State = State.setTimeWork;
                    break;
                case "offset":
                    if (user.State != State.setOffset && user.State != State.setDuration)
                    {
                        await client.AnswerCallbackQueryAsync(query.Id, "Вы не указываете сдвиг сейчас.");
                        break;
                    }

                    user.State = State.setDuration;
                    await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                        "Введите пределы интервала в секундах. (<strong>Пример:</strong> <em>30:120</em>).\nРекомендуемые параметры нижнего предела:\nНоввый аккаунт: <code>120 секунд.</code>\n3 - 6 месяцев: <code>90 секунд.</code>\nБольше года: <code>72 секунды.</code>\n",
                        replyMarkup: Keyboards.Back("interval"), parseMode: ParseMode.Html);
                    break;
                case "offsetSelect":
                    if (user.State != State.enterOffset)
                    {
                        await client.AnswerCallbackQueryAsync(query.Id, "Вы не вводите сдвиг сейчас.");
                        break;
                    }

                    await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                        "С какого поста начать отработку?", replyMarkup: Keyboards.SetOffset);
                    user.State = State.setOffset;
                    break;
                case "subscribes":
                    await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                        $"<b>Ваш Id:</b> {user.Id}\n<b>Бонусный счет:</b> {user.Bonus}₽\n<b>Реферальная ссылка:</b> https://telegram.me/LikeChatVip_bot?start={user.Id}",
                        ParseMode.Html, disableWebPagePreview: true, replyMarkup: Keyboards.Subscribes);
                    break;
            }
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data.StartsWith("back");
        }
    }
}
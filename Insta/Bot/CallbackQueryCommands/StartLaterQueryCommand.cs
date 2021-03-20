using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands
{
    public class StartLaterQueryCommand : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            await client.DeleteMessageAsync(query.From.Id,
                query.Message.MessageId);
            user.State = State.setDate;
            await client.SendTextMessageAsync(query.From.Id,
                "Введите время запуска по МСК в формате ЧЧ:мм. (<strong>Пример:</strong> <em>13:30</em>).",
                replyMarkup: Keyboards.Back, parseMode: ParseMode.Html);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "startLater" && user.State == State.setTimeWork;
        }
    }
}
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands
{
    public class SetOffsetQueryCommand:ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            await client.SendTextMessageAsync(user.Id, "Введите сдвиг.", replyMarkup: Keyboards.Back);
            user.State = State.enterOffset;
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "setOffset" && user.State == State.setOffset;
        }
    }
}
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands
{
    public class SelectSaveModeQueryCommand : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            user.CurrentWorks.ForEach(x => x.SetMode(Mode.save));
            user.State = State.setHashtag;
            await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                "Введите хештег без #.", replyMarkup: Keyboards.Back);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "startSave" && user.State == State.setMode;
        }
    }
}
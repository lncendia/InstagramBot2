using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class StartWithOutOffsetQueryCommand : ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        user.State = State.setTimeWork;
        await client.EditMessageTextAsync(query.From.Id,query.Message.MessageId,
            "Выбирете, когда хотите начать.", replyMarkup: Keyboards.StartWork);
    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data == "lastPost" && user.State == State.setOffset;
    }
}
using System.Linq;
using System.Threading.Tasks;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class MySubscribesQueryCommand:ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        var subscribes = $"У вас {user.Subscribes.Count} подписки(ок).\n";
        var i = 0;
        foreach (var sub in user.Subscribes.ToList())
        {
            i++;
            subscribes += $"Подписка {i}. Истекает {sub.EndSubscribe:D}\n";
        }
        await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId, subscribes,
            replyMarkup: Keyboards.Back("subscribes"));
    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data == "subscribes";
    }
}
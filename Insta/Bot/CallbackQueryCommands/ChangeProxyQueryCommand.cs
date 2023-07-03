using System.Linq;
using System.Threading.Tasks;
using Insta.Interfaces;
using Insta.Working;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class ChangeProxyQueryCommand : ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        if (query.Data.StartsWith("changeProxy"))
        {
            var instagram = user.Instagrams.FirstOrDefault(_ => _.Id == int.Parse(query.Data[12..]));
            if (instagram == null)
            {
                await client.AnswerCallbackQueryAsync(query.Id, "Инстаграм не найден.");
                await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            }
            else
            {
                var message = (Operation.ChangeProxy(instagram)) ? "Успешно." : "Ошибка.";
                await client.AnswerCallbackQueryAsync(query.Id, message);
                await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            }
        }
    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data.StartsWith("changeProxy");
    }
}
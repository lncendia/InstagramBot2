using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Working;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class CancelWorkQueryCommand : ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        if (user.State != State.main)
        {
            await client.AnswerCallbackQueryAsync(query.Id, "Вы должны быть в главное меню.");
            return;
        }
        var work = user.Works.Find(x => x.Id == int.Parse(query.Data[7..]));
        if (work == null)
        {
            user.State = State.main;
            await client.AnswerCallbackQueryAsync(query.Id, "Отработка не найдена.");
            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            return;
        }

        if (work.IsStarted)
        {
            work.CancelTokenSource.Cancel();
        }
        else
        {
            await work.TimerDisposeAsync();
        }

        user.Works.Remove(work);
        await client.AnswerCallbackQueryAsync(query.Id, "Отработка отменена.");
        await client.DeleteMessageAsync(query.From.Id,
            query.Message.MessageId);
        user.State = State.main;
    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data.StartsWith("cancel");
    }
}
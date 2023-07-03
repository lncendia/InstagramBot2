using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class StartEnterAccountDataQueryCommand : ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        if (user.State != State.main)
        {
            await client.AnswerCallbackQueryAsync(query.Id, "Вы должны быть в главное меню.");
            return;
        }

        await client.DeleteMessageAsync(query.From.Id,
            query.Message.MessageId);
        if (user.Instagrams.Count >= user.Subscribes.Count)
        {
            await client.SendTextMessageAsync(query.From.Id,
                "Увы... Так не работает.");
            return;
        }

        user.EnterData = new Instagram {User = user};
        user.State = State.enterLogin;
        await client.SendTextMessageAsync(query.From.Id,
            "Введите логин", replyMarkup: Keyboards.Main);
    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data == "enterData";
    }
}
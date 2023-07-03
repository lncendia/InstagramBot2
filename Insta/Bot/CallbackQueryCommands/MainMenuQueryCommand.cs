using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class MainMenuQueryCommand : ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        await client.DeleteMessageAsync(query.From.Id,
            query.Message.MessageId);
        foreach (var work in user.CurrentWorks)
        {
            user.Works.Remove(work);
        }

        user.CurrentWorks.Clear();
        user.EnterData = null;
        user.State = State.main;
        await client.SendTextMessageAsync(query.From.Id,
            "Вы в главном меню.", replyMarkup: Keyboards.MainKeyboard);

    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data == "mainMenu" && user.State != State.block;
    }
}
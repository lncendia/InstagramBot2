using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands;

public class SendKeyboardCommand:ITextCommand
{
    public async Task Execute(ITelegramBotClient client, User user, Message message)
    {
        user.CurrentWorks.ForEach(_ => user.Works.Remove(_));
        user.CurrentWorks.Clear();
        user.EnterData = null;
        user.State = State.main;
        await client.SendTextMessageAsync(message.From.Id,
            "Вы в главном меню.", replyMarkup:Keyboards.MainKeyboard);
    }

    public bool Compare(Message message, User user)
    {
        return message.Type == MessageType.Text && message.Text.StartsWith("/start") && user.State != State.block;
    }
}
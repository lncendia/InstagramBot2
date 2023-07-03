using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Working;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands;

public class EnterPasswordCommand : ITextCommand
{
    public async Task Execute(ITelegramBotClient client, User user, Message message)
    {
        if (message.Text.Length < 6)
        {
            await client.SendTextMessageAsync(message.Chat.Id,
                "Пароль не может быть меньше 6 символов!");
            return;
        }

        user.State = State.block;
        user.EnterData.Password = message.Text;
        var login = await Operation.CheckLoginAsync(user.EnterData);
        if (await MainBot.Login(client, user, login))
        {
            await using var db = new Db();
            db.Update(user);
            db.Add(user.EnterData);
            user.EnterData = null;
            user.State = State.main;
            await db.SaveChangesAsync();
        }
    }

    public bool Compare(Message message, User user)
    {
        return message.Type == MessageType.Text && user.State == State.enterPassword;
    }
}
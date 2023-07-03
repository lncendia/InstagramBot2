using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Working;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands;

public class EnterChallengeRequireCodeCommand : ITextCommand
{
    public async Task Execute(ITelegramBotClient client, User user, Message message)
    {
        var y = await Operation.SendCodeChallengeRequiredAsync(user.EnterData.Api,
            message.Text);
        if (await MainBot.Login(client, user, y))
        {
            await using var db = new Db();
            db.Update(user);
            db.Add(user.EnterData);
            user.EnterData = null;
            await db.SaveChangesAsync();
        }

    }

    public bool Compare(Message message, User user)
    {
        return message.Type == MessageType.Text && user.State == State.challengeRequiredAccept;
    }
}
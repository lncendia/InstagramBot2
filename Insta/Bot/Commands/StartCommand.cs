using System.Linq;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using User = Insta.Model.User;

namespace Insta.Bot.Commands
{
    public class StartCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            await using Db db = new Db();
            user = new User {Id = message.From.Id, State = State.main};
            if (message.Text.Length > 7 && long.TryParse(message.Text[7..], out long id))
            {
                User referal = BotSettings.Users.FirstOrDefault(_ => _.Id == id);
                if (referal != null)
                {
                    db.Update(referal);
                    user.Referal = referal;
                }
            }
            BotSettings.Users.Add(user);
            db.Add(user);
            await db.SaveChangesAsync();
            await client.SendStickerAsync(message.From.Id,
                new InputOnlineFile("CAACAgIAAxkBAAK_HGAQINBHw7QKWWRV4LsEU4nNBxQ3AAKZAAPZvGoabgceWN53_gIeBA"),
                replyMarkup: Keyboards.MainKeyboard);
            await client.SendTextMessageAsync(message.Chat.Id,
                "Добро пожаловать.\nДля дальнейшей работы тебе необходимо оплатить подписку и ввести данные своего instagram.");
        }

        public bool Compare(Message message, User user)
        {
            return user is null;
        }
    }
}
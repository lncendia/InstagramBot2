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
using System.Linq;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands
{
    public class EnterLoginCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (user.Instagrams.Any(instagram => instagram.Username == message.Text))
            {
                await client.SendTextMessageAsync(message.Chat.Id,
                    "Вы уже добавили этот аккаунт.");
                user.State = State.main;
                return;
            }

            user.EnterData.Username = message.Text;
            user.State = State.enterPassword;
            await client.SendTextMessageAsync(message.Chat.Id,
                "Теперь введите пароль.", replyMarkup: Keyboards.Back("password"));
        }

        public bool Compare(Message message, User user)
        {
            return message.Type == MessageType.Text && user.State == State.enterLogin;
        }
    }
}
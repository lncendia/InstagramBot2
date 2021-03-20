using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands
{
    public class WorkCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            await client.SendTextMessageAsync(message.Chat.Id,
                "Выберите, что вы хотите сделать.", replyMarkup: Keyboards.Working);
        }

        public bool Compare(Message message, User user)
        {
            return message.Type == MessageType.Text && message.Text == "❤ Отработка" && user.State == State.main;
        }
    }
}
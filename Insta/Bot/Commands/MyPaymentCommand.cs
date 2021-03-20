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
    public class MyPaymentCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            string subscribes = $"У вас {user.Subscribes.Count} подписки(ок).\n";
            int i = 0;
            foreach (var sub in user.Subscribes.ToList())
            {
                i++;
                subscribes += $"Подписка {i}. Истекает {sub.EndSubscribe:D}\n";
            }

            await client.SendTextMessageAsync(message.Chat.Id,
                subscribes);
        }

        public bool Compare(Message message, User user)
        {
            return message.Type == MessageType.Text && message.Text == "⏱ Мои подписки" && user.State == State.main;
        }
    }
}